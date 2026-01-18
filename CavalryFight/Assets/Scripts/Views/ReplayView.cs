#nullable enable

using System;
using System.Collections.Generic;
using System.ComponentModel;
using CavalryFight.Core.MVVM;
using CavalryFight.Core.Services;
using CavalryFight.Services.Audio;
using CavalryFight.Services.Replay;
using CavalryFight.ViewModels;
using UnityEngine;
using UnityEngine.UIElements;

namespace CavalryFight.Views
{
    /// <summary>
    /// リプレイ視聴画面のView
    /// </summary>
    /// <remarks>
    /// リプレイの再生制御UIを管理します。
    /// タイムライン、再生コントロール、カメラ切り替えなどを提供します。
    /// </remarks>
    [RequireComponent(typeof(UIDocument))]
    public class ReplayView : UIToolkitViewBase<ReplayViewModel>
    {
        #region Serialized Fields

        [Header("Audio")]
        [SerializeField] private AudioClip? _buttonClickSFX;

        [Header("Keyboard Shortcuts")]
        [SerializeField] private KeyCode _toggleUIKey = KeyCode.H;
        [SerializeField] private KeyCode _playPauseKey = KeyCode.Space;
        [SerializeField] private KeyCode _skipBackKey = KeyCode.LeftArrow;
        [SerializeField] private KeyCode _skipForwardKey = KeyCode.RightArrow;
        [SerializeField] private KeyCode _cameraModeKey = KeyCode.C;

        #endregion

        #region UI Elements

        // Container
        private VisualElement? _uiContainer;

        // Header
        private VisualElement? _headerPanel;
        private Label? _mapLabel;
        private Label? _gameModeLabel;
        private Label? _scoreLabel;

        // Timeline
        private VisualElement? _timelinePanel;
        private Slider? _timelineSlider;
        private Label? _currentTimeLabel;
        private Label? _durationLabel;

        // Controls
        private VisualElement? _controlsPanel;
        private Button? _skipBackButton;
        private Button? _playPauseButton;
        private Button? _skipForwardButton;
        private Button? _stopButton;

        // Speed
        private DropdownField? _speedDropdown;

        // Highlights
        private VisualElement? _highlightsPanel;
        private Button? _prevHighlightButton;
        private Button? _nextHighlightButton;
        private Label? _highlightLabel;

        // Camera
        private Button? _cameraModeButton;
        private Label? _cameraModeLabel;

        // Footer
        private Button? _toggleUIButton;
        private Button? _exitButton;

        #endregion

        #region Services

        private IAudioService? _audioService;
        private IReplayService? _replayService;

        #endregion

        #region Unity Lifecycle

        protected override void Awake()
        {
            base.Awake();

            _audioService = ServiceLocator.Instance.Get<IAudioService>();
            _replayService = ServiceLocator.Instance.Get<IReplayService>();

            if (_replayService != null)
            {
                ViewModel = new ReplayViewModel(_replayService);
            }
            else
            {
                Debug.LogError("[ReplayView] Replay service not available");
            }
        }

        private void Update()
        {
            // 再生時間を更新
            ViewModel?.Tick(Time.unscaledDeltaTime);

            // ReplayPlaybackManagerにフレームを適用
            if (ViewModel != null && ReplayPlaybackManager.Instance != null)
            {
                ReplayPlaybackManager.Instance.ApplyFrameAtTime(ViewModel.CurrentTime);

                // カメラマネージャーに現在のフレームを設定
                if (ReplayCameraManager.Instance != null)
                {
                    var frame = ViewModel.GetCurrentFrame();
                    ReplayCameraManager.Instance.SetCurrentFrame(frame);
                }
            }

            // キーボードショートカット
            HandleKeyboardInput();
        }

        #endregion

        #region UI Setup

        protected override void OnRootVisualElementReady(VisualElement root)
        {
            base.OnRootVisualElementReady(root);

            GetUIElements();
            SetupSpeedDropdown();
            RegisterEventHandlers();
            UpdateUIFromViewModel();
        }

