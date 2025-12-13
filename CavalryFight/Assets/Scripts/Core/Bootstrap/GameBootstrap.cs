#nullable enable

using CavalryFight.Core.Services;
using CavalryFight.Services.AI;
using CavalryFight.Services.Audio;
using CavalryFight.Services.Customization;
using CavalryFight.Services.Customization.Appliers;
using CavalryFight.Services.GameSettings;
using CavalryFight.Services.Input;
using CavalryFight.Services.Lobby;
using CavalryFight.Services.Match;
using CavalryFight.Services.Replay;
using CavalryFight.Services.SceneManagement;
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

            // 全サービスを初期化
            InitializeServices();

            Debug.Log("[GameBootstrap] Initialization complete.");
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
        }

        /// <summary>
        /// すべてのサービスを初期化します
        /// </summary>
        private void InitializeServices()
        {
            Debug.Log("[GameBootstrap] Initializing services...");

            // ServiceLocatorから全サービスを取得して初期化
            var inputBindingService = ServiceLocator.Instance.Get<IInputBindingService>();
            var inputService = ServiceLocator.Instance.Get<IInputService>();
            var audioService = ServiceLocator.Instance.Get<IAudioService>();
            var settingsService = ServiceLocator.Instance.Get<IGameSettingsService>();
            var sceneService = ServiceLocator.Instance.Get<ISceneManagementService>();
            var aiService = ServiceLocator.Instance.Get<IBlazeAIService>();
            var customizationService = ServiceLocator.Instance.Get<ICustomizationService>();
            var replayRecorder = ServiceLocator.Instance.Get<IReplayRecorder>();
            var replayPlayer = ServiceLocator.Instance.Get<IReplayPlayer>();
            var lobbyService = ServiceLocator.Instance.Get<ILobbyService>();
            var matchService = ServiceLocator.Instance.Get<IMatchService>();

            // 初期化
            inputBindingService?.Initialize();
            inputService?.Initialize();
            audioService?.Initialize();
            settingsService?.Initialize();
            sceneService?.Initialize();
            aiService?.Initialize();
            customizationService?.Initialize();
            replayRecorder?.Initialize();
            replayPlayer?.Initialize();
            lobbyService?.Initialize();
            matchService?.Initialize();

            Debug.Log("[GameBootstrap] All services initialized.");
        }

        #endregion
    }
}
