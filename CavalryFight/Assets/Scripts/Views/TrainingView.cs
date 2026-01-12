#nullable enable

using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UIElements;
using CavalryFight.Core.MVVM;
using CavalryFight.Core.Services;
using CavalryFight.Services.Audio;
using CavalryFight.Services.GameSettings;
using CavalryFight.Services.Input;
using CavalryFight.Services.SceneManagement;
using CavalryFight.Services.Training;
using CavalryFight.ViewModels;

namespace CavalryFight.Views
{
    /// <summary>
    /// トレーニングシーンのView
    /// </summary>
    /// <remarks>
    /// HUDの表示とポーズメニューの制御を行います。
    /// </remarks>
    [RequireComponent(typeof(UIDocument))]
    public class TrainingView : UIToolkitViewBase<TrainingViewModel>
    {
        #region Serialized Fields

        [Header("Audio")]
        [SerializeField] private AudioClip? _bgmClip;
        [SerializeField] private AudioClip? _buttonClickSfx;

        [Header("Score Popup")]
        [SerializeField] private GameObject? _scorePopupPrefab;

        [Header("Key Bindings")]
        [SerializeField] private KeyBindingView? _keyBindingView;

        #endregion

        #region Private Fields

        private IAudioService? _audioService;
        private IInputService? _inputService;
        private IGameSettingsService? _gameSettingsService;
        private SettingsViewModel? _settingsViewModel;
        private Label? _scoreLabel;
        private Label? _arrowsFiredLabel;
        private Label? _hitsLabel;
        private Label? _accuracyLabel;

        // Crosshair
        private VisualElement? _crosshair;

        // Settings/Pause Popup
        private VisualElement? _pauseSettingsPopup;
        private Button? _resumeButton;
        private Button? _backToMenuButton;

        // Audio
        private Slider? _masterVolumeSlider;
        private Slider? _bgmVolumeSlider;
        private Slider? _sfxVolumeSlider;

        // Video
        private DropdownField? _displayModeDropdown;
        private DropdownField? _resolutionDropdown;
        private DropdownField? _qualityDropdown;
        private Toggle? _vSyncToggle;
        private DropdownField? _targetFpsDropdown;
        private DropdownField? _antiAliasingDropdown;

        // Gameplay
        private Slider? _cameraSensitivitySlider;
        private Toggle? _invertYAxisToggle;

        // Settings Buttons
        private Button? _keyBindingsButton;
        private Button? _applySettingsButton;
        private Button? _resetSettingsButton;

        #endregion

        #region Unity Lifecycle

        protected override void Awake()
        {
            base.Awake();

            // サービス取得
            _audioService = ServiceLocator.Instance.Get<IAudioService>();
            _inputService = ServiceLocator.Instance.Get<IInputService>();
            _gameSettingsService = ServiceLocator.Instance.Get<IGameSettingsService>();
            var sceneService = ServiceLocator.Instance.Get<ISceneManagementService>();

            if (sceneService == null)
            {
                Debug.LogError("[TrainingView] ISceneManagementService が取得できませんでした！");
                return;
            }

            // ViewModel作成
            ViewModel = new TrainingViewModel(sceneService);

            // 設定用ViewModel作成
            _settingsViewModel = new SettingsViewModel();
        }

        protected override void OnEnable()
        {
            base.OnEnable();

            // BGMを再生
            if (_bgmClip != null && _audioService != null)
            {
                _audioService.PlayBgm(_bgmClip, loop: true, fadeInDuration: 2f);
            }

            // TrainingManagerイベント購読
            SubscribeToTrainingManager();
        }

        private void Start()
        {
            // OnEnableでTrainingManagerがまだ初期化されていない場合に備えて再購読
            SubscribeToTrainingManager();
        }