        private void GetUIElements()
        {
            // Container
            _uiContainer = Q<VisualElement>("UIContainer");

            // Header
            _headerPanel = Q<VisualElement>("HeaderPanel");
            _mapLabel = Q<Label>("MapLabel");
            _gameModeLabel = Q<Label>("GameModeLabel");
            _scoreLabel = Q<Label>("ScoreLabel");

            // Timeline
            _timelinePanel = Q<VisualElement>("TimelinePanel");
            _timelineSlider = Q<Slider>("TimelineSlider");
            _currentTimeLabel = Q<Label>("CurrentTimeLabel");
            _durationLabel = Q<Label>("DurationLabel");

            // Controls
            _controlsPanel = Q<VisualElement>("ControlsPanel");
            _skipBackButton = Q<Button>("SkipBackButton");
            _playPauseButton = Q<Button>("PlayPauseButton");
            _skipForwardButton = Q<Button>("SkipForwardButton");
            _stopButton = Q<Button>("StopButton");

            // Speed
            _speedDropdown = Q<DropdownField>("SpeedDropdown");

            // Highlights
            _highlightsPanel = Q<VisualElement>("HighlightsPanel");
            _prevHighlightButton = Q<Button>("PrevHighlightButton");
            _nextHighlightButton = Q<Button>("NextHighlightButton");
            _highlightLabel = Q<Label>("HighlightLabel");

            // Camera
            _cameraModeButton = Q<Button>("CameraModeButton");
            _cameraModeLabel = Q<Label>("CameraModeLabel");

            // Footer
            _toggleUIButton = Q<Button>("ToggleUIButton");
            _exitButton = Q<Button>("ExitButton");
        }

        private void SetupSpeedDropdown()
        {
            if (_speedDropdown == null || ViewModel == null)
            {
                return;
            }

            var choices = new List<string>();
            foreach (var speed in ViewModel.AvailableSpeeds)
            {
                choices.Add($"{speed:F2}x");
            }

            _speedDropdown.choices = choices;
            _speedDropdown.index = ViewModel.PlaybackSpeedIndex;
        }

        private void RegisterEventHandlers()
        {
            // Timeline
            _timelineSlider?.RegisterValueChangedCallback(OnTimelineChanged);

            // Controls
            _skipBackButton?.RegisterCallback<ClickEvent>(OnSkipBackClicked);
            _playPauseButton?.RegisterCallback<ClickEvent>(OnPlayPauseClicked);
            _skipForwardButton?.RegisterCallback<ClickEvent>(OnSkipForwardClicked);
            _stopButton?.RegisterCallback<ClickEvent>(OnStopClicked);

            // Speed
            _speedDropdown?.RegisterValueChangedCallback(OnSpeedChanged);

            // Highlights
            _prevHighlightButton?.RegisterCallback<ClickEvent>(OnPrevHighlightClicked);
            _nextHighlightButton?.RegisterCallback<ClickEvent>(OnNextHighlightClicked);

            // Camera
            _cameraModeButton?.RegisterCallback<ClickEvent>(OnCameraModeClicked);

            // Footer
            _toggleUIButton?.RegisterCallback<ClickEvent>(OnToggleUIClicked);
            _exitButton?.RegisterCallback<ClickEvent>(OnExitClicked);
        }

        private void UnregisterEventHandlers()
        {
            _timelineSlider?.UnregisterValueChangedCallback(OnTimelineChanged);

            _skipBackButton?.UnregisterCallback<ClickEvent>(OnSkipBackClicked);
            _playPauseButton?.UnregisterCallback<ClickEvent>(OnPlayPauseClicked);
            _skipForwardButton?.UnregisterCallback<ClickEvent>(OnSkipForwardClicked);
            _stopButton?.UnregisterCallback<ClickEvent>(OnStopClicked);

            _speedDropdown?.UnregisterValueChangedCallback(OnSpeedChanged);

            _prevHighlightButton?.UnregisterCallback<ClickEvent>(OnPrevHighlightClicked);
            _nextHighlightButton?.UnregisterCallback<ClickEvent>(OnNextHighlightClicked);

            _cameraModeButton?.UnregisterCallback<ClickEvent>(OnCameraModeClicked);

            _toggleUIButton?.UnregisterCallback<ClickEvent>(OnToggleUIClicked);
            _exitButton?.UnregisterCallback<ClickEvent>(OnExitClicked);
        }

