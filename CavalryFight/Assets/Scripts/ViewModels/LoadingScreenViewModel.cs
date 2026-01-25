#nullable enable

using CavalryFight.Core.MVVM;
using CavalryFight.Core.Services;
using CavalryFight.Services.Match;
using CavalryFight.Services.SceneManagement;
using UnityEngine;

namespace CavalryFight.ViewModels
{
    /// <summary>
    /// ローディング画面のViewModel
    /// </summary>
    /// <remarks>
    /// シーン遷移時のローディング進捗とメッセージを管理します。
    /// ISceneManagementServiceのイベントを購読して自動的に更新されます。
    /// Match/Trainingシーンでは、IMatchReadinessProviderのAllEntitiesReadyイベントを
    /// 待ってからローディング画面を非表示にします。
    /// </remarks>
    public class LoadingScreenViewModel : ViewModelBase
    {
        #region Fields

        private readonly ISceneManagementService? _sceneManagementService;
        private float _loadingProgress;
        private string _loadingMessage = "Loading...";
        private bool _isVisible;

        // エンティティ準備待機フラグ
        private bool _waitingForEntities;
        private bool _sceneLoadCompleted; // シーンロード完了フラグ
        private IMatchReadinessProvider? _matchReadinessProvider;
        private float _entityWaitStartTime;
        private float _lastProviderSearchTime;
        private const float ENTITY_WAIT_TIMEOUT = 30f; // エンティティ準備待機のタイムアウト（秒）
        private const float PROVIDER_SEARCH_INTERVAL = 0.5f; // プロバイダー検索間隔（秒）
        private const float INITIAL_SEARCH_DELAY = 0.5f; // 初回検索前の遅延（秒）

        #endregion

        #region Properties

        /// <summary>
        /// ローディングの進捗を取得または設定します（0.0～1.0）
        /// </summary>
        public float LoadingProgress
        {
            get => _loadingProgress;
            set => SetProperty(ref _loadingProgress, value);
        }

        /// <summary>
        /// ローディングメッセージを取得または設定します
        /// </summary>
        public string LoadingMessage
        {
            get => _loadingMessage;
            set => SetProperty(ref _loadingMessage, value);
        }

        /// <summary>
        /// ローディング画面の表示状態を取得または設定します
        /// </summary>
        public bool IsVisible
        {
            get => _isVisible;
            set => SetProperty(ref _isVisible, value);
        }

        #endregion

        #region Constructor

        /// <summary>
        /// LoadingScreenViewModelの新しいインスタンスを初期化します
        /// </summary>
        public LoadingScreenViewModel()
        {
            // サービスを取得
            _sceneManagementService = ServiceLocator.Instance.Get<ISceneManagementService>();

            // イベントを購読
            if (_sceneManagementService != null)
            {
                _sceneManagementService.SceneLoadStarted += OnLoadingStarted;
                _sceneManagementService.SceneLoadCompleted += OnLoadingCompleted;
            }

            Debug.Log("[LoadingScreenViewModel] ViewModel initialized.");
        }

        #endregion

        #region Event Handlers

        /// <summary>
        /// ローディング開始時に呼ばれます
        /// </summary>
        private void OnLoadingStarted(object? sender, SceneLoadEventArgs e)
        {
            Debug.Log($"[LoadingScreenViewModel] Loading started: {e.SceneName}");

            // 前回のMatchReadinessProviderをクリーンアップ
            UnsubscribeFromMatchReadinessProvider();

            // フラグをリセット
            _sceneLoadCompleted = false;
            _waitingForEntities = false;

            IsVisible = true;
            LoadingProgress = 0.0f;
            LoadingMessage = "Loading...";

            // Match/Training/Huntingシーンへの遷移の場合、エンティティ準備完了を待つ
            bool isGameplay = IsGameplayScene(e.SceneName);
            if (isGameplay)
            {
                _waitingForEntities = true;
                Debug.Log($"[LoadingScreenViewModel] Will wait for entities ready (gameplay scene: {e.SceneName})");
            }
        }