        /// <summary>
        /// TrainingManagerのイベントを購読します
        /// </summary>
        private void SubscribeToTrainingManager()
        {
            if (TrainingManager.Instance != null)
            {
                // 重複購読を防ぐために先に解除
                TrainingManager.Instance.ArrowFired -= OnArrowFired;
                TrainingManager.Instance.TargetHit -= OnTargetHit;
                TrainingManager.Instance.ScoreEarned -= OnScoreEarned;
                TrainingManager.Instance.ChargingStarted -= OnChargingStarted;
                TrainingManager.Instance.ChargingEnded -= OnChargingEnded;

                // 購読
                TrainingManager.Instance.ArrowFired += OnArrowFired;
                TrainingManager.Instance.TargetHit += OnTargetHit;
                TrainingManager.Instance.ScoreEarned += OnScoreEarned;
                TrainingManager.Instance.ChargingStarted += OnChargingStarted;
                TrainingManager.Instance.ChargingEnded += OnChargingEnded;

                Debug.Log("[TrainingView] Subscribed to TrainingManager events");
            }
            else
            {
                Debug.LogWarning("[TrainingView] TrainingManager.Instance is null, cannot subscribe to events");
            }
        }

        protected override void OnDisable()
        {
            // TrainingManagerイベント購読解除
            if (TrainingManager.Instance != null)
            {
                TrainingManager.Instance.ArrowFired -= OnArrowFired;
                TrainingManager.Instance.TargetHit -= OnTargetHit;
                TrainingManager.Instance.ScoreEarned -= OnScoreEarned;
                TrainingManager.Instance.ChargingStarted -= OnChargingStarted;
                TrainingManager.Instance.ChargingEnded -= OnChargingEnded;
            }

            // シーン離脱時にTime.timeScaleを必ずリセット（ポーズ中でも）
            Time.timeScale = 1f;

            // 入力を有効化
            if (_inputService != null)
            {
                _inputService.InputEnabled = true;
            }

            // BGMは停止しない（シーン遷移時の継続再生のため）
            // 次のシーンが異なるBGMを要求する場合は、そのシーンのOnEnable()で自動的に切り替わる
            base.OnDisable();
        }

        private void Update()
        {
            if (_inputService == null || ViewModel == null)
            {
                return;
            }

            // ポーズボタンチェック（ESCキーを直接チェックもフォールバックとして追加）
            bool menuButtonPressed = _inputService.GetMenuButtonDown();
            bool escapePressed = UnityEngine.Input.GetKeyDown(KeyCode.Escape);

            if (menuButtonPressed || escapePressed)
            {
                // KeyBindingViewが表示されている場合は、KeyBindingViewを閉じる
                if (_keyBindingView != null && _keyBindingView.IsVisible)
                {
                    _keyBindingView.Hide();
                    return;
                }

                ViewModel.TogglePause();
            }
        }

        #endregion

        #region UIToolkitViewBase Overrides

        protected override void OnRootVisualElementReady(VisualElement root)
        {
            base.OnRootVisualElementReady(root);

            GetUIElements();
            RegisterEventHandlers();
            UpdateUIFromViewModel();

            // 初期状態ではポーズパネルを非表示（UXMLで既にdisplay:noneが設定されている）
        }

        protected override void BindViewModel(TrainingViewModel viewModel)
        {
            base.BindViewModel(viewModel);

            if (viewModel != null)
            {
                viewModel.PropertyChanged += OnViewModelPropertyChanged;
                viewModel.PauseStateChanged += OnPauseStateChanged;
            }
        }

        protected override void UnbindViewModel()
        {
            if (ViewModel != null)
            {
                ViewModel.PropertyChanged -= OnViewModelPropertyChanged;
                ViewModel.PauseStateChanged -= OnPauseStateChanged;
            }

            UnregisterEventHandlers();
            base.UnbindViewModel();
        }

        #endregion

        #region UI Setup

