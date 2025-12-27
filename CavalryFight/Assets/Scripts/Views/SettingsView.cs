#nullable enable

using CavalryFight.Core.MVVM;
using CavalryFight.ViewModels;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UIElements;

namespace CavalryFight.Views
{
    /// <summary>
    /// 設定画面のView
    /// </summary>
    /// <remarks>
    /// UI Toolkitを使用して設定UIを表示します。
    /// SettingsViewModelとバインドされ、ユーザー操作を処理します。
    /// </remarks>
    [RequireComponent(typeof(UIDocument))]
    public class SettingsView : UIToolkitViewBase<SettingsViewModel>
    {
        #region Fields

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
        private Slider? _movementSensitivitySlider;
        private Slider? _cameraSensitivitySlider;
        private Toggle? _invertYAxisToggle;

        // Buttons
        private Button? _applyButton;
        private Button? _resetButton;
        private Button? _keyBindingButton;
        private Button? _backButton;
        private Label? _titleLabel;

        // Key Binding Popup
        private KeyBindingView? _keyBindingView;

        #endregion

        #region Unity Lifecycle

        /// <summary>
        /// 初期化処理
        /// </summary>
        protected override void Awake()
        {
            base.Awake();

            // ViewModelを作成してバインド
            ViewModel = new SettingsViewModel();

            // KeyBindingViewを取得
            _keyBindingView = FindFirstObjectByType<KeyBindingView>(FindObjectsInactive.Include);
            if (_keyBindingView == null)
            {
                Debug.LogWarning("[SettingsView] KeyBindingView not found in scene.", this);
            }
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
            GetUIElements();

            // UI要素の検証
            ValidateUIElements();

            // Dropdownの選択肢を設定
            SetupDropdowns();

            // ViewModelの値でUIを更新（現在の設定値を表示）
            UpdateUIFromViewModel();

            // イベントハンドラを登録
            RegisterEventHandlers();
        }

        /// <summary>
        /// ViewModelとのバインディングを設定します。
        /// </summary>
        /// <param name="viewModel">バインドするViewModel</param>
        protected override void BindViewModel(SettingsViewModel viewModel)
        {
            base.BindViewModel(viewModel);

            // PropertyChangedイベントを購読
            viewModel.PropertyChanged += OnViewModelPropertyChanged;

            // キーバインディング開く要求イベントを購読
            viewModel.OpenKeyBindingsRequested += OnOpenKeyBindingsRequested;
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
                ViewModel.OpenKeyBindingsRequested -= OnOpenKeyBindingsRequested;
            }

            // イベントハンドラを解除
            UnregisterEventHandlers();

            base.UnbindViewModel();
        }

        #endregion

        #region Private Methods - Setup

        /// <summary>
        /// UI要素を取得します
        /// </summary>
        private void GetUIElements()
        {
            // Title
            _titleLabel = Q<Label>("TitleLabel");

            // Audio
            _masterVolumeSlider = Q<Slider>("MasterVolumeSlider");
            _bgmVolumeSlider = Q<Slider>("BgmVolumeSlider");
            _sfxVolumeSlider = Q<Slider>("SfxVolumeSlider");

            // Video
            _displayModeDropdown = Q<DropdownField>("DisplayModeDropdown");
            _resolutionDropdown = Q<DropdownField>("ResolutionDropdown");
            _qualityDropdown = Q<DropdownField>("QualityDropdown");
            _vSyncToggle = Q<Toggle>("VSyncToggle");
            _targetFpsDropdown = Q<DropdownField>("TargetFpsDropdown");
            _antiAliasingDropdown = Q<DropdownField>("AntiAliasingDropdown");

            // Gameplay
            _movementSensitivitySlider = Q<Slider>("MovementSensitivitySlider");
            _cameraSensitivitySlider = Q<Slider>("CameraSensitivitySlider");
            _invertYAxisToggle = Q<Toggle>("InvertYAxisToggle");

            // Buttons
            _applyButton = Q<Button>("ApplyButton");
            _resetButton = Q<Button>("ResetButton");
            _keyBindingButton = Q<Button>("KeyBindingButton");
            _backButton = Q<Button>("BackButton");
        }

