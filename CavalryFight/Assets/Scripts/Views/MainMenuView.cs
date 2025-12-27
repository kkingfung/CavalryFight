#nullable enable

using CavalryFight.Core.MVVM;
using CavalryFight.Core.Services;
using CavalryFight.Services.Audio;
using CavalryFight.ViewModels;
using UnityEngine;
using UnityEngine.UIElements;

namespace CavalryFight.Views
{
    /// <summary>
    /// メインメニュー画面のView
    /// </summary>
    /// <remarks>
    /// UI Toolkitを使用してメインメニューUIを表示します。
    /// MainMenuViewModelとバインドされ、ユーザー操作を処理します。
    /// </remarks>
    [RequireComponent(typeof(UIDocument))]
    public class MainMenuView : UIToolkitViewBase<MainMenuViewModel>
    {
        #region Serialized Fields

        [Header("Audio")]
        [SerializeField] private AudioClip? _bgmClip;
        [SerializeField] private AudioClip? _buttonClickSfx;

        #endregion

        #region Fields

        private Button? _startTrainingButton;
        private Button? _matchLobbyButton;
        private Button? _replayButton;
        private Button? _customizationButton;
        private Button? _settingsButton;
        private Button? _quitButton;
        private Label? _titleLabel;
        private Label? _subtitleLabel;

        #endregion

        #region Unity Lifecycle

        /// <summary>
        /// 初期化処理
        /// </summary>
        protected override void Awake()
        {
            base.Awake();

            // ViewModelを作成してバインド
            ViewModel = new MainMenuViewModel();
        }

        /// <summary>
        /// 有効化時の処理
        /// </summary>
        protected override void OnEnable()
        {
            base.OnEnable();

            // BGMを再生
            if (_bgmClip != null)
            {
                var audioService = ServiceLocator.Instance.Get<IAudioService>();
                if (audioService != null)
                {
                    audioService.PlayBgm(_bgmClip, loop: true, fadeInDuration: 2f);
                }
            }
        }

        /// <summary>
        /// 無効化時の処理
        /// </summary>
        protected override void OnDisable()
        {
            // BGMは停止しない（シーン遷移時の継続再生のため）
            // 次のシーンが異なるBGMを要求する場合は、そのシーンのOnEnable()で自動的に切り替わる
            base.OnDisable();
        }

        #endregion

        #region Protected Methods

        /// <summary>
        /// RootVisualElementが準備できた時に呼び出されます。
        /// </summary>
        /// <param name="root">ルートVisualElement</param>
        protected override void OnRootVisualElementReady(VisualElement root)
        {
            base.OnRootVisualElementReady(root);

            // UI要素を取得
            _titleLabel = Q<Label>("TitleLabel");
            _subtitleLabel = Q<Label>("SubtitleLabel");
            _startTrainingButton = Q<Button>("StartTrainingButton");
            _matchLobbyButton = Q<Button>("MatchLobbyButton");
            _replayButton = Q<Button>("ReplayButton");
            _customizationButton = Q<Button>("CustomizationButton");
            _settingsButton = Q<Button>("SettingsButton");
            _quitButton = Q<Button>("QuitButton");

            // UI要素の検証
            ValidateUIElements();

            // イベントハンドラを登録
            RegisterEventHandlers();
        }

        /// <summary>
        /// ViewModelとのバインディングを設定します。
        /// </summary>
        /// <param name="viewModel">バインドするViewModel</param>
        protected override void BindViewModel(MainMenuViewModel viewModel)
        {
            base.BindViewModel(viewModel);

            // プロパティバインディング
            if (_titleLabel != null)
            {
                _titleLabel.text = viewModel.Title;
            }

            if (_subtitleLabel != null)
            {
                _subtitleLabel.text = viewModel.Subtitle;
            }

            // PropertyChangedイベントを購読
            viewModel.PropertyChanged += OnViewModelPropertyChanged;
        }

        /// <summary>
        /// ViewModelとのバインディングを解除します。
        /// </summary>
        protected override void UnbindViewModel()
        {
            // イベント購読解除
            if (ViewModel != null)
            {
                ViewModel.PropertyChanged -= OnViewModelPropertyChanged;
            }

            // イベントハンドラを解除
            UnregisterEventHandlers();

            base.UnbindViewModel();
        }

        #endregion

        #region Private Methods

        /// <summary>
        /// UI要素が正しく取得できているか検証します。
        /// </summary>
        private void ValidateUIElements()
        {
            if (_titleLabel == null)
            {
                Debug.LogWarning("[MainMenuView] TitleLabel not found in UXML.", this);
            }

            if (_subtitleLabel == null)
            {
                Debug.LogWarning("[MainMenuView] SubtitleLabel not found in UXML.", this);
            }

            if (_startTrainingButton == null)
            {
                Debug.LogWarning("[MainMenuView] StartTrainingButton not found in UXML.", this);
            }

            if (_matchLobbyButton == null)
            {
                Debug.LogWarning("[MainMenuView] MatchLobbyButton not found in UXML.", this);
            }

            if (_replayButton == null)
            {
                Debug.LogWarning("[MainMenuView] ReplayButton not found in UXML.", this);
            }

            if (_customizationButton == null)
            {
                Debug.LogWarning("[MainMenuView] CustomizationButton not found in UXML.", this);
            }

            if (_settingsButton == null)
            {
                Debug.LogWarning("[MainMenuView] SettingsButton not found in UXML.", this);
            }

            if (_quitButton == null)
            {
                Debug.LogWarning("[MainMenuView] QuitButton not found in UXML.", this);
            }
        }