        /// <summary>
        /// UI要素を取得します
        /// </summary>
        private void GetUIElements()
        {
            if (RootVisualElement == null)
            {
                return;
            }

            // HUD
            _scoreLabel = Q<Label>("ScoreLabel");
            _arrowsFiredLabel = Q<Label>("ArrowsFiredLabel");
            _hitsLabel = Q<Label>("HitsLabel");
            _accuracyLabel = Q<Label>("AccuracyLabel");

            // Crosshair
            _crosshair = Q<VisualElement>("Crosshair");
            if (_crosshair != null)
            {
                Debug.Log("[TrainingView] Crosshair element found.");
            }

            // CrosshairContainerはマウスイベントを無視（UIの下のゲームをクリック可能に）
            var crosshairContainer = Q<VisualElement>("CrosshairContainer");
            if (crosshairContainer != null)
            {
                crosshairContainer.pickingMode = PickingMode.Ignore;
            }

            // Settings Popup (shown when paused)
            _pauseSettingsPopup = Q<VisualElement>("SettingsPopup");
            Debug.Log($"[TrainingView] SettingsPopup found: {_pauseSettingsPopup != null}");

            // SettingsPopup内から全てのUIを取得
            if (_pauseSettingsPopup != null)
            {
                // Footer Buttons（Resume, Back to Menu）
                _resumeButton = _pauseSettingsPopup.Q<Button>("ResumeButton");
                _backToMenuButton = _pauseSettingsPopup.Q<Button>("BackToMenuButton");

                // Audio Settings
                _masterVolumeSlider = _pauseSettingsPopup.Q<Slider>("MasterVolumeSlider");
                _bgmVolumeSlider = _pauseSettingsPopup.Q<Slider>("BgmVolumeSlider");
                _sfxVolumeSlider = _pauseSettingsPopup.Q<Slider>("SfxVolumeSlider");

                // Video Settings
                _displayModeDropdown = _pauseSettingsPopup.Q<DropdownField>("DisplayModeDropdown");
                _resolutionDropdown = _pauseSettingsPopup.Q<DropdownField>("ResolutionDropdown");
                _qualityDropdown = _pauseSettingsPopup.Q<DropdownField>("QualityDropdown");
                _vSyncToggle = _pauseSettingsPopup.Q<Toggle>("VSyncToggle");
                _targetFpsDropdown = _pauseSettingsPopup.Q<DropdownField>("TargetFpsDropdown");
                _antiAliasingDropdown = _pauseSettingsPopup.Q<DropdownField>("AntiAliasingDropdown");

                // Gameplay Settings
                _cameraSensitivitySlider = _pauseSettingsPopup.Q<Slider>("CameraSensitivitySlider");
                _invertYAxisToggle = _pauseSettingsPopup.Q<Toggle>("InvertYAxisToggle");

                // Settings Buttons
                _keyBindingsButton = _pauseSettingsPopup.Q<Button>("KeyBindingsButton");
                _applySettingsButton = _pauseSettingsPopup.Q<Button>("ApplySettingsButton");
                _resetSettingsButton = _pauseSettingsPopup.Q<Button>("ResetSettingsButton");

                Debug.Log($"[TrainingView] ResumeButton found: {_resumeButton != null}");
                Debug.Log($"[TrainingView] BackToMenuButton found: {_backToMenuButton != null}");
                Debug.Log($"[TrainingView] MasterVolumeSlider found: {_masterVolumeSlider != null}");
                Debug.Log($"[TrainingView] ApplySettingsButton found: {_applySettingsButton != null}");
            }
            else
            {
                Debug.LogError("[TrainingView] SettingsPopup not found! Buttons and settings will not work.");
            }
        }

        /// <summary>
        /// イベントハンドラを登録します
        /// </summary>
        private void RegisterEventHandlers()
        {
            Debug.Log("[TrainingView] Registering event handlers...");

            // Settings/Pause Popup Buttons
            if (_resumeButton != null)
            {
                _resumeButton.clicked += OnResumeClicked;
                Debug.Log("[TrainingView] ResumeButton event handler registered.");
            }
            else
            {
                Debug.LogWarning("[TrainingView] ResumeButton is null, cannot register handler!");
            }

            if (_backToMenuButton != null)
            {
                _backToMenuButton.clicked += OnBackToMenuClicked;
                Debug.Log("[TrainingView] BackToMenuButton event handler registered.");
            }
            else
            {
                Debug.LogWarning("[TrainingView] BackToMenuButton is null, cannot register handler!");
            }

            // Settings Buttons
            if (_keyBindingsButton != null)
            {
                _keyBindingsButton.clicked += OnKeyBindingsClicked;
            }

            if (_applySettingsButton != null)
            {
                _applySettingsButton.clicked += OnApplySettingsClicked;
                Debug.Log("[TrainingView] ApplySettingsButton event handler registered.");
            }
            else
            {
                Debug.LogWarning("[TrainingView] ApplySettingsButton is null, cannot register handler!");
            }

            if (_resetSettingsButton != null)
            {
                _resetSettingsButton.clicked += OnResetSettingsClicked;
            }

            // Audio Settings
            if (_masterVolumeSlider != null)
            {
                _masterVolumeSlider.RegisterValueChangedCallback(OnMasterVolumeChanged);
            }

            if (_bgmVolumeSlider != null)
            {
                _bgmVolumeSlider.RegisterValueChangedCallback(OnBgmVolumeChanged);
            }

            if (_sfxVolumeSlider != null)
            {
                _sfxVolumeSlider.RegisterValueChangedCallback(OnSfxVolumeChanged);
            }

            // Video Settings
            if (_displayModeDropdown != null)
            {
                _displayModeDropdown.RegisterValueChangedCallback(OnDisplayModeChanged);
            }

            if (_resolutionDropdown != null)
            {
                _resolutionDropdown.RegisterValueChangedCallback(OnResolutionChanged);
            }

            if (_qualityDropdown != null)
            {
                _qualityDropdown.RegisterValueChangedCallback(OnQualityChanged);
            }

            if (_vSyncToggle != null)
            {
                _vSyncToggle.RegisterValueChangedCallback(OnVSyncChanged);
            }

            if (_targetFpsDropdown != null)
            {
                _targetFpsDropdown.RegisterValueChangedCallback(OnTargetFpsChanged);
            }

            if (_antiAliasingDropdown != null)
            {
                _antiAliasingDropdown.RegisterValueChangedCallback(OnAntiAliasingChanged);
            }

            // Gameplay Settings
            if (_cameraSensitivitySlider != null)
            {
                _cameraSensitivitySlider.RegisterValueChangedCallback(OnCameraSensitivityChanged);
            }

            if (_invertYAxisToggle != null)
            {
                _invertYAxisToggle.RegisterValueChangedCallback(OnInvertYAxisChanged);
            }
        }

