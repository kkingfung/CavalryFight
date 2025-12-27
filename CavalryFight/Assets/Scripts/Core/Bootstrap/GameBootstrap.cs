#nullable enable

using CavalryFight.Core.Services;
using CavalryFight.Services.AI;
using CavalryFight.Services.Audio;
using CavalryFight.Services.Customization;
using CavalryFight.Services.GameSettings;
using CavalryFight.Services.GameState;
using CavalryFight.Services.Input;
using CavalryFight.Services.Lobby;
using CavalryFight.Services.Match;
using CavalryFight.Services.Replay;
using CavalryFight.Services.SceneManagement;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;

namespace CavalryFight.Core.Bootstrap
{
    /// <summary>
    /// ゲームブートストラップ
    /// </summary>
    /// <remarks>
    /// ゲーム起動時にすべてのサービスを初期化し、ServiceLocatorに登録します。
    /// このコンポーネントは永続化され、シーン遷移時も破棄されません。
    /// </remarks>
    [RequireComponent(typeof(ServiceUpdater))]
    [DisallowMultipleComponent]
    public class GameBootstrap : MonoBehaviour
    {
        #region Fields

        [Header("Configuration")]
        [Tooltip("シーンコレクション設定")]
        [SerializeField] private SceneCollectionConfig? _sceneCollectionConfig;

        /// <summary>
        /// クリティカルなサービスのリスト（これらが失敗すると起動を中止）
        /// </summary>
        private static readonly HashSet<Type> CriticalServices = new HashSet<Type>
        {
            typeof(IInputService),
            typeof(ISceneManagementService),
            typeof(IGameStateService)
        };

        /// <summary>
        /// 初期化に失敗したサービスのリスト
        /// </summary>
        private readonly List<Type> _failedServices = new List<Type>();

        #endregion

        #region Unity Lifecycle

        /// <summary>
        /// 初期化
        /// </summary>
        private void Awake()
        {
            // シーン遷移時も破棄されないようにする
            DontDestroyOnLoad(gameObject);

            Debug.Log("[GameBootstrap] Starting initialization...");

            // サービスを登録
            RegisterServices();

            // 依存関係を検証
            ValidateServiceDependencies();

            // 全サービスを初期化（非同期）
            // async voidを使用して例外を適切にキャッチ
            InitializeServicesAsyncWrapper();
        }

        /// <summary>
        /// InitializeServicesAsyncのラッパー（例外ハンドリング用）
        /// </summary>
        private async void InitializeServicesAsyncWrapper()
        {
            try
            {
                await InitializeServicesAsync();
            }
            catch (Exception ex)
            {
                Debug.LogError($"[GameBootstrap] Fatal error during service initialization: {ex.Message}\n{ex.StackTrace}");
#if UNITY_EDITOR
                UnityEditor.EditorApplication.isPlaying = false;
#else
                Application.Quit();
#endif
            }
        }

        /// <summary>
        /// 破棄時のクリーンアップ
        /// </summary>
        private void OnDestroy()
        {
            Debug.Log("[GameBootstrap] Starting cleanup...");

            // 全サービスを破棄（初期化の逆順）
            DisposeServices();

            Debug.Log("[GameBootstrap] Cleanup complete.");
        }

        #endregion

        #region Service Registration

        /// <summary>
        /// サービスを登録します
        /// </summary>
        /// <remarks>
        /// 依存関係の順序に注意してください。
        /// </remarks>
        private void RegisterServices()
        {
            Debug.Log("[GameBootstrap] Registering services...");

            // Core services (no dependencies)
            ServiceLocator.Instance.Register<IInputBindingService>(new InputBindingService());
            ServiceLocator.Instance.Register<IInputService>(new InputService());

            // Infrastructure services
            ServiceLocator.Instance.Register<IAudioService>(new AudioService());
            ServiceLocator.Instance.Register<IGameSettingsService>(new GameSettingsService());
            ServiceLocator.Instance.Register<ISceneManagementService>(new SceneManagementService());
            ServiceLocator.Instance.Register<IGameStateService>(new GameStateService());

            // Gameplay services
            ServiceLocator.Instance.Register<IBlazeAIService>(new BlazeAIService());

            // Customization service (with appliers)
            var characterApplier = new P09CharacterApplier();
            var mountApplier = new MalbersHorseApplier();
            var customizationService = new CustomizationService(characterApplier, mountApplier);
            ServiceLocator.Instance.Register<ICustomizationService>(customizationService);

            // Replay services
            ServiceLocator.Instance.Register<IReplayRecorder>(new ReplayRecorder());
            ServiceLocator.Instance.Register<IReplayPlayer>(new ReplayPlayer());

            // Network services
            // Note: NetworkLobbyManager and NetworkMatchManager are spawned by their services when needed
            ServiceLocator.Instance.Register<ILobbyService>(new LobbyService());
            ServiceLocator.Instance.Register<IMatchService>(new MatchService());

            Debug.Log("[GameBootstrap] All services registered.");

            // デバッグ: 登録されているサービスの一覧を出力
            ServiceLocator.Instance.LogRegisteredServices();
        }