        /// <summary>
        /// ローディング完了時に呼ばれます
        /// </summary>
        private void OnLoadingCompleted(object? sender, SceneLoadEventArgs e)
        {
            Debug.Log($"[LoadingScreenViewModel] Scene load completed: {e.SceneName} ({e.Duration:F2}s)");

            _sceneLoadCompleted = true;

            if (_waitingForEntities)
            {
                // ゲームプレイシーンの場合、エンティティ準備完了を待つ
                LoadingMessage = "Initializing match...";
                LoadingProgress = 0.5f;
                _entityWaitStartTime = Time.realtimeSinceStartup;
                _lastProviderSearchTime = Time.realtimeSinceStartup; // 初回検索は遅延後

                Debug.Log("[LoadingScreenViewModel] Scene loaded. Now waiting for entities to be ready...");
                // ここでは非表示にしない - AllEntitiesReadyイベントを待つ
                // プロバイダー検索はUpdateEntityLoadProgressで行う（初期化完了を待つため）
            }
            else
            {
                // 通常のシーン遷移はすぐに完了
                LoadingProgress = 1.0f;
                LoadingMessage = "Complete!";
                IsVisible = false;
            }
        }

        /// <summary>
        /// すべてのエンティティの準備が完了した時に呼ばれます
        /// </summary>
        private void OnAllEntitiesReady()
        {
            Debug.Log("[LoadingScreenViewModel] All entities ready! Hiding loading screen.");
            _waitingForEntities = false;
            LoadingProgress = 1.0f;
            LoadingMessage = "Ready!";
            IsVisible = false;
        }

        /// <summary>
        /// ゲームプレイシーン（Match/Training/Hunting）かどうかを判定します
        /// </summary>
        private bool IsGameplayScene(string sceneName)
        {
            string lowerName = sceneName.ToLowerInvariant();
            return lowerName.Contains("match") ||
                   lowerName.Contains("training") ||
                   lowerName.Contains("hunting");
        }

        /// <summary>
        /// MatchReadinessProviderへの購読を試みます
        /// </summary>
        private void TrySubscribeToMatchReadinessProvider()
        {
            // 既に購読中なら何もしない
            if (_matchReadinessProvider != null)
            {
                return;
            }

            Debug.Log("[LoadingScreenViewModel] Searching for IMatchReadinessProvider...");

            // シーン内のIMatchReadinessProviderを実装するMonoBehaviourを探す
            // （MatchManagerなど）
            var allMonoBehaviours = Object.FindObjectsOfType<MonoBehaviour>();
            Debug.Log($"[LoadingScreenViewModel] Found {allMonoBehaviours.Length} MonoBehaviours in scene");

            foreach (var monoBehaviour in allMonoBehaviours)
            {
                if (monoBehaviour is IMatchReadinessProvider provider)
                {
                    _matchReadinessProvider = provider;
                    _matchReadinessProvider.AllEntitiesReady += OnAllEntitiesReady;
                    Debug.Log($"[LoadingScreenViewModel] Subscribed to {monoBehaviour.GetType().Name}'s AllEntitiesReady event. AreAllEntitiesReady={provider.AreAllEntitiesReady}");

                    // 既に準備完了している場合は即座に非表示
                    if (_matchReadinessProvider.AreAllEntitiesReady)
                    {
                        Debug.Log("[LoadingScreenViewModel] Entities already ready! Hiding loading screen.");
                        OnAllEntitiesReady();
                    }
                    else
                    {
                        Debug.Log($"[LoadingScreenViewModel] Waiting for AllEntitiesReady event. Progress={provider.EntityLoadProgress:P0}, Status={provider.LoadStatusMessage}");
                    }
                    return;
                }
            }

            // 見つからない場合はログを出力（タイムアウトでフォールバック）
            Debug.Log("[LoadingScreenViewModel] IMatchReadinessProvider not found yet. Will retry...");
        }