        /// <summary>
        /// イベントハンドラを解除します
        /// </summary>
        private void UnregisterEventHandlers()
        {
            // Settings/Pause Popup Buttons
            if (_resumeButton != null)
            {
                _resumeButton.clicked -= OnResumeClicked;
            }

            if (_backToMenuButton != null)
            {
                _backToMenuButton.clicked -= OnBackToMenuClicked;
            }

            // Settings Buttons
            if (_keyBindingsButton != null)
            {
                _keyBindingsButton.clicked -= OnKeyBindingsClicked;
            }

            if (_applySettingsButton != null)
            {
                _applySettingsButton.clicked -= OnApplySettingsClicked;
            }

            if (_resetSettingsButton != null)
            {
                _resetSettingsButton.clicked -= OnResetSettingsClicked;
            }

            // Audio Settings
            if (_masterVolumeSlider != null)
            {
                _masterVolumeSlider.UnregisterValueChangedCallback(OnMasterVolumeChanged);
            }

            if (_bgmVolumeSlider != null)
            {
                _bgmVolumeSlider.UnregisterValueChangedCallback(OnBgmVolumeChanged);
            }

            if (_sfxVolumeSlider != null)
            {
                _sfxVolumeSlider.UnregisterValueChangedCallback(OnSfxVolumeChanged);
            }

            // Video Settings
            if (_displayModeDropdown != null)
            {
                _displayModeDropdown.UnregisterValueChangedCallback(OnDisplayModeChanged);
            }

            if (_resolutionDropdown != null)
            {
                _resolutionDropdown.UnregisterValueChangedCallback(OnResolutionChanged);
            }

            if (_qualityDropdown != null)
            {
                _qualityDropdown.UnregisterValueChangedCallback(OnQualityChanged);
            }

            if (_vSyncToggle != null)
            {
                _vSyncToggle.UnregisterValueChangedCallback(OnVSyncChanged);
            }

            if (_targetFpsDropdown != null)
            {
                _targetFpsDropdown.UnregisterValueChangedCallback(OnTargetFpsChanged);
            }

            if (_antiAliasingDropdown != null)
            {
                _antiAliasingDropdown.UnregisterValueChangedCallback(OnAntiAliasingChanged);
            }

            // Gameplay Settings
            if (_cameraSensitivitySlider != null)
            {
                _cameraSensitivitySlider.UnregisterValueChangedCallback(OnCameraSensitivityChanged);
            }

            if (_invertYAxisToggle != null)
            {
                _invertYAxisToggle.UnregisterValueChangedCallback(OnInvertYAxisChanged);
            }
        }

        /// <summary>
        /// ViewModelの値でUIを初期化します
        /// </summary>
        private void UpdateUIFromViewModel()
        {
            if (ViewModel == null)
            {
                return;
            }

            UpdateStatistics();
        }