        /// <summary>
        /// イベントハンドラを登録します。
        /// </summary>
        private void RegisterEventHandlers()
        {
            if (_startTrainingButton != null)
            {
                _startTrainingButton.clicked += OnStartTrainingButtonClicked;
            }

            if (_matchLobbyButton != null)
            {
                _matchLobbyButton.clicked += OnMatchLobbyButtonClicked;
            }

            if (_replayButton != null)
            {
                _replayButton.clicked += OnReplayButtonClicked;
            }

            if (_customizationButton != null)
            {
                _customizationButton.clicked += OnCustomizationButtonClicked;
            }

            if (_settingsButton != null)
            {
                _settingsButton.clicked += OnSettingsButtonClicked;
            }

            if (_quitButton != null)
            {
                _quitButton.clicked += OnQuitButtonClicked;
            }
        }

        /// <summary>
        /// イベントハンドラを解除します。
        /// </summary>
        private void UnregisterEventHandlers()
        {
            if (_startTrainingButton != null)
            {
                _startTrainingButton.clicked -= OnStartTrainingButtonClicked;
            }

            if (_matchLobbyButton != null)
            {
                _matchLobbyButton.clicked -= OnMatchLobbyButtonClicked;
            }

            if (_replayButton != null)
            {
                _replayButton.clicked -= OnReplayButtonClicked;
            }

            if (_customizationButton != null)
            {
                _customizationButton.clicked -= OnCustomizationButtonClicked;
            }

            if (_settingsButton != null)
            {
                _settingsButton.clicked -= OnSettingsButtonClicked;
            }

            if (_quitButton != null)
            {
                _quitButton.clicked -= OnQuitButtonClicked;
            }
        }

        #endregion

        #region Event Handlers

        /// <summary>
        /// ViewModelのプロパティ変更イベントを処理します。
        /// </summary>
        private void OnViewModelPropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            if (ViewModel == null)
            {
                return;
            }

            switch (e.PropertyName)
            {
                case nameof(MainMenuViewModel.Title):
                    if (_titleLabel != null)
                    {
                        _titleLabel.text = ViewModel.Title;
                    }
                    break;

                case nameof(MainMenuViewModel.Subtitle):
                    if (_subtitleLabel != null)
                    {
                        _subtitleLabel.text = ViewModel.Subtitle;
                    }
                    break;
            }
        }

        /// <summary>
        /// トレーニング開始ボタンがクリックされた時の処理
        /// </summary>
        private void OnStartTrainingButtonClicked()
        {
            PlayButtonClickSfx();
            ViewModel?.StartTrainingCommand.Execute(null);
        }

        /// <summary>
        /// マッチロビーボタンがクリックされた時の処理
        /// </summary>
        private void OnMatchLobbyButtonClicked()
        {
            PlayButtonClickSfx();
            ViewModel?.OpenMatchLobbyCommand.Execute(null);
        }

        /// <summary>
        /// リプレイ履歴ボタンがクリックされた時の処理
        /// </summary>
        private void OnReplayButtonClicked()
        {
            PlayButtonClickSfx();
            ViewModel?.OpenReplayHistoryCommand.Execute(null);
        }

        /// <summary>
        /// カスタマイゼーションボタンがクリックされた時の処理
        /// </summary>
        private void OnCustomizationButtonClicked()
        {
            PlayButtonClickSfx();
            ViewModel?.OpenCustomizationCommand.Execute(null);
        }

        /// <summary>
        /// 設定ボタンがクリックされた時の処理
        /// </summary>
        private void OnSettingsButtonClicked()
        {
            PlayButtonClickSfx();
            ViewModel?.OpenSettingsCommand.Execute(null);
        }

        /// <summary>
        /// 終了ボタンがクリックされた時の処理
        /// </summary>
        private void OnQuitButtonClicked()
        {
            PlayButtonClickSfx();
            ViewModel?.QuitGameCommand.Execute(null);
        }

        #endregion

        #region Private Methods - Audio

        /// <summary>
        /// ボタンクリック効果音を再生します
        /// </summary>
        private void PlayButtonClickSfx()
        {
            if (_buttonClickSfx != null)
            {
                var audioService = ServiceLocator.Instance.Get<IAudioService>();
                if (audioService != null)
                {
                    audioService.PlaySfx(_buttonClickSfx);
                }
            }
        }

        #endregion
    }
}