        /// <summary>
        /// MatchReadinessProviderからの購読を解除します
        /// </summary>
        private void UnsubscribeFromMatchReadinessProvider()
        {
            if (_matchReadinessProvider != null)
            {
                _matchReadinessProvider.AllEntitiesReady -= OnAllEntitiesReady;
                _matchReadinessProvider = null;
            }
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// ローディング画面を手動で表示します
        /// </summary>
        public void Show()
        {
            Debug.Log("[LoadingScreenViewModel] Showing loading screen.");
            IsVisible = true;
            LoadingProgress = 0.0f;
            LoadingMessage = "Loading...";
        }

        /// <summary>
        /// ローディング画面を手動で非表示にします
        /// </summary>
        public void Hide()
        {
            Debug.Log("[LoadingScreenViewModel] Hiding loading screen.");
            IsVisible = false;
        }

        /// <summary>
        /// 現在のローディング進捗を更新します
        /// </summary>
        /// <remarks>
        /// Viewから毎フレーム呼び出されることを想定しています。
        /// SceneManagementServiceからの進捗を取得して、UIに反映します。
        /// エンティティ準備待機中の場合は、MatchReadinessProviderの進捗も反映します。
        /// </remarks>
        public void UpdateProgress()
        {
            // シーンロード完了後、エンティティ準備待機中の場合
            if (_waitingForEntities && _sceneLoadCompleted)
            {
                // エンティティ準備待機中の場合、MatchReadinessProviderの進捗を使用
                UpdateEntityLoadProgress();
                return;
            }

            // シーンロード中（または非ゲームプレイシーン）
            if (_sceneManagementService == null)
            {
                return;
            }

            // サービスから現在の進捗を取得
            float progress = _sceneManagementService.LoadProgress;

            // 進捗が変わっていない場合はスキップ
            if (Mathf.Approximately(LoadingProgress, progress))
            {
                return;
            }

            LoadingProgress = progress;

            // 進捗に応じてメッセージを更新
            if (progress < 0.3f)
            {
                LoadingMessage = "Preparing scene...";
            }
            else if (progress < 0.7f)
            {
                LoadingMessage = "Loading assets...";
            }
            else if (progress < 0.95f)
            {
                LoadingMessage = "Finalizing...";
            }
            else
            {
                LoadingMessage = "Almost ready...";
            }
        }

        /// <summary>
        /// エンティティロード進捗を更新します（MatchReadinessProviderから）
        /// </summary>
        private void UpdateEntityLoadProgress()
        {
            float currentTime = Time.realtimeSinceStartup;
            float elapsedTime = currentTime - _entityWaitStartTime;

            // タイムアウトチェック
            if (elapsedTime >= ENTITY_WAIT_TIMEOUT)
            {
                Debug.LogWarning($"[LoadingScreenViewModel] Entity wait timeout ({ENTITY_WAIT_TIMEOUT}s). Force hiding loading screen.");
                _waitingForEntities = false;
                LoadingProgress = 1.0f;
                LoadingMessage = "Ready!";
                IsVisible = false;
                return;
            }

            // MatchReadinessProviderがない場合は定期的に検索を試みる
            if (_matchReadinessProvider == null)
            {
                // 初回検索は遅延後、その後は一定間隔で検索
                float timeSinceLastSearch = currentTime - _lastProviderSearchTime;
                bool shouldSearch = (elapsedTime >= INITIAL_SEARCH_DELAY) && (timeSinceLastSearch >= PROVIDER_SEARCH_INTERVAL);

                if (shouldSearch)
                {
                    _lastProviderSearchTime = currentTime;
                    TrySubscribeToMatchReadinessProvider();
                }
            }

            if (_matchReadinessProvider != null)
            {
                // MatchReadinessProviderの進捗とメッセージを反映
                float entityProgress = _matchReadinessProvider.EntityLoadProgress;
                string statusMessage = _matchReadinessProvider.LoadStatusMessage;

                // シーンロード（0-50%）+ エンティティロード（50-100%）で計算
                LoadingProgress = 0.5f + entityProgress * 0.5f;
                LoadingMessage = statusMessage;
            }
            else
            {
                // MatchReadinessProviderがまだ見つからない場合は待機中表示
                LoadingMessage = $"Initializing match... ({elapsedTime:F0}s)";
                // 進捗は緩やかに上げる（最大70%まで）
                LoadingProgress = Mathf.Min(0.5f + elapsedTime * 0.01f, 0.7f);
            }
        }

        #endregion

        #region Protected Methods

        /// <summary>
        /// リソースを解放します
        /// </summary>
        protected override void OnDispose()
        {
            // イベント購読解除
            if (_sceneManagementService != null)
            {
                _sceneManagementService.SceneLoadStarted -= OnLoadingStarted;
                _sceneManagementService.SceneLoadCompleted -= OnLoadingCompleted;
            }

            // MatchReadinessProvider購読解除
            UnsubscribeFromMatchReadinessProvider();

            base.OnDispose();
            Debug.Log("[LoadingScreenViewModel] ViewModel disposed.");
        }

        #endregion
    }
}
