#nullable enable

using System;
using System.Collections.Generic;
using System.Linq;
using CavalryFight.Core.Services;
using CavalryFight.Services.Audio;
using CavalryFight.Services.Input;
using CavalryFight.ViewModels;
using UnityEngine;
using UnityEngine.UIElements;

namespace CavalryFight.Views.Components
{
    /// <summary>
    /// 設定ポップアップの共通コントローラー
    /// </summary>
    /// <remarks>
    /// TrainingViewとMatchViewで共有される設定ポップアップのUIロジックを管理します。
    /// </remarks>
    public class SettingsPopupController : IDisposable
    {
        #region Fields

        private readonly VisualElement _popupRoot;
        private readonly SettingsViewModel _settingsViewModel;
        private readonly IInputService? _inputService;
        private readonly IAudioService? _audioService;
        private readonly AudioClip? _buttonClickSfx;
        private readonly KeyBindingView? _keyBindingView;

        // Audio Controls
        private Slider? _masterVolumeSlider;
        private Slider? _bgmVolumeSlider;
        private Slider? _sfxVolumeSlider;

        // Video Controls
        private DropdownField? _displayModeDropdown;
        private DropdownField? _resolutionDropdown;
        private DropdownField? _qualityDropdown;
        private Toggle? _vSyncToggle;
        private DropdownField? _targetFpsDropdown;
        private DropdownField? _antiAliasingDropdown;

        // Gameplay Controls
        private Slider? _movementSensitivitySlider;
        private Slider? _cameraSensitivitySlider;
        private Toggle? _invertYAxisToggle;

        // Buttons
        private Button? _resumeButton;
        private Button? _backToMenuButton;
        private Button? _keyBindingsButton;
        private Button? _applySettingsButton;
        private Button? _resetSettingsButton;

        private bool _isDisposed;

        #endregion

        #region Events

        /// <summary>
        /// 再開ボタンがクリックされた時に発生します
        /// </summary>
        public event EventHandler? ResumeRequested;

        /// <summary>
        /// メニューに戻るボタンがクリックされた時に発生します
        /// </summary>
        public event EventHandler? BackToMenuRequested;

        #endregion

        #region Constructor