        #endregion

        #region Event Handlers

        /// <summary>
        /// ViewModelのプロパティ変更時の処理
        /// </summary>
        private void OnViewModelPropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            if (ViewModel == null)
            {
                return;
            }

            switch (e.PropertyName)
            {
                case nameof(TrainingViewModel.Score):
                case nameof(TrainingViewModel.ArrowsFired):
                case nameof(TrainingViewModel.Hits):
                case nameof(TrainingViewModel.Accuracy):
                    UpdateStatistics();
                    break;
            }
        }

        /// <summary>
        /// ポーズ状態変更時の処理
        /// </summary>
        private void OnPauseStateChanged(object? sender, System.EventArgs e)
        {
            if (ViewModel == null)
            {
                return;
            }

            if (ViewModel.IsPaused)
            {
                ShowPauseSettingsPopup();
            }
            else
            {
                HidePauseSettingsPopup();
            }
        }

        /// <summary>
        /// Resume ボタンクリック時の処理
        /// </summary>
        private void OnResumeClicked()
        {
            Debug.Log("[TrainingView] Resume button clicked!");
            PlayButtonClickSfx();
            ViewModel?.Resume();
        }

        /// <summary>
        /// Back to Menu ボタンクリック時の処理
        /// </summary>
        private void OnBackToMenuClicked()
        {
            Debug.Log("[TrainingView] Back to Menu button clicked!");
            PlayButtonClickSfx();

            // シーン遷移前にTime.timeScaleをリセット
            Time.timeScale = 1f;

            // 入力を有効化
            if (_inputService != null)
            {
                _inputService.InputEnabled = true;
            }

            ViewModel?.BackToMainMenu();
        }

        #endregion

        #region UI Updates

        /// <summary>
        /// 統計表示を更新します
        /// </summary>
        private void UpdateStatistics()
        {
            if (ViewModel == null)
            {
                return;
            }

            if (_scoreLabel != null)
            {
                _scoreLabel.text = $"Score: {ViewModel.Score}";
            }

            if (_arrowsFiredLabel != null)
            {
                _arrowsFiredLabel.text = $"Arrows: {ViewModel.ArrowsFired}";
            }

            if (_hitsLabel != null)
            {
                _hitsLabel.text = $"Hits: {ViewModel.Hits}";
            }

            if (_accuracyLabel != null)
            {
                _accuracyLabel.text = $"Accuracy: {ViewModel.Accuracy:F1}%";
            }
        }

        /// <summary>
        /// 設定ポップアップを表示します
        /// </summary>
        private void ShowPauseSettingsPopup()
        {
            // ドロップダウンを設定
            SetupSettingsDropdowns();

            // 現在の設定を読み込んでUIに反映
            LoadCurrentSettings();

            // 設定ポップアップを表示
            if (_pauseSettingsPopup != null)
            {
                _pauseSettingsPopup.style.display = DisplayStyle.Flex;

                // ポップアップがフォーカスを取得できるようにする
                _pauseSettingsPopup.focusable = true;
                _pauseSettingsPopup.pickingMode = PickingMode.Position;
            }

            // ポーズ中はクロスヘアを非表示
            HideCrosshair();

            // ゲーム時間を停止
            Time.timeScale = 0f;

            // 入力を無効化（ゲーム入力のみ、UIは操作可能）
            if (_inputService != null)
            {
                _inputService.InputEnabled = false;
            }

            // カーソルを表示してロック解除
            UnityEngine.Cursor.lockState = CursorLockMode.None;
            UnityEngine.Cursor.visible = true;

            Debug.Log("[TrainingView] Settings popup shown.");
        }

        /// <summary>
        /// 設定ポップアップを非表示にします
        /// </summary>
        private void HidePauseSettingsPopup()
        {
            if (_pauseSettingsPopup != null)
            {
                _pauseSettingsPopup.style.display = DisplayStyle.None;
            }

            // ゲーム時間を再開
            Time.timeScale = 1f;

            // 入力を有効化
            if (_inputService != null)
            {
                _inputService.InputEnabled = true;
            }

            // カーソルをロックして非表示に戻す
            UnityEngine.Cursor.lockState = CursorLockMode.Locked;
            UnityEngine.Cursor.visible = false;

            Debug.Log("[TrainingView] Settings popup hidden.");
        }

