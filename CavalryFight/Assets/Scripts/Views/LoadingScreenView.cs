#nullable enable

using CavalryFight.Core.MVVM;
using CavalryFight.ViewModels;
using UnityEngine;
using UnityEngine.UIElements;

namespace CavalryFight.Views
{
    /// <summary>
    /// ローディング画面のView
    /// </summary>
    /// <remarks>
    /// UI Toolkitを使用してローディング画面UIを表示します。
    /// LoadingScreenViewModelとバインドされ、ローディング進捗を表示します。
    /// </remarks>
    [RequireComponent(typeof(UIDocument))]
    public class LoadingScreenView : UIToolkitViewBase<LoadingScreenViewModel>
    {
        #region Fields

        private VisualElement? _rootContainer;
        private VisualElement? _backgroundTexture;
        private Label? _loadingMessageLabel;
        private VisualElement? _progressBar;
        private Label? _progressPercentageLabel;

        #endregion

        #region Unity Lifecycle

        /// <summary>
        /// 初期化処理
        /// </summary>
        protected override void Awake()
        {
            base.Awake();

            // ViewModelを作成してバインド
            ViewModel = new LoadingScreenViewModel();
        }

        /// <summary>
        /// 毎フレームの更新処理
        /// </summary>
        protected virtual void Update()
        {
            // ローディング中のみ進捗を更新
            if (ViewModel != null && ViewModel.IsVisible)
            {
                ViewModel.UpdateProgress();
            }
        }

        #endregion

        #region Protected Methods

        /// <summary>
        /// RootVisualElementが準備できた時に呼び出されます
        /// </summary>
        /// <param name="root">ルートVisualElement</param>
        protected override void OnRootVisualElementReady(VisualElement root)
        {
            base.OnRootVisualElementReady(root);

            // UI要素を取得
            _rootContainer = Q<VisualElement>("Root");
            _backgroundTexture = Q<VisualElement>("BackgroundTexture");
            _loadingMessageLabel = Q<Label>("LoadingMessageLabel");
            _progressBar = Q<VisualElement>("ProgressBar");
            _progressPercentageLabel = Q<Label>("ProgressPercentageLabel");

            // UI要素の検証
            ValidateUIElements();

            // 初期表示状態を設定
            UpdateVisibility();
        }

        /// <summary>
        /// ViewModelとのバインディングを設定します
        /// </summary>
        /// <param name="viewModel">バインドするViewModel</param>
        protected override void BindViewModel(LoadingScreenViewModel viewModel)
        {
            base.BindViewModel(viewModel);

            // PropertyChangedイベントを購読
            viewModel.PropertyChanged += OnViewModelPropertyChanged;

            // 初期値を反映
            UpdateUIFromViewModel();
        }

        /// <summary>
        /// ViewModelとのバインディングを解除します
        /// </summary>
        protected override void UnbindViewModel()
        {
            // イベント購読解除
            if (ViewModel != null)
            {
                ViewModel.PropertyChanged -= OnViewModelPropertyChanged;
            }

            base.UnbindViewModel();
        }

        #endregion

        #region Private Methods

        /// <summary>
        /// UI要素が正しく取得できているか検証します
        /// </summary>
        private void ValidateUIElements()
        {
            if (_rootContainer == null)
            {
                Debug.LogWarning("[LoadingScreenView] Root not found in UXML.", this);
            }

            if (_backgroundTexture == null)
            {
                Debug.LogWarning("[LoadingScreenView] BackgroundTexture not found in UXML.", this);
            }

            if (_loadingMessageLabel == null)
            {
                Debug.LogWarning("[LoadingScreenView] LoadingMessageLabel not found in UXML.", this);
            }

            if (_progressBar == null)
            {
                Debug.LogWarning("[LoadingScreenView] ProgressBar not found in UXML.", this);
            }

            if (_progressPercentageLabel == null)
            {
                Debug.LogWarning("[LoadingScreenView] ProgressPercentageLabel not found in UXML.", this);
            }
        }

        /// <summary>
        /// ViewModelの状態からUIを更新します
        /// </summary>
        private void UpdateUIFromViewModel()
        {
            if (ViewModel == null)
            {
                return;
            }

            UpdateVisibility();
            UpdateLoadingMessage();
            UpdateProgressBar();
            UpdateBackgroundColor();
        }

        /// <summary>
        /// 表示/非表示を更新します
        /// </summary>
        private void UpdateVisibility()
        {
            if (_rootContainer == null || ViewModel == null)
            {
                return;
            }

            if (ViewModel.IsVisible)
            {
                _rootContainer.style.display = DisplayStyle.Flex;
            }
            else
            {
                _rootContainer.style.display = DisplayStyle.None;
            }
        }

        /// <summary>
        /// ローディングメッセージを更新します
        /// </summary>
        private void UpdateLoadingMessage()
        {
            if (_loadingMessageLabel == null || ViewModel == null)
            {
                return;
            }

            _loadingMessageLabel.text = ViewModel.LoadingMessage;
        }

        /// <summary>
        /// プログレスバーを更新します
        /// </summary>
        private void UpdateProgressBar()
        {
            if (ViewModel == null)
            {
                return;
            }

            // プログレスバーの幅を更新
            if (_progressBar != null)
            {
                float percentage = Mathf.Clamp01(ViewModel.LoadingProgress) * 100f;
                _progressBar.style.width = Length.Percent(percentage);
            }

            // パーセンテージテキストを更新
            if (_progressPercentageLabel != null)
            {
                int percentage = Mathf.RoundToInt(Mathf.Clamp01(ViewModel.LoadingProgress) * 100f);
                _progressPercentageLabel.text = $"{percentage}%";
            }
        }

        /// <summary>
        /// 背景色を進捗に応じて更新します（黒から白へ）
        /// </summary>
        private void UpdateBackgroundColor()
        {
            if (_backgroundTexture == null || ViewModel == null)
            {
                return;
            }

            // 進捗に応じて黒(0,0,0)から白(255,255,255)へ補間
            float progress = Mathf.Clamp01(ViewModel.LoadingProgress);
            Color color = Color.Lerp(Color.black, Color.white, progress);

            _backgroundTexture.style.backgroundColor = color;
        }

        #endregion

        #region Event Handlers

        /// <summary>
        /// ViewModelのプロパティ変更イベントを処理します
        /// </summary>
        private void OnViewModelPropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            if (ViewModel == null)
            {
                return;
            }

            switch (e.PropertyName)
            {
                case nameof(LoadingScreenViewModel.IsVisible):
                    UpdateVisibility();
                    break;

                case nameof(LoadingScreenViewModel.LoadingMessage):
                    UpdateLoadingMessage();
                    break;

                case nameof(LoadingScreenViewModel.LoadingProgress):
                    UpdateProgressBar();
                    UpdateBackgroundColor();
                    break;

                case "":
                    // すべてのプロパティが変更された
                    UpdateUIFromViewModel();
                    break;
            }
        }

        #endregion
    }
}