        /// <summary>
        /// すべてのサービスを初期化し、MainMenuへ遷移します
        /// </summary>
        private async Task InitializeServicesAsync()
        {
            Debug.Log("[GameBootstrap] Initializing services...");

            _failedServices.Clear();

            // 初期化順序に従ってサービスを初期化（依存関係の順）
            // Core services
            InitializeService<IInputBindingService>();
            InitializeService<IInputService>();

            // Infrastructure services
            InitializeService<IAudioService>();
            InitializeService<IGameSettingsService>();
            InitializeService<ISceneManagementService>();
            ConfigureSceneManagementService();
            InitializeService<IGameStateService>();

            // Gameplay services
            InitializeService<IBlazeAIService>();
            InitializeService<ICustomizationService>();
            InitializeService<IReplayRecorder>();
            InitializeService<IReplayPlayer>();

            // Network services
            InitializeService<ILobbyService>();
            InitializeService<IMatchService>();

            // 初期化結果を確認
            if (_failedServices.Count > 0)
            {
                Debug.LogWarning($"[GameBootstrap] {_failedServices.Count} service(s) failed to initialize.");

                // クリティカルなサービスが失敗していないかチェック
                bool criticalServiceFailed = false;
                foreach (var failedServiceType in _failedServices)
                {
                    if (CriticalServices.Contains(failedServiceType))
                    {
                        Debug.LogError($"[GameBootstrap] CRITICAL: {failedServiceType.Name} failed to initialize!");
                        criticalServiceFailed = true;
                    }
                }

                if (criticalServiceFailed)
                {
                    Debug.LogError("[GameBootstrap] Critical service initialization failed. Application cannot continue.");
                    // ゲームを終了するか、エラー画面を表示
#if UNITY_EDITOR
                    UnityEditor.EditorApplication.isPlaying = false;
#else
                    Application.Quit();
#endif
                    return;
                }
            }

            Debug.Log("[GameBootstrap] All services initialized successfully.");

            // すべてのサービスの初期化が完了したら、MainMenuへ遷移
            if (_sceneCollectionConfig != null && _sceneCollectionConfig.MainMenu != null)
            {
                Debug.Log("[GameBootstrap] Transitioning to MainMenu...");
                var sceneService = ServiceLocator.Instance.Get<ISceneManagementService>();
                if (sceneService != null)
                {
                    try
                    {
                        await sceneService.OpenCollectionAsync(_sceneCollectionConfig.MainMenu);
                    }
                    catch (Exception ex)
                    {
                        Debug.LogError($"[GameBootstrap] Failed to transition to MainMenu: {ex.Message}\n{ex.StackTrace}");
                        Debug.LogError("[GameBootstrap] Application cannot continue. Please check the scene configuration.");
#if UNITY_EDITOR
                        UnityEditor.EditorApplication.isPlaying = false;
#else
                        Application.Quit();
#endif
                        return;
                    }
                }
                else
                {
                    Debug.LogError("[GameBootstrap] SceneManagementService not found. Cannot transition to MainMenu.");
#if UNITY_EDITOR
                    UnityEditor.EditorApplication.isPlaying = false;
#else
                    Application.Quit();
#endif
                    return;
                }
            }
            else
            {
                Debug.LogWarning("[GameBootstrap] MainMenu scene collection is not configured. Staying in Startup scene.");
            }

            Debug.Log("[GameBootstrap] Initialization complete.");
        }

        /// <summary>
        /// 個別のサービスを初期化します
        /// </summary>
        /// <typeparam name="T">サービスの型</typeparam>
        private void InitializeService<T>() where T : class, IService
        {
            try
            {
                var service = ServiceLocator.Instance.Get<T>();
                service.Initialize();
                Debug.Log($"[GameBootstrap] {typeof(T).Name} initialized successfully.");
            }
            catch (Exception ex)
            {
                Debug.LogError($"[GameBootstrap] Failed to initialize {typeof(T).Name}: {ex.Message}\n{ex.StackTrace}");
                _failedServices.Add(typeof(T));

                // クリティカルサービスでない場合は続行
                if (!CriticalServices.Contains(typeof(T)))
                {
                    Debug.LogWarning($"[GameBootstrap] {typeof(T).Name} is not critical. Continuing initialization...");
                }
            }
        }

        /// <summary>
        /// SceneManagementService にシーンコレクションを設定します
        /// </summary>
        private void ConfigureSceneManagementService()
        {
            if (_sceneCollectionConfig == null)
            {
                Debug.LogError("[GameBootstrap] SceneCollectionConfig is not assigned! Scene loading will not work.");
                return;
            }

            var sceneService = ServiceLocator.Instance.Get<ISceneManagementService>();
            if (sceneService == null)
            {
                Debug.LogError("[GameBootstrap] SceneManagementService not found. Cannot configure scene collections.");
                return;
            }

            sceneService.RegisterSceneCollections(
                startup: _sceneCollectionConfig.Startup,
                mainMenu: _sceneCollectionConfig.MainMenu,
                lobby: _sceneCollectionConfig.Lobby,
                settings: _sceneCollectionConfig.Settings,
                match: _sceneCollectionConfig.Match,
                training: _sceneCollectionConfig.Training,
                results: _sceneCollectionConfig.Results,
                replay: _sceneCollectionConfig.Replay
            );

            Debug.Log("[GameBootstrap] SceneManagementService configured with scene collections.");
        }

