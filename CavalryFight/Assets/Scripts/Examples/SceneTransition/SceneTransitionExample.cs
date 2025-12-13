#nullable enable

using CavalryFight.Core.MVVM;
using CavalryFight.Core.Commands;
using CavalryFight.Core.Services;
using CavalryFight.Services.SceneManagement;
using AdvancedSceneManager.Models;
using UnityEngine;

namespace CavalryFight.Examples.SceneTransition
{
    /// <summary>
    /// シーン遷移のサンプルViewModel
    /// </summary>
    /// <remarks>
    /// Advanced Scene ManagerとMVVMシステムを使用した
    /// シーン遷移の実装例です。
    /// </remarks>
    public class SceneTransitionExampleViewModel : ViewModelBase
    {
        #region Fields

        private readonly ISceneManagementService _sceneService;
        private string _currentSceneName;
        private string _statusMessage;
        private float _loadProgress;

        #endregion

        #region Properties

        /// <summary>
        /// 現在のシーン名を取得します。
        /// </summary>
        public string CurrentSceneName
        {
            get => _currentSceneName;
            private set => SetProperty(ref _currentSceneName, value);
        }

        /// <summary>
        /// ステータスメッセージを取得します。
        /// </summary>
        public string StatusMessage
        {
            get => _statusMessage;
            private set => SetProperty(ref _statusMessage, value);
        }

        /// <summary>
        /// ロード進捗を取得します（0.0～1.0）
        /// </summary>
        public float LoadProgress
        {
            get => _loadProgress;
            private set => SetProperty(ref _loadProgress, value);
        }

        /// <summary>
        /// ロード中かどうかを取得します。
        /// </summary>
        public bool IsLoading => _sceneService.IsLoading;

        #endregion

        #region Commands

        /// <summary>
        /// シーンAを開くコマンド
        /// </summary>
        public ICommand OpenSceneACommand { get; }

        /// <summary>
        /// シーンBを開くコマンド
        /// </summary>
        public ICommand OpenSceneBCommand { get; }

        /// <summary>
        /// シーンコレクションを開くコマンド
        /// </summary>
        public ICommand OpenCollectionCommand { get; }

        /// <summary>
        /// すべてのシーンを閉じるコマンド
        /// </summary>
        public ICommand CloseAllCommand { get; }

        #endregion

        #region Constructor

        /// <summary>
        /// SceneTransitionExampleViewModelの新しいインスタンスを初期化します。
        /// </summary>
        public SceneTransitionExampleViewModel()
        {
            // サービスを取得
            _sceneService = ServiceLocator.Instance.Get<ISceneManagementService>();

            _currentSceneName = "None";
            _statusMessage = "Ready";
            _loadProgress = 0f;

            // イベントを購読
            _sceneService.SceneLoadStarted += OnSceneLoadStarted;
            _sceneService.SceneLoadCompleted += OnSceneLoadCompleted;
            _sceneService.SceneLoadFailed += OnSceneLoadFailed;

            // コマンドの初期化
            OpenSceneACommand = new RelayCommand(
                execute: () => OpenScene("SceneA"),
                canExecute: () => !IsLoading
            );

            OpenSceneBCommand = new RelayCommand(
                execute: () => OpenScene("SceneB"),
                canExecute: () => !IsLoading
            );

            OpenCollectionCommand = new RelayCommand(
                execute: OpenCollection,
                canExecute: () => !IsLoading
            );

            CloseAllCommand = new RelayCommand(
                execute: CloseAllScenes,
                canExecute: () => !IsLoading
            );
        }

        #endregion

        #region Private Methods

        /// <summary>
        /// シーンを開きます。
        /// </summary>
        /// <param name="sceneName">開くシーンの名前</param>
        private void OpenScene(string sceneName)
        {
            // Resourcesフォルダからシーンを読み込む
            // 実際のプロジェクトでは、ScriptableObjectで管理することを推奨
            var scene = Resources.Load<Scene>($"Scenes/{sceneName}");

            if (scene != null)
            {
                StatusMessage = $"Opening {sceneName}...";
                _sceneService.OpenScene(scene, useLoadingScreen: true);
            }
            else
            {
                StatusMessage = $"Scene {sceneName} not found!";
                Debug.LogWarning($"[SceneTransitionExample] Scene not found: {sceneName}");
            }
        }