        /// <summary>
        /// UI要素が正しく取得できているか検証します。
        /// </summary>
        private void ValidateUIElements()
        {
            if (_titleLabel == null)
            {
                Debug.LogWarning("[SettingsView] TitleLabel not found in UXML.", this);
            }

            // Audio
            if (_masterVolumeSlider == null)
            {
                Debug.LogWarning("[SettingsView] MasterVolumeSlider not found in UXML.", this);
            }
            if (_bgmVolumeSlider == null)
            {
                Debug.LogWarning("[SettingsView] BgmVolumeSlider not found in UXML.", this);
            }
            if (_sfxVolumeSlider == null)
            {
                Debug.LogWarning("[SettingsView] SfxVolumeSlider not found in UXML.", this);
            }

            // Video
            if (_displayModeDropdown == null)
            {
                Debug.LogWarning("[SettingsView] DisplayModeDropdown not found in UXML.", this);
            }
            if (_resolutionDropdown == null)
            {
                Debug.LogWarning("[SettingsView] ResolutionDropdown not found in UXML.", this);
            }
            if (_qualityDropdown == null)
            {
                Debug.LogWarning("[SettingsView] QualityDropdown not found in UXML.", this);
            }
            if (_vSyncToggle == null)
            {
                Debug.LogWarning("[SettingsView] VSyncToggle not found in UXML.", this);
            }
            if (_targetFpsDropdown == null)
            {
                Debug.LogWarning("[SettingsView] TargetFpsDropdown not found in UXML.", this);
            }
            if (_antiAliasingDropdown == null)
            {
                Debug.LogWarning("[SettingsView] AntiAliasingDropdown not found in UXML.", this);
            }

            // Gameplay
            if (_movementSensitivitySlider == null)
            {
                Debug.LogWarning("[SettingsView] MovementSensitivitySlider not found in UXML.", this);
            }
            if (_cameraSensitivitySlider == null)
            {
                Debug.LogWarning("[SettingsView] CameraSensitivitySlider not found in UXML.", this);
            }
            if (_invertYAxisToggle == null)
            {
                Debug.LogWarning("[SettingsView] InvertYAxisToggle not found in UXML.", this);
            }

            // Buttons
            if (_applyButton == null)
            {
                Debug.LogWarning("[SettingsView] ApplyButton not found in UXML.", this);
            }
            if (_resetButton == null)
            {
                Debug.LogWarning("[SettingsView] ResetButton not found in UXML.", this);
            }
            if (_keyBindingButton == null)
            {
                Debug.LogWarning("[SettingsView] KeyBindingButton not found in UXML.", this);
            }
            if (_backButton == null)
            {
                Debug.LogWarning("[SettingsView] BackButton not found in UXML.", this);
            }
        }

        /// <summary>
        /// Dropdownの選択肢を設定します
        /// </summary>
        private void SetupDropdowns()
        {
            if (ViewModel == null)
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
                _resolutionDropdown.choices = ViewModel.AvailableResolutions
                    .Select(r => $"{r.width} x {r.height}")
                    .ToList();
            }