        /// <summary>
        /// すべてのサービスを破棄します
        /// </summary>
        /// <remarks>
        /// 初期化の逆順で破棄します。
        /// 一部のサービス破棄が失敗しても、他のサービスは破棄を続行します。
        /// </remarks>
        private void DisposeServices()
        {
            Debug.Log("[GameBootstrap] Disposing services...");

            // 初期化の逆順で破棄
            // 4. Network Services
            DisposeService<IMatchService>("MatchService");
            DisposeService<ILobbyService>("LobbyService");

            // 3. Gameplay Services
            DisposeService<IReplayPlayer>("ReplayPlayer");
            DisposeService<IReplayRecorder>("ReplayRecorder");
            DisposeService<ICustomizationService>("CustomizationService");
            DisposeService<IBlazeAIService>("BlazeAIService");

            // 2. Infrastructure Services
            DisposeService<IGameStateService>("GameStateService");
            DisposeService<ISceneManagementService>("SceneManagementService");
            DisposeService<IGameSettingsService>("GameSettingsService");
            DisposeService<IAudioService>("AudioService");

            // 1. Core Services
            DisposeService<IInputService>("InputService");
            DisposeService<IInputBindingService>("InputBindingService");

            Debug.Log("[GameBootstrap] All services disposed.");
        }

        /// <summary>
        /// 個別のサービスを破棄します
        /// </summary>
        /// <typeparam name="T">サービスの型</typeparam>
        /// <param name="serviceName">サービス名（ログ用）</param>
        private void DisposeService<T>(string serviceName) where T : class, IService
        {
            try
            {
                var service = ServiceLocator.Instance.Get<T>();
                service.Dispose();
                Debug.Log($"[GameBootstrap] {serviceName} disposed successfully.");
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"[GameBootstrap] Failed to dispose {serviceName}: {ex.Message}");
            }
        }

        #endregion

        #region Service Dependency Validation

        /// <summary>
        /// サービスの依存関係を検証します
        /// </summary>
        /// <remarks>
        /// 各サービスが必要とする依存サービスが正しく登録されているかを確認します。
        /// 依存関係が満たされていない場合はエラーログを出力します。
        /// </remarks>
        private void ValidateServiceDependencies()
        {
            Debug.Log("[GameBootstrap] Validating service dependencies...");

            // 依存関係マップを構築
            var dependencies = BuildDependencyMap();

            bool allDependenciesSatisfied = true;

            // 各サービスの依存関係をチェック
            foreach (var kvp in dependencies)
            {
                Type serviceType = kvp.Key;
                List<Type> requiredDependencies = kvp.Value;

                foreach (Type dependencyType in requiredDependencies)
                {
                    if (!IsServiceRegistered(dependencyType))
                    {
                        Debug.LogError($"[GameBootstrap] Dependency validation failed: {serviceType.Name} requires {dependencyType.Name}, but it is not registered!");
                        allDependenciesSatisfied = false;
                    }
                }
            }

            if (allDependenciesSatisfied)
            {
                Debug.Log("[GameBootstrap] All service dependencies are satisfied.");
            }
            else
            {
                Debug.LogError("[GameBootstrap] Some service dependencies are not satisfied. Please check the errors above.");
            }
        }

        /// <summary>
        /// サービス依存関係マップを構築します
        /// </summary>
        /// <returns>サービスタイプと依存サービスタイプのマップ</returns>
        private Dictionary<Type, List<Type>> BuildDependencyMap()
        {
            var dependencies = new Dictionary<Type, List<Type>>();

            // GameStateService は SceneManagementService に依存
            dependencies[typeof(IGameStateService)] = new List<Type>
            {
                typeof(ISceneManagementService)
            };

            // 将来的に他の依存関係を追加する場合はここに記述
            // 例:
            // dependencies[typeof(ISomeService)] = new List<Type>
            // {
            //     typeof(IAnotherService),
            //     typeof(IYetAnotherService)
            // };

            return dependencies;
        }

        /// <summary>
        /// サービスが登録されているかを確認します
        /// </summary>
        /// <param name="serviceType">サービスタイプ</param>
        /// <returns>登録されている場合true</returns>
        private bool IsServiceRegistered(Type serviceType)
        {
            try
            {
                // リフレクションを使用してServiceLocator.Get<T>()を呼び出す
                var getMethod = typeof(ServiceLocator).GetMethod("Get");
                if (getMethod == null)
                {
                    Debug.LogError("[GameBootstrap] ServiceLocator.Get method not found.");
                    return false;
                }

                var genericMethod = getMethod.MakeGenericMethod(serviceType);
                var service = genericMethod.Invoke(ServiceLocator.Instance, null);

                return service != null;
            }
            catch (Exception ex)
            {
                Debug.LogError($"[GameBootstrap] Error checking service registration for {serviceType.Name}: {ex.Message}");
                return false;
            }
        }

        #endregion
    }
}