        #endregion

        #region Audio

        /// <summary>
        /// ボタンクリック音を再生します
        /// </summary>
        private void PlayButtonClickSfx()
        {
            if (_buttonClickSfx != null && _audioService != null)
            {
                _audioService.PlaySfx(_buttonClickSfx);
            }
        }

        #endregion

        #region Settings Popup

        /// <summary>
        /// ドロップダウンの選択肢を設定します
        /// </summary>
        private void SetupSettingsDropdowns()
        {
            if (_settingsViewModel == null)
            {
                return;
            }

            // Display Mode
            if (_displayModeDropdown != null)
            {
                _displayModeDropdown.choices = new List<string> { "Fullscreen", "Windowed", "Borderless Window" };
            }

            // Resolution
            if (_resolutionDropdown != null)
            {
                _resolutionDropdown.choices = _settingsViewModel.AvailableResolutions
                    .Select(r => $"{r.width} x {r.height}")
                    .ToList();
            }

            // Quality
            if (_qualityDropdown != null)
            {
                _qualityDropdown.choices = _settingsViewModel.QualityLevelNames;
            }

            // Target FPS
            if (_targetFpsDropdown != null)
            {
                _targetFpsDropdown.choices = new List<string> { "30 FPS", "60 FPS", "120 FPS", "144 FPS", "Unlimited" };
            }

            // Anti-Aliasing
            if (_antiAliasingDropdown != null)
            {
                _antiAliasingDropdown.choices = new List<string> { "Off", "2x MSAA", "4x MSAA", "8x MSAA" };
            }
        }

        /// <summary>
        /// 現在の設定を読み込みます
        /// </summary>
        private void LoadCurrentSettings()
        {
            if (_settingsViewModel == null)
            {
                return;
            }

            // Audio
            if (_masterVolumeSlider != null)
            {
                _masterVolumeSlider.SetValueWithoutNotify(_settingsViewModel.MasterVolume);
            }

            if (_bgmVolumeSlider != null)
            {
                _bgmVolumeSlider.SetValueWithoutNotify(_settingsViewModel.BgmVolume);
            }

            if (_sfxVolumeSlider != null)
            {
                _sfxVolumeSlider.SetValueWithoutNotify(_settingsViewModel.SfxVolume);
            }

            // Video
            if (_displayModeDropdown != null)
            {
                _displayModeDropdown.index = _settingsViewModel.DisplayModeIndex;
            }

            if (_resolutionDropdown != null)
            {
                _resolutionDropdown.index = _settingsViewModel.ResolutionIndex;
            }

            if (_qualityDropdown != null)
            {
                _qualityDropdown.index = _settingsViewModel.QualityLevelIndex;
            }

            if (_vSyncToggle != null)
            {
                _vSyncToggle.SetValueWithoutNotify(_settingsViewModel.VSync);
            }

            if (_targetFpsDropdown != null)
            {
                _targetFpsDropdown.index = _settingsViewModel.TargetFrameRateIndex;
            }

            if (_antiAliasingDropdown != null)
            {
                _antiAliasingDropdown.index = _settingsViewModel.AntiAliasingIndex;
            }

            // Gameplay
            if (_cameraSensitivitySlider != null)
            {
                _cameraSensitivitySlider.SetValueWithoutNotify(_settingsViewModel.CameraSensitivity);
            }

            if (_invertYAxisToggle != null)
            {
                _invertYAxisToggle.SetValueWithoutNotify(_settingsViewModel.InvertYAxis);
            }
        }

        /// <summary>
        /// Key Bindings ボタンクリック時の処理
        /// </summary>
        private void OnKeyBindingsClicked()
        {
            PlayButtonClickSfx();

            if (_keyBindingView != null)
            {
                _keyBindingView.Show();
                Debug.Log("[TrainingView] Key Bindings popup shown.");
            }
            else
            {
                Debug.LogWarning("[TrainingView] KeyBindingView is not assigned!");
            }
        }