        #endregion

        #region UI Update

        private void UpdateUIFromViewModel()
        {
            if (ViewModel == null)
            {
                return;
            }

            UpdateHeader();
            UpdateTimeline();
            UpdatePlayPauseButton();
            UpdateCameraMode();
            UpdateHighlights();
            UpdateUIVisibility();
        }

        private void UpdateHeader()
        {
            if (ViewModel == null)
            {
                return;
            }

            if (_mapLabel != null)
            {
                _mapLabel.text = ViewModel.MapName;
            }

            if (_gameModeLabel != null)
            {
                _gameModeLabel.text = ViewModel.GameMode;
            }

            if (_scoreLabel != null)
            {
                _scoreLabel.text = ViewModel.ScoreText;
            }
        }

        private void UpdateTimeline()
        {
            if (ViewModel == null)
            {
                return;
            }

            if (_timelineSlider != null)
            {
                _timelineSlider.highValue = ViewModel.Duration;
                _timelineSlider.SetValueWithoutNotify(ViewModel.CurrentTime);
            }

            if (_currentTimeLabel != null)
            {
                _currentTimeLabel.text = ViewModel.CurrentTimeText;
            }

            if (_durationLabel != null)
            {
                _durationLabel.text = ViewModel.DurationText;
            }
        }

        private void UpdatePlayPauseButton()
        {
            if (_playPauseButton == null || ViewModel == null)
            {
                return;
            }

            _playPauseButton.text = ViewModel.PlayPauseButtonText;
        }

        private void UpdateCameraMode()
        {
            if (_cameraModeLabel == null || ViewModel == null)
            {
                return;
            }

            _cameraModeLabel.text = ViewModel.CameraModeText;
        }

        private void UpdateHighlights()
        {
            if (ViewModel == null)
            {
                return;
            }

            bool hasHighlights = ViewModel.HasHighlights;

            if (_prevHighlightButton != null)
            {
                _prevHighlightButton.SetEnabled(hasHighlights);
            }

            if (_nextHighlightButton != null)
            {
                _nextHighlightButton.SetEnabled(hasHighlights);
            }

            if (_highlightLabel != null)
            {
                _highlightLabel.text = hasHighlights
                    ? $"Highlights ({ViewModel.Highlights.Count})"
                    : "No Highlights";
            }
        }

        private void UpdateUIVisibility()
        {
            if (_uiContainer == null || ViewModel == null)
            {
                return;
            }

            _uiContainer.style.display = ViewModel.IsUIVisible
                ? DisplayStyle.Flex
                : DisplayStyle.None;

            if (_toggleUIButton != null)
            {
                _toggleUIButton.text = ViewModel.IsUIVisible ? $"Hide UI ({_toggleUIKey})" : $"Show UI ({_toggleUIKey})";
            }
        }

        #endregion

        #region Keyboard Input

        private void HandleKeyboardInput()
        {
            if (ViewModel == null)
            {
                return;
            }

            // Toggle UI
            if (Input.GetKeyDown(_toggleUIKey))
            {
                ViewModel.ToggleUICommand.Execute(null);
            }

            // Play/Pause
            if (Input.GetKeyDown(_playPauseKey))
            {
                ViewModel.TogglePlayPauseCommand.Execute(null);
            }

            // Skip backward
            if (Input.GetKeyDown(_skipBackKey))
            {
                ViewModel.SkipBackwardCommand.Execute(null);
            }

            // Skip forward
            if (Input.GetKeyDown(_skipForwardKey))
            {
                ViewModel.SkipForwardCommand.Execute(null);
            }

            // Camera mode
            if (Input.GetKeyDown(_cameraModeKey))
            {
                ViewModel.CycleCameraModeCommand.Execute(null);
            }
        }