            // Quality
            if (_qualityDropdown != null)
            {
                _qualityDropdown.choices = ViewModel.QualityLevelNames;
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
        /// ViewModelの値でUIを更新します
        /// </summary>
        private void UpdateUIFromViewModel()
        {
            if (ViewModel == null)
            {
                return;
            }

            // Audio
            if (_masterVolumeSlider != null)
            {
                _masterVolumeSlider.value = ViewModel.MasterVolume;
            }
            if (_bgmVolumeSlider != null)
            {
                _bgmVolumeSlider.value = ViewModel.BgmVolume;
            }
            if (_sfxVolumeSlider != null)
            {
                _sfxVolumeSlider.value = ViewModel.SfxVolume;
            }

            // Video
            if (_displayModeDropdown != null)
            {
                _displayModeDropdown.index = ViewModel.DisplayModeIndex;
            }
            if (_resolutionDropdown != null)
            {
                _resolutionDropdown.index = ViewModel.ResolutionIndex;
            }
            if (_qualityDropdown != null)
            {
                _qualityDropdown.index = ViewModel.QualityLevelIndex;
            }
            if (_vSyncToggle != null)
            {
                _vSyncToggle.value = ViewModel.VSync;
            }
            if (_targetFpsDropdown != null)
            {
                _targetFpsDropdown.index = ViewModel.TargetFrameRateIndex;
            }
            if (_antiAliasingDropdown != null)
            {
                _antiAliasingDropdown.index = ViewModel.AntiAliasingIndex;
            }

            // Gameplay
            if (_movementSensitivitySlider != null)
            {
                _movementSensitivitySlider.value = ViewModel.MovementSensitivity;
            }
            if (_cameraSensitivitySlider != null)
            {
                _cameraSensitivitySlider.value = ViewModel.CameraSensitivity;
            }
            if (_invertYAxisToggle != null)
            {
                _invertYAxisToggle.value = ViewModel.InvertYAxis;
            }
        }

        #endregion

        #region Private Methods - Event Registration

        /// <summary>
        /// イベントハンドラを登録します。
        /// </summary>
        private void RegisterEventHandlers()
        {
            // Audio
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

            // Video
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

            // Gameplay
            if (_movementSensitivitySlider != null)
            {
                _movementSensitivitySlider.RegisterValueChangedCallback(OnMovementSensitivityChanged);
            }
            if (_cameraSensitivitySlider != null)
            {
                _cameraSensitivitySlider.RegisterValueChangedCallback(OnCameraSensitivityChanged);
            }
            if (_invertYAxisToggle != null)
            {
                _invertYAxisToggle.RegisterValueChangedCallback(OnInvertYAxisChanged);
            }

            // Buttons
            if (_applyButton != null)
            {
                _applyButton.clicked += OnApplyButtonClicked;
            }
            if (_resetButton != null)
            {
                _resetButton.clicked += OnResetButtonClicked;
            }
            if (_keyBindingButton != null)
            {
                _keyBindingButton.clicked += OnKeyBindingButtonClicked;
            }
            if (_backButton != null)
            {
                _backButton.clicked += OnBackButtonClicked;
            }
        }

        /// <summary>
        /// イベントハンドラを解除します。
        /// </summary>
        private void UnregisterEventHandlers()
        {
            // Audio
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

            // Video
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

            // Gameplay
            if (_movementSensitivitySlider != null)
            {
                _movementSensitivitySlider.UnregisterValueChangedCallback(OnMovementSensitivityChanged);
            }
            if (_cameraSensitivitySlider != null)
            {
                _cameraSensitivitySlider.UnregisterValueChangedCallback(OnCameraSensitivityChanged);
            }
            if (_invertYAxisToggle != null)
            {
                _invertYAxisToggle.UnregisterValueChangedCallback(OnInvertYAxisChanged);
            }

            // Buttons
            if (_applyButton != null)
            {
                _applyButton.clicked -= OnApplyButtonClicked;
            }
            if (_resetButton != null)
            {
                _resetButton.clicked -= OnResetButtonClicked;
            }
            if (_keyBindingButton != null)
            {
                _keyBindingButton.clicked -= OnKeyBindingButtonClicked;
            }
            if (_backButton != null)
            {
                _backButton.clicked -= OnBackButtonClicked;
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
                // Audio
                case nameof(SettingsViewModel.MasterVolume):
                    if (_masterVolumeSlider != null)
                    {
                        _masterVolumeSlider.value = ViewModel.MasterVolume;
                    }
                    break;
                case nameof(SettingsViewModel.BgmVolume):
                    if (_bgmVolumeSlider != null)
                    {
                        _bgmVolumeSlider.value = ViewModel.BgmVolume;
                    }
                    break;
                case nameof(SettingsViewModel.SfxVolume):
                    if (_sfxVolumeSlider != null)
                    {
                        _sfxVolumeSlider.value = ViewModel.SfxVolume;
                    }
                    break;

                // Video
                case nameof(SettingsViewModel.DisplayModeIndex):
                    if (_displayModeDropdown != null)
                    {
                        _displayModeDropdown.index = ViewModel.DisplayModeIndex;
                    }
                    break;
                case nameof(SettingsViewModel.ResolutionIndex):
                    if (_resolutionDropdown != null)
                    {
                        _resolutionDropdown.index = ViewModel.ResolutionIndex;
                    }
                    break;
                case nameof(SettingsViewModel.QualityLevelIndex):
                    if (_qualityDropdown != null)
                    {
                        _qualityDropdown.index = ViewModel.QualityLevelIndex;
                    }
                    break;
                case nameof(SettingsViewModel.VSync):
                    if (_vSyncToggle != null)
                    {
                        _vSyncToggle.value = ViewModel.VSync;
                    }
                    break;
                case nameof(SettingsViewModel.TargetFrameRateIndex):
                    if (_targetFpsDropdown != null)
                    {
                        _targetFpsDropdown.index = ViewModel.TargetFrameRateIndex;
                    }
                    break;
                case nameof(SettingsViewModel.AntiAliasingIndex):
                    if (_antiAliasingDropdown != null)
                    {
                        _antiAliasingDropdown.index = ViewModel.AntiAliasingIndex;
                    }
                    break;

                // Gameplay
                case nameof(SettingsViewModel.MovementSensitivity):
                    if (_movementSensitivitySlider != null)
                    {
                        _movementSensitivitySlider.value = ViewModel.MovementSensitivity;
                    }
                    break;
                case nameof(SettingsViewModel.CameraSensitivity):
                    if (_cameraSensitivitySlider != null)
                    {
                        _cameraSensitivitySlider.value = ViewModel.CameraSensitivity;
                    }
                    break;
                case nameof(SettingsViewModel.InvertYAxis):
                    if (_invertYAxisToggle != null)
                    {
                        _invertYAxisToggle.value = ViewModel.InvertYAxis;
                    }
                    break;
            }
        }

        // Audio Change Handlers
        private void OnMasterVolumeChanged(ChangeEvent<float> evt)
        {
            if (ViewModel != null)
            {
                ViewModel.MasterVolume = evt.newValue;
            }
        }

        private void OnBgmVolumeChanged(ChangeEvent<float> evt)
        {
            if (ViewModel != null)
            {
                ViewModel.BgmVolume = evt.newValue;
            }
        }

        private void OnSfxVolumeChanged(ChangeEvent<float> evt)
        {
            if (ViewModel != null)
            {
                ViewModel.SfxVolume = evt.newValue;
            }
        }

        // Video Change Handlers
        private void OnDisplayModeChanged(ChangeEvent<string> evt)
        {
            if (ViewModel != null && _displayModeDropdown != null)
            {
                ViewModel.DisplayModeIndex = _displayModeDropdown.index;
            }
        }

        private void OnResolutionChanged(ChangeEvent<string> evt)
        {
            if (ViewModel != null && _resolutionDropdown != null)
            {
                ViewModel.ResolutionIndex = _resolutionDropdown.index;
            }
        }

        private void OnQualityChanged(ChangeEvent<string> evt)
        {
            if (ViewModel != null && _qualityDropdown != null)
            {
                ViewModel.QualityLevelIndex = _qualityDropdown.index;
            }
        }

        private void OnVSyncChanged(ChangeEvent<bool> evt)
        {
            if (ViewModel != null)
            {
                ViewModel.VSync = evt.newValue;
            }
        }

        private void OnTargetFpsChanged(ChangeEvent<string> evt)
        {
            if (ViewModel != null && _targetFpsDropdown != null)
            {
                ViewModel.TargetFrameRateIndex = _targetFpsDropdown.index;
            }
        }

        private void OnAntiAliasingChanged(ChangeEvent<string> evt)
        {
            if (ViewModel != null && _antiAliasingDropdown != null)
            {
                ViewModel.AntiAliasingIndex = _antiAliasingDropdown.index;
            }
        }

        // Gameplay Change Handlers
        private void OnMovementSensitivityChanged(ChangeEvent<float> evt)
        {
            if (ViewModel != null)
            {
                ViewModel.MovementSensitivity = evt.newValue;
            }
        }

        private void OnCameraSensitivityChanged(ChangeEvent<float> evt)
        {
            if (ViewModel != null)
            {
                ViewModel.CameraSensitivity = evt.newValue;
            }
        }

        private void OnInvertYAxisChanged(ChangeEvent<bool> evt)
        {
            if (ViewModel != null)
            {
                ViewModel.InvertYAxis = evt.newValue;
            }
        }

        // Button Handlers
        private void OnApplyButtonClicked()
        {
            ViewModel?.ApplySettingsCommand.Execute(null);
        }

        private void OnResetButtonClicked()
        {
            ViewModel?.ResetSettingsCommand.Execute(null);
        }

        private void OnKeyBindingButtonClicked()
        {
            ViewModel?.OpenKeyBindingsCommand.Execute(null);
        }

        /// <summary>
        /// キーバインディング開く要求イベントを処理します
        /// </summary>
        private void OnOpenKeyBindingsRequested(object? sender, System.EventArgs e)
        {
            if (_keyBindingView != null)
            {
                _keyBindingView.Show();
            }
            else
            {
                Debug.LogWarning("[SettingsView] Cannot open key bindings: KeyBindingView is not available.", this);
            }
        }

        private void OnBackButtonClicked()
        {
            ViewModel?.BackToMenuCommand.Execute(null);
        }

        #endregion
    }
}