        /// <summary>
        /// Apply ボタンクリック時の処理
        /// </summary>
        private void OnApplySettingsClicked()
        {
            PlayButtonClickSfx();

            if (_settingsViewModel == null)
            {
                Debug.LogWarning("[TrainingView] SettingsViewModel is null!");
                return;
            }

            // デバッグログ
            bool canApply = _settingsViewModel.ApplySettingsCommand.CanExecute(null);
            Debug.Log($"[TrainingView] CanApplySettings: {canApply}, HasPendingChanges: {_settingsViewModel.HasPendingChanges}");

            // CanExecute がfalseでも強制的に適用を試みる
            if (canApply)
            {
                _settingsViewModel.ApplySettingsCommand.Execute(null);
            }
            else
            {
                // CanExecuteがfalseの場合でも、設定を直接適用してみる
                Debug.LogWarning("[TrainingView] CanApplySettings returned false, attempting force apply...");
                var gameSettingsService = ServiceLocator.Instance.Get<IGameSettingsService>();
                if (gameSettingsService != null)
                {
                    gameSettingsService.ApplySettings();
                    gameSettingsService.SaveSettings();
                    Debug.Log("[TrainingView] Force applied settings via GameSettingsService.");
                }
                else
                {
                    Debug.LogError("[TrainingView] GameSettingsService not found!");
                }
            }

            Debug.Log("[TrainingView] Settings apply completed.");
        }

        /// <summary>
        /// Reset ボタンクリック時の処理
        /// </summary>
        private void OnResetSettingsClicked()
        {
            PlayButtonClickSfx();
            _settingsViewModel?.ResetSettingsCommand.Execute(null);

            // UIを再読み込み
            LoadCurrentSettings();
            Debug.Log("[TrainingView] Settings reset.");
        }

        #endregion

        #region Settings Change Handlers

        // Audio Change Handlers
        /// <summary>
        /// マスターボリューム変更時の処理
        /// </summary>
        private void OnMasterVolumeChanged(ChangeEvent<float> evt)
        {
            if (_settingsViewModel != null)
            {
                _settingsViewModel.MasterVolume = evt.newValue;
                Debug.Log($"[TrainingView] Master volume changed to: {evt.newValue}");
            }
        }

        /// <summary>
        /// BGMボリューム変更時の処理
        /// </summary>
        private void OnBgmVolumeChanged(ChangeEvent<float> evt)
        {
            if (_settingsViewModel != null)
            {
                _settingsViewModel.BgmVolume = evt.newValue;
            }
        }

        /// <summary>
        /// SFXボリューム変更時の処理
        /// </summary>
        private void OnSfxVolumeChanged(ChangeEvent<float> evt)
        {
            if (_settingsViewModel != null)
            {
                _settingsViewModel.SfxVolume = evt.newValue;
            }
        }

        // Video Change Handlers
        /// <summary>
        /// 表示モード変更時の処理
        /// </summary>
        private void OnDisplayModeChanged(ChangeEvent<string> evt)
        {
            if (_settingsViewModel != null && _displayModeDropdown != null)
            {
                _settingsViewModel.DisplayModeIndex = _displayModeDropdown.index;
            }
        }

        /// <summary>
        /// 解像度変更時の処理
        /// </summary>
        private void OnResolutionChanged(ChangeEvent<string> evt)
        {
            if (_settingsViewModel != null && _resolutionDropdown != null)
            {
                _settingsViewModel.ResolutionIndex = _resolutionDropdown.index;
            }
        }

        /// <summary>
        /// 品質プリセット変更時の処理
        /// </summary>
        private void OnQualityChanged(ChangeEvent<string> evt)
        {
            if (_settingsViewModel != null && _qualityDropdown != null)
            {
                _settingsViewModel.QualityLevelIndex = _qualityDropdown.index;
            }
        }

        /// <summary>
        /// VSync変更時の処理
        /// </summary>
        private void OnVSyncChanged(ChangeEvent<bool> evt)
        {
            if (_settingsViewModel != null)
            {
                _settingsViewModel.VSync = evt.newValue;
            }
        }

        /// <summary>
        /// ターゲットFPS変更時の処理
        /// </summary>
        private void OnTargetFpsChanged(ChangeEvent<string> evt)
        {
            if (_settingsViewModel != null && _targetFpsDropdown != null)
            {
                _settingsViewModel.TargetFrameRateIndex = _targetFpsDropdown.index;
            }
        }