        #endregion

        #region Event Handlers

        private void OnTimelineChanged(ChangeEvent<float> evt)
        {
            ViewModel?.SeekTo(evt.newValue);
        }

        private void OnSkipBackClicked(ClickEvent evt)
        {
            PlayButtonSound();
            ViewModel?.SkipBackwardCommand.Execute(null);
        }

        private void OnPlayPauseClicked(ClickEvent evt)
        {
            PlayButtonSound();
            ViewModel?.TogglePlayPauseCommand.Execute(null);
        }

        private void OnSkipForwardClicked(ClickEvent evt)
        {
            PlayButtonSound();
            ViewModel?.SkipForwardCommand.Execute(null);
        }

        private void OnStopClicked(ClickEvent evt)
        {
            PlayButtonSound();
            ViewModel?.StopCommand.Execute(null);
        }

        private void OnSpeedChanged(ChangeEvent<string> evt)
        {
            if (ViewModel == null || _speedDropdown == null)
            {
                return;
            }

            ViewModel.PlaybackSpeedIndex = _speedDropdown.index;
        }

        private void OnPrevHighlightClicked(ClickEvent evt)
        {
            PlayButtonSound();
            ViewModel?.PreviousHighlightCommand.Execute(null);
        }

        private void OnNextHighlightClicked(ClickEvent evt)
        {
            PlayButtonSound();
            ViewModel?.NextHighlightCommand.Execute(null);
        }

        private void OnCameraModeClicked(ClickEvent evt)
        {
            PlayButtonSound();
            ViewModel?.CycleCameraModeCommand.Execute(null);
        }

        private void OnToggleUIClicked(ClickEvent evt)
        {
            PlayButtonSound();
            ViewModel?.ToggleUICommand.Execute(null);
        }

        private void OnExitClicked(ClickEvent evt)
        {
            PlayButtonSound();
            ViewModel?.ExitCommand.Execute(null);
        }

        #endregion

        #region Audio

        private void PlayButtonSound()
        {
            if (_audioService != null && _buttonClickSFX != null)
            {
                _audioService.PlaySfx(_buttonClickSFX);
            }
        }

        #endregion

        #region ViewModel Binding

        protected override void BindViewModel(ReplayViewModel viewModel)
        {
            base.BindViewModel(viewModel);
            viewModel.PropertyChanged += OnViewModelPropertyChanged;
            viewModel.TimeChanged += OnTimeChanged;
            viewModel.PlaybackStateChanged += OnPlaybackStateChanged;
            viewModel.CameraModeChanged += OnCameraModeChanged;
        }

        protected override void UnbindViewModel()
        {
            if (ViewModel != null)
            {
                ViewModel.PropertyChanged -= OnViewModelPropertyChanged;
                ViewModel.TimeChanged -= OnTimeChanged;
                ViewModel.PlaybackStateChanged -= OnPlaybackStateChanged;
                ViewModel.CameraModeChanged -= OnCameraModeChanged;
            }

            UnregisterEventHandlers();
            base.UnbindViewModel();
        }

        private void OnViewModelPropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            switch (e.PropertyName)
            {
                case nameof(ReplayViewModel.CurrentTime):
                case nameof(ReplayViewModel.CurrentTimeText):
                case nameof(ReplayViewModel.Progress):
                    UpdateTimeline();
                    break;

                case nameof(ReplayViewModel.IsPlaying):
                case nameof(ReplayViewModel.IsPaused):
                case nameof(ReplayViewModel.PlayPauseButtonText):
                    UpdatePlayPauseButton();
                    break;

                case nameof(ReplayViewModel.CurrentCameraMode):
                case nameof(ReplayViewModel.CameraModeText):
                    UpdateCameraMode();
                    break;

                case nameof(ReplayViewModel.IsUIVisible):
                    UpdateUIVisibility();
                    break;

                case nameof(ReplayViewModel.ReplayData):
                case nameof(ReplayViewModel.HasReplay):
                    UpdateUIFromViewModel();
                    break;

                case nameof(ReplayViewModel.Highlights):
                case nameof(ReplayViewModel.HasHighlights):
                    UpdateHighlights();
                    break;
            }
        }