        /// <summary>
        /// シーンコレクションを開きます。
        /// </summary>
        private void OpenCollection()
        {
            var collection = Resources.Load<SceneCollection>("SceneCollections/ExampleCollection");

            if (collection != null)
            {
                StatusMessage = "Opening collection...";
                _sceneService.OpenCollection(collection, openAll: true);
            }
            else
            {
                StatusMessage = "Collection not found!";
                Debug.LogWarning("[SceneTransitionExample] Collection not found");
            }
        }

        /// <summary>
        /// すべてのシーンを閉じます。
        /// </summary>
        private void CloseAllScenes()
        {
            StatusMessage = "Closing all scenes...";
            _sceneService.CloseAll();

            CurrentSceneName = "None";
            StatusMessage = "All scenes closed.";
        }

        /// <summary>
        /// シーンロード開始時のイベントハンドラ
        /// </summary>
        /// <param name="sender">イベント送信元</param>
        /// <param name="e">シーンロードイベントの引数</param>
        private void OnSceneLoadStarted(object? sender, SceneLoadEventArgs e)
        {
            CurrentSceneName = e.SceneName;
            StatusMessage = $"Loading {e.SceneName}...";
            LoadProgress = 0f;

            OnPropertyChanged(nameof(IsLoading));
            RaiseCommandsCanExecuteChanged();

            Debug.Log($"[SceneTransitionExample] Load started: {e.SceneName}");
        }

        /// <summary>
        /// シーンロード完了時のイベントハンドラ
        /// </summary>
        /// <param name="sender">イベント送信元</param>
        /// <param name="e">シーンロードイベントの引数</param>
        private void OnSceneLoadCompleted(object? sender, SceneLoadEventArgs e)
        {
            CurrentSceneName = e.SceneName;
            StatusMessage = $"Loaded {e.SceneName} in {e.Duration:F2}s";
            LoadProgress = 1f;

            OnPropertyChanged(nameof(IsLoading));
            RaiseCommandsCanExecuteChanged();

            Debug.Log($"[SceneTransitionExample] Load completed: {e.SceneName} ({e.Duration:F2}s)");
        }

        /// <summary>
        /// シーンロード失敗時のイベントハンドラ
        /// </summary>
        /// <param name="sender">イベント送信元</param>
        /// <param name="e">シーンロードエラーイベントの引数</param>
        private void OnSceneLoadFailed(object? sender, SceneLoadErrorEventArgs e)
        {
            StatusMessage = $"Failed to load {e.SceneName}: {e.ErrorMessage}";
            LoadProgress = 0f;

            OnPropertyChanged(nameof(IsLoading));
            RaiseCommandsCanExecuteChanged();

            Debug.LogError($"[SceneTransitionExample] Load failed: {e.SceneName} - {e.ErrorMessage}");
        }

        /// <summary>
        /// すべてのコマンドのCanExecuteChangedを発行します。
        /// </summary>
        private void RaiseCommandsCanExecuteChanged()
        {
            (OpenSceneACommand as RelayCommand)?.RaiseCanExecuteChanged();
            (OpenSceneBCommand as RelayCommand)?.RaiseCanExecuteChanged();
            (OpenCollectionCommand as RelayCommand)?.RaiseCanExecuteChanged();
            (CloseAllCommand as RelayCommand)?.RaiseCanExecuteChanged();
        }

        #endregion

        #region Protected Methods

        /// <summary>
        /// ViewModelの破棄処理
        /// </summary>
        protected override void OnDispose()
        {
            // イベントの購読解除
            _sceneService.SceneLoadStarted -= OnSceneLoadStarted;
            _sceneService.SceneLoadCompleted -= OnSceneLoadCompleted;
            _sceneService.SceneLoadFailed -= OnSceneLoadFailed;

            Debug.Log("[SceneTransitionExample] ViewModel disposed.");
            base.OnDispose();
        }

        #endregion
    }
}