        /// <summary>
        /// アンチエイリアシング変更時の処理
        /// </summary>
        private void OnAntiAliasingChanged(ChangeEvent<string> evt)
        {
            if (_settingsViewModel != null && _antiAliasingDropdown != null)
            {
                _settingsViewModel.AntiAliasingIndex = _antiAliasingDropdown.index;
            }
        }

        // Gameplay Change Handlers
        /// <summary>
        /// カメラ感度変更時の処理
        /// </summary>
        private void OnCameraSensitivityChanged(ChangeEvent<float> evt)
        {
            if (_settingsViewModel != null)
            {
                _settingsViewModel.CameraSensitivity = evt.newValue;
            }
        }

        /// <summary>
        /// Y軸反転変更時の処理
        /// </summary>
        private void OnInvertYAxisChanged(ChangeEvent<bool> evt)
        {
            if (_settingsViewModel != null)
            {
                _settingsViewModel.InvertYAxis = evt.newValue;
            }
        }

        #endregion

        #region Crosshair

        /// <summary>
        /// チャージ開始時の処理
        /// </summary>
        private void OnChargingStarted(object? sender, System.EventArgs e)
        {
            Debug.Log("[TrainingView] OnChargingStarted - showing crosshair");
            ShowCrosshair();
        }

        /// <summary>
        /// チャージ終了時の処理
        /// </summary>
        private void OnChargingEnded(object? sender, System.EventArgs e)
        {
            Debug.Log("[TrainingView] OnChargingEnded - hiding crosshair");
            HideCrosshair();
        }

        /// <summary>
        /// クロスヘアを表示します
        /// </summary>
        private void ShowCrosshair()
        {
            if (_crosshair != null)
            {
                _crosshair.RemoveFromClassList("hidden");
                Debug.Log("[TrainingView] Crosshair shown");
            }
            else
            {
                Debug.LogWarning("[TrainingView] Crosshair element is null!");
            }
        }

        /// <summary>
        /// クロスヘアを非表示にします
        /// </summary>
        private void HideCrosshair()
        {
            if (_crosshair != null)
            {
                _crosshair.AddToClassList("hidden");
            }
        }

        #endregion

        #region Training Events

        /// <summary>
        /// 矢が発射された時の処理
        /// </summary>
        private void OnArrowFired(object? sender, System.EventArgs e)
        {
            ViewModel?.RecordArrowFired();
        }

        /// <summary>
        /// ターゲットに命中した時の処理
        /// </summary>
        private void OnTargetHit(object? sender, System.EventArgs e)
        {
            ViewModel?.RecordHit();
        }

        /// <summary>
        /// スコアを獲得した時の処理
        /// </summary>
        private void OnScoreEarned(object? sender, ScoreEarnedEventArgs e)
        {
            ViewModel?.RecordScore(e.Score);

            // スコアポップアップ表示
            ShowScorePopup(e.Score, e.HitPosition);

            Debug.Log($"[TrainingView] Score earned: {e.Score} at {e.HitPosition}");
        }

        /// <summary>
        /// スコアポップアップを表示します
        /// </summary>
        /// <param name="score">スコア</param>
        /// <param name="position">表示位置</param>
        private void ShowScorePopup(int score, Vector3 position)
        {
            if (_scorePopupPrefab == null)
            {
                return;
            }

            // 少し上にオフセット
            Vector3 popupPosition = position + Vector3.up * 0.5f;

            // スコアポップアップをインスタンス化
            GameObject popupObj = Instantiate(_scorePopupPrefab, popupPosition, Quaternion.identity);

            // スコアを設定
            ScorePopup? popup = popupObj.GetComponent<ScorePopup>();
            if (popup != null)
            {
                // スコアに応じて色を変える
                Color popupColor = GetScoreColor(score);
                popup.SetScore(score, popupColor);
            }
        }

        /// <summary>
        /// スコアに応じた色を取得します
        /// </summary>
        /// <param name="score">スコア</param>
        /// <returns>色</returns>
        private Color GetScoreColor(int score)
        {
            // スコアに応じてグラデーション
            if (score >= 20) // フルチャージ
            {
                return new Color(1f, 0.84f, 0f); // ゴールド
            }
            else if (score >= 15)
            {
                return new Color(1f, 0.5f, 0f); // オレンジ
            }
            else if (score >= 10)
            {
                return Color.yellow;
            }
            else
            {
                return Color.white;
            }
        }

        #endregion
    }
}