        private void OnTimeChanged(object? sender, float time)
        {
            UpdateTimeline();
        }

        private void OnPlaybackStateChanged(object? sender, bool isPlaying)
        {
            UpdatePlayPauseButton();
        }

        private void OnCameraModeChanged(object? sender, ReplayViewModel.CameraMode mode)
        {
            UpdateCameraMode();

            // カメラマネージャーにモード変更を通知
            if (ReplayCameraManager.Instance != null)
            {
                // ViewModelのCameraModeをReplayCameraModeに変換
                var cameraMode = mode switch
                {
                    ReplayViewModel.CameraMode.Follow => ReplayCameraMode.Follow,
                    ReplayViewModel.CameraMode.Free => ReplayCameraMode.Free,
                    ReplayViewModel.CameraMode.Cinematic => ReplayCameraMode.Cinematic,
                    _ => ReplayCameraMode.Follow
                };
                ReplayCameraManager.Instance.SetCameraMode(cameraMode);
            }

            // フリーカメラモード切り替え時のUI設定
            bool isFreeCameraMode = mode == ReplayViewModel.CameraMode.Free;
            SetFreeCameraModeUI(isFreeCameraMode);

            if (isFreeCameraMode)
            {
                BlurUIFocus();
            }
        }

        /// <summary>
        /// UIのフォーカスを解除してカメラ入力を優先します
        /// </summary>
        private void BlurUIFocus()
        {
            // フォーカスを解除
            if (RootVisualElement != null)
            {
                RootVisualElement.focusController?.focusedElement?.Blur();
            }
        }

        /// <summary>
        /// フリーカメラモード用にUIの入力を無効化します
        /// </summary>
        private void SetFreeCameraModeUI(bool isFreeCameraMode)
        {
            if (_uiContainer == null)
            {
                return;
            }

            // フリーカメラモードではUIの入力を無効化（表示は維持）
            _uiContainer.pickingMode = isFreeCameraMode ? PickingMode.Ignore : PickingMode.Position;

            // ルート要素全体のpickingModeを設定
            // これにより「Screen position out of view frustum」エラーを防止
            if (RootVisualElement != null)
            {
                RootVisualElement.pickingMode = isFreeCameraMode ? PickingMode.Ignore : PickingMode.Position;
            }

            // スライダーのフォーカスを無効化
            if (_timelineSlider != null)
            {
                _timelineSlider.focusable = !isFreeCameraMode;
                _timelineSlider.pickingMode = isFreeCameraMode ? PickingMode.Ignore : PickingMode.Position;
            }

            // ドロップダウンのフォーカスを無効化
            if (_speedDropdown != null)
            {
                _speedDropdown.focusable = !isFreeCameraMode;
                _speedDropdown.pickingMode = isFreeCameraMode ? PickingMode.Ignore : PickingMode.Position;
            }

            // すべてのボタンのpickingModeを設定
            SetButtonPickingMode(_skipBackButton, isFreeCameraMode);
            SetButtonPickingMode(_playPauseButton, isFreeCameraMode);
            SetButtonPickingMode(_skipForwardButton, isFreeCameraMode);
            SetButtonPickingMode(_stopButton, isFreeCameraMode);
            SetButtonPickingMode(_prevHighlightButton, isFreeCameraMode);
            SetButtonPickingMode(_nextHighlightButton, isFreeCameraMode);
            SetButtonPickingMode(_cameraModeButton, isFreeCameraMode);
            SetButtonPickingMode(_toggleUIButton, isFreeCameraMode);
            SetButtonPickingMode(_exitButton, isFreeCameraMode);
        }

        private void SetButtonPickingMode(Button? button, bool isFreeCameraMode)
        {
            if (button != null)
            {
                button.pickingMode = isFreeCameraMode ? PickingMode.Ignore : PickingMode.Position;
            }
        }

        #endregion
    }
}