        /// <summary>
        /// コンストラクタ
        /// </summary>
        /// <param name="popupRoot">設定ポップアップのルートVisualElement</param>
        /// <param name="keyBindingView">キーバインディングView（オプション）</param>
        /// <param name="buttonClickSfx">ボタンクリック音（オプション）</param>
        public SettingsPopupController(
            VisualElement popupRoot,
            KeyBindingView? keyBindingView = null,
            AudioClip? buttonClickSfx = null)
        {
            _popupRoot = popupRoot ?? throw new ArgumentNullException(nameof(popupRoot));
            _settingsViewModel = new SettingsViewModel();
            _inputService = ServiceLocator.Instance.Get<IInputService>();
            _audioService = ServiceLocator.Instance.Get<IAudioService>();
            _keyBindingView = keyBindingView;
            _buttonClickSfx = buttonClickSfx;

            GetUIElements();
            SetupDropdowns();
            RegisterEventHandlers();
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// ポップアップを表示します
        /// </summary>
        public void Show()
        {
            LoadCurrentSettings();
            _popupRoot.RemoveFromClassList("hidden");
            _popupRoot.style.display = DisplayStyle.Flex;

            // ゲーム時間を停止
            Time.timeScale = 0f;

            // 入力を無効化
            if (_inputService != null)
            {
                _inputService.InputEnabled = false;
            }

            Debug.Log("[SettingsPopupController] Settings popup shown.");
        }

        /// <summary>
        /// ポップアップを非表示にします
        /// </summary>
        public void Hide()
        {
            _popupRoot.AddToClassList("hidden");
            _popupRoot.style.display = DisplayStyle.None;

            // ゲーム時間を再開
            Time.timeScale = 1f;

            // 入力を有効化
            if (_inputService != null)
            {
                _inputService.InputEnabled = true;
            }

            Debug.Log("[SettingsPopupController] Settings popup hidden.");
        }

        /// <summary>
        /// ポップアップが表示されているかどうか
        /// </summary>
        public bool IsVisible => _popupRoot.style.display == DisplayStyle.Flex;

        #endregion

        #region Private Methods - Setup

        private void GetUIElements()
        {
            // Audio
            _masterVolumeSlider = _popupRoot.Q<Slider>("MasterVolumeSlider");
            _bgmVolumeSlider = _popupRoot.Q<Slider>("BgmVolumeSlider");
            _sfxVolumeSlider = _popupRoot.Q<Slider>("SfxVolumeSlider");

            // Video
            _displayModeDropdown = _popupRoot.Q<DropdownField>("DisplayModeDropdown");
            _resolutionDropdown = _popupRoot.Q<DropdownField>("ResolutionDropdown");
            _qualityDropdown = _popupRoot.Q<DropdownField>("QualityDropdown");
            _vSyncToggle = _popupRoot.Q<Toggle>("VSyncToggle");
            _targetFpsDropdown = _popupRoot.Q<DropdownField>("TargetFpsDropdown");
            _antiAliasingDropdown = _popupRoot.Q<DropdownField>("AntiAliasingDropdown");

            // Gameplay
            _movementSensitivitySlider = _popupRoot.Q<Slider>("MovementSensitivitySlider");
            _cameraSensitivitySlider = _popupRoot.Q<Slider>("CameraSensitivitySlider");
            _invertYAxisToggle = _popupRoot.Q<Toggle>("InvertYAxisToggle");

            // Buttons（SettingsResumeButtonとResumeButtonの両方をサポート）
            _resumeButton = _popupRoot.Q<Button>("SettingsResumeButton") ?? _popupRoot.Q<Button>("ResumeButton");
            _backToMenuButton = _popupRoot.Q<Button>("BackToMenuButton");
            _keyBindingsButton = _popupRoot.Q<Button>("KeyBindingsButton");
            _applySettingsButton = _popupRoot.Q<Button>("ApplySettingsButton");
            _resetSettingsButton = _popupRoot.Q<Button>("ResetSettingsButton");

            Debug.Log($"[SettingsPopupController] ResumeButton found: {_resumeButton != null}");
            Debug.Log($"[SettingsPopupController] ApplySettingsButton found: {_applySettingsButton != null}");
        }

        private void SetupDropdowns()
        {
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

        private void RegisterEventHandlers()
        {
            // Buttons
            if (_resumeButton != null)
            {
                _resumeButton.clicked += OnResumeClicked;
            }

            if (_backToMenuButton != null)
            {
                _backToMenuButton.clicked += OnBackToMenuClicked;
            }

            if (_keyBindingsButton != null)
            {
                _keyBindingsButton.clicked += OnKeyBindingsClicked;
            }

            if (_applySettingsButton != null)
            {
                _applySettingsButton.clicked += OnApplySettingsClicked;
            }

            if (_resetSettingsButton != null)
            {
                _resetSettingsButton.clicked += OnResetSettingsClicked;
            }

            // Audio Sliders
            _masterVolumeSlider?.RegisterValueChangedCallback(OnMasterVolumeChanged);
            _bgmVolumeSlider?.RegisterValueChangedCallback(OnBgmVolumeChanged);
            _sfxVolumeSlider?.RegisterValueChangedCallback(OnSfxVolumeChanged);

            // Video Dropdowns
            _displayModeDropdown?.RegisterValueChangedCallback(OnDisplayModeChanged);
            _resolutionDropdown?.RegisterValueChangedCallback(OnResolutionChanged);
            _qualityDropdown?.RegisterValueChangedCallback(OnQualityChanged);
            _vSyncToggle?.RegisterValueChangedCallback(OnVSyncChanged);
            _targetFpsDropdown?.RegisterValueChangedCallback(OnTargetFpsChanged);
            _antiAliasingDropdown?.RegisterValueChangedCallback(OnAntiAliasingChanged);

            // Gameplay
            _movementSensitivitySlider?.RegisterValueChangedCallback(OnMovementSensitivityChanged);
            _cameraSensitivitySlider?.RegisterValueChangedCallback(OnCameraSensitivityChanged);
            _invertYAxisToggle?.RegisterValueChangedCallback(OnInvertYAxisChanged);
        }

        private void UnregisterEventHandlers()
        {
            // Buttons
            if (_resumeButton != null)
            {
                _resumeButton.clicked -= OnResumeClicked;
            }

            if (_backToMenuButton != null)
            {
                _backToMenuButton.clicked -= OnBackToMenuClicked;
            }

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

            // Audio Sliders
            _masterVolumeSlider?.UnregisterValueChangedCallback(OnMasterVolumeChanged);
            _bgmVolumeSlider?.UnregisterValueChangedCallback(OnBgmVolumeChanged);
            _sfxVolumeSlider?.UnregisterValueChangedCallback(OnSfxVolumeChanged);

            // Video Dropdowns
            _displayModeDropdown?.UnregisterValueChangedCallback(OnDisplayModeChanged);
            _resolutionDropdown?.UnregisterValueChangedCallback(OnResolutionChanged);
            _qualityDropdown?.UnregisterValueChangedCallback(OnQualityChanged);
            _vSyncToggle?.UnregisterValueChangedCallback(OnVSyncChanged);
            _targetFpsDropdown?.UnregisterValueChangedCallback(OnTargetFpsChanged);
            _antiAliasingDropdown?.UnregisterValueChangedCallback(OnAntiAliasingChanged);

            // Gameplay
            _movementSensitivitySlider?.UnregisterValueChangedCallback(OnMovementSensitivityChanged);
            _cameraSensitivitySlider?.UnregisterValueChangedCallback(OnCameraSensitivityChanged);
            _invertYAxisToggle?.UnregisterValueChangedCallback(OnInvertYAxisChanged);
        }

        private void LoadCurrentSettings()
        {
            // Audio
            _masterVolumeSlider?.SetValueWithoutNotify(_settingsViewModel.MasterVolume);
            _bgmVolumeSlider?.SetValueWithoutNotify(_settingsViewModel.BgmVolume);
            _sfxVolumeSlider?.SetValueWithoutNotify(_settingsViewModel.SfxVolume);

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

            _vSyncToggle?.SetValueWithoutNotify(_settingsViewModel.VSync);

            if (_targetFpsDropdown != null)
            {
                _targetFpsDropdown.index = _settingsViewModel.TargetFrameRateIndex;
            }

            if (_antiAliasingDropdown != null)
            {
                _antiAliasingDropdown.index = _settingsViewModel.AntiAliasingIndex;
            }

            // Gameplay
            _movementSensitivitySlider?.SetValueWithoutNotify(_settingsViewModel.MovementSensitivity);
            _cameraSensitivitySlider?.SetValueWithoutNotify(_settingsViewModel.CameraSensitivity);
            _invertYAxisToggle?.SetValueWithoutNotify(_settingsViewModel.InvertYAxis);
        }

        #endregion

        #region Event Handlers

        private void PlayButtonClickSfx()
        {
            if (_buttonClickSfx != null && _audioService != null)
            {
                _audioService.PlaySfx(_buttonClickSfx);
            }
        }

        private void OnResumeClicked()
        {
            PlayButtonClickSfx();
            ResumeRequested?.Invoke(this, EventArgs.Empty);
        }

        private void OnBackToMenuClicked()
        {
            PlayButtonClickSfx();

            // シーン遷移前にTime.timeScaleをリセット
            Time.timeScale = 1f;

            // 入力を有効化
            if (_inputService != null)
            {
                _inputService.InputEnabled = true;
            }

            BackToMenuRequested?.Invoke(this, EventArgs.Empty);
        }

        private void OnKeyBindingsClicked()
        {
            PlayButtonClickSfx();

            if (_keyBindingView != null)
            {
                _keyBindingView.Show();
                Debug.Log("[SettingsPopupController] Key Bindings popup shown.");
            }
            else
            {
                Debug.LogWarning("[SettingsPopupController] KeyBindingView is not assigned!");
            }
        }

        private void OnApplySettingsClicked()
        {
            PlayButtonClickSfx();

            // デバッグログ
            bool canApply = _settingsViewModel.ApplySettingsCommand.CanExecute(null);
            Debug.Log($"[SettingsPopupController] CanApplySettings: {canApply}, HasPendingChanges: {_settingsViewModel.HasPendingChanges}");

            if (canApply)
            {
                _settingsViewModel.ApplySettingsCommand.Execute(null);
            }
            else
            {
                // CanExecuteがfalseの場合でも、設定を直接適用してみる
                Debug.LogWarning("[SettingsPopupController] CanApplySettings returned false, attempting force apply...");
                var gameSettingsService = ServiceLocator.Instance.Get<CavalryFight.Services.GameSettings.IGameSettingsService>();
                if (gameSettingsService != null)
                {
                    gameSettingsService.ApplySettings();
                    gameSettingsService.SaveSettings();
                    Debug.Log("[SettingsPopupController] Force applied settings via GameSettingsService.");
                }
            }

            Debug.Log("[SettingsPopupController] Settings apply completed.");
        }

        private void OnResetSettingsClicked()
        {
            PlayButtonClickSfx();
            _settingsViewModel.ResetSettingsCommand.Execute(null);
            LoadCurrentSettings();
            Debug.Log("[SettingsPopupController] Settings reset.");
        }

        // Audio Change Handlers
        private void OnMasterVolumeChanged(ChangeEvent<float> evt)
        {
            _settingsViewModel.MasterVolume = evt.newValue;
        }

        private void OnBgmVolumeChanged(ChangeEvent<float> evt)
        {
            _settingsViewModel.BgmVolume = evt.newValue;
        }

        private void OnSfxVolumeChanged(ChangeEvent<float> evt)
        {
            _settingsViewModel.SfxVolume = evt.newValue;
        }

        // Video Change Handlers
        private void OnDisplayModeChanged(ChangeEvent<string> evt)
        {
            if (_displayModeDropdown != null)
            {
                _settingsViewModel.DisplayModeIndex = _displayModeDropdown.index;
            }
        }

        private void OnResolutionChanged(ChangeEvent<string> evt)
        {
            if (_resolutionDropdown != null)
            {
                _settingsViewModel.ResolutionIndex = _resolutionDropdown.index;
            }
        }

        private void OnQualityChanged(ChangeEvent<string> evt)
        {
            if (_qualityDropdown != null)
            {
                _settingsViewModel.QualityLevelIndex = _qualityDropdown.index;
            }
        }

        private void OnVSyncChanged(ChangeEvent<bool> evt)
        {
            _settingsViewModel.VSync = evt.newValue;
        }

        private void OnTargetFpsChanged(ChangeEvent<string> evt)
        {
            if (_targetFpsDropdown != null)
            {
                _settingsViewModel.TargetFrameRateIndex = _targetFpsDropdown.index;
            }
        }

        private void OnAntiAliasingChanged(ChangeEvent<string> evt)
        {
            if (_antiAliasingDropdown != null)
            {
                _settingsViewModel.AntiAliasingIndex = _antiAliasingDropdown.index;
            }
        }

        // Gameplay Change Handlers
        private void OnMovementSensitivityChanged(ChangeEvent<float> evt)
        {
            _settingsViewModel.MovementSensitivity = evt.newValue;
        }

        private void OnCameraSensitivityChanged(ChangeEvent<float> evt)
        {
            _settingsViewModel.CameraSensitivity = evt.newValue;
        }

        private void OnInvertYAxisChanged(ChangeEvent<bool> evt)
        {
            _settingsViewModel.InvertYAxis = evt.newValue;
        }

        #endregion

        #region IDisposable

        public void Dispose()
        {
            if (_isDisposed)
            {
                return;
            }

            UnregisterEventHandlers();
            _settingsViewModel.Dispose();
            _isDisposed = true;
        }

        #endregion
    }
}
