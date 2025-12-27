#nullable enable

using CavalryFight.Core.MVVM;
using CavalryFight.Core.Services;
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
    /// </remarks>
    public class LoadingScreenViewModel : ViewModelBase
    {
        #region Fields

        private readonly ISceneManagementService? _sceneManagementService;
        private float _loadingProgress;
        private string _loadingMessage = "Loading...";
        private bool _isVisible;

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
            IsVisible = true;
            LoadingProgress = 0.0f;
            LoadingMessage = "Loading...";
        }

        /// <summary>
        /// ローディング完了時に呼ばれます
        /// </summary>
        private void OnLoadingCompleted(object? sender, SceneLoadEventArgs e)
        {
            Debug.Log($"[LoadingScreenViewModel] Loading completed: {e.SceneName} ({e.Duration:F2}s)");
            LoadingProgress = 1.0f;
            LoadingMessage = "Complete!";
            IsVisible = false;
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
        /// </remarks>
        public void UpdateProgress()
        {
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

            base.OnDispose();
            Debug.Log("[LoadingScreenViewModel] ViewModel disposed.");
        }

        #endregion
    }
}
