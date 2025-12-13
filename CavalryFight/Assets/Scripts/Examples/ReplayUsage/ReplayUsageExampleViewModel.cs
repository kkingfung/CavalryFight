#nullable enable

using System;
using System.Collections.Generic;
using UnityEngine;
using CavalryFight.Core.MVVM;
using CavalryFight.Core.Services;
using CavalryFight.Core.Commands;

namespace CavalryFight.Services.Replay
{
    /// <summary>
    /// リプレイシステム使用例のViewModel
    /// </summary>
    /// <remarks>
    /// ReplayServiceの使用方法を示すサンプル実装です。
    /// 録画、再生、カメラ制御、ハイライト、保存/読込の例を含みます。
    /// </remarks>
    public class ReplayUsageExampleViewModel : ViewModelBase
    {
        #region Fields

        private readonly IReplayRecorder _replayRecorder;
        private readonly IReplayPlayer _replayPlayer;
        private IReplayCameraController? _currentCameraController;
        private Camera? _replayCamera;

        // 録画状態
        private bool _isRecording;
        private float _recordingTime;

        // 再生状態
        private bool _isPlaying;
        private bool _isPaused;
        private float _playbackTime;
        private float _playbackSpeed;
        private ReplayData? _currentReplay;

        // 保存されたリプレイ
        private List<string> _savedReplays = new List<string>();
        private string _selectedReplayName = string.Empty;

        // カメラモード
        private CameraMode _currentCameraMode = CameraMode.Free;

        #endregion

        #region Enums

        /// <summary>
        /// カメラモード
        /// </summary>
        public enum CameraMode
        {
            Free,
            FollowPlayer,
            OrbitPlayer
        }

        #endregion

        #region Properties - Recording

        /// <summary>
        /// 録画中かどうかを取得します
        /// </summary>
        public bool IsRecording
        {
            get { return _isRecording; }
            private set { SetProperty(ref _isRecording, value); }
        }

        /// <summary>
        /// 録画時間を取得します（秒）
        /// </summary>
        public float RecordingTime
        {
            get { return _recordingTime; }
            private set { SetProperty(ref _recordingTime, value); }
        }

        #endregion

        #region Properties - Playback

        /// <summary>
        /// 再生中かどうかを取得します
        /// </summary>
        public bool IsPlaying
        {
            get { return _isPlaying; }
            private set { SetProperty(ref _isPlaying, value); }
        }

        /// <summary>
        /// 一時停止中かどうかを取得します
        /// </summary>
        public bool IsPaused
        {
            get { return _isPaused; }
            private set { SetProperty(ref _isPaused, value); }
        }

        /// <summary>
        /// 再生時間を取得または設定します（秒）
        /// </summary>
        public float PlaybackTime
        {
            get { return _playbackTime; }
            set
            {
                if (SetProperty(ref _playbackTime, value))
                {
                    if (_isPlaying)
                    {
                        _replayPlayer.SeekTo(value);
                    }
                }
            }
        }

        /// <summary>
        /// 再生速度を取得または設定します
        /// </summary>
        public float PlaybackSpeed
        {
            get { return _playbackSpeed; }
            set
            {
                if (SetProperty(ref _playbackSpeed, value))
                {
                    _replayPlayer.PlaybackSpeed = value;
                }
            }
        }

        /// <summary>
        /// 現在のリプレイデータを取得します
        /// </summary>
        public ReplayData? CurrentReplay
        {
            get { return _currentReplay; }
            private set { SetProperty(ref _currentReplay, value); }
        }

        #endregion

        #region Properties - Saved Replays

        /// <summary>
        /// 保存されたリプレイのリストを取得します
        /// </summary>
        public List<string> SavedReplays
        {
            get { return _savedReplays; }
            private set { SetProperty(ref _savedReplays, value); }
        }

        /// <summary>
        /// 選択されたリプレイ名を取得または設定します
        /// </summary>
        public string SelectedReplayName
        {
            get { return _selectedReplayName; }
            set { SetProperty(ref _selectedReplayName, value); }
        }

        #endregion

        #region Properties - Camera

        /// <summary>
        /// 現在のカメラモードを取得または設定します
        /// </summary>
        public CameraMode CurrentCameraMode
        {
            get { return _currentCameraMode; }
            set
            {
                if (SetProperty(ref _currentCameraMode, value))
                {
                    SwitchCameraMode(value);
                }
            }
        }

        #endregion

        #region Commands

        /// <summary>
        /// 録画開始コマンド
        /// </summary>
        public ICommand StartRecordingCommand { get; }

        /// <summary>
        /// 録画停止コマンド
        /// </summary>
        public ICommand StopRecordingCommand { get; }

        /// <summary>
        /// 再生開始コマンド
        /// </summary>
        public ICommand StartPlaybackCommand { get; }

        /// <summary>
        /// 再生停止コマンド
        /// </summary>
        public ICommand StopPlaybackCommand { get; }

        /// <summary>
        /// 一時停止/再開コマンド
        /// </summary>
        public ICommand TogglePauseCommand { get; }

        /// <summary>
        /// リプレイ保存コマンド
        /// </summary>
        public ICommand SaveReplayCommand { get; }

        /// <summary>
        /// リプレイ読込コマンド
        /// </summary>
        public ICommand LoadReplayCommand { get; }

        /// <summary>
        /// リプレイリスト更新コマンド
        /// </summary>
        public ICommand RefreshReplaysCommand { get; }

        /// <summary>
        /// ハイライトジャンプコマンド
        /// </summary>
        public ICommand JumpToHighlightCommand { get; }

        #endregion

        #region Constructor

        /// <summary>
        /// ReplayUsageExampleViewModelの新しいインスタンスを初期化します
        /// </summary>
        public ReplayUsageExampleViewModel()
        {
            // サービスを取得
            _replayRecorder = ServiceLocator.Instance.Get<IReplayRecorder>()
                ?? throw new InvalidOperationException("ReplayRecorder is not registered.");
            _replayPlayer = ServiceLocator.Instance.Get<IReplayPlayer>()
                ?? throw new InvalidOperationException("ReplayPlayer is not registered.");

            // レコーダーのイベントを購読
            _replayRecorder.RecordingStarted += OnRecordingStarted;
            _replayRecorder.RecordingStopped += OnRecordingStopped;

            // プレイヤーのイベントを購読
            _replayPlayer.PlaybackStarted += OnPlaybackStarted;
            _replayPlayer.PlaybackStopped += OnPlaybackStopped;
            _replayPlayer.PlaybackPaused += OnPlaybackPaused;
            _replayPlayer.PlaybackResumed += OnPlaybackResumed;
            _replayPlayer.PlaybackTimeChanged += OnPlaybackTimeChanged;

            // コマンドを初期化
            StartRecordingCommand = new RelayCommand(ExecuteStartRecording, CanExecuteStartRecording);
            StopRecordingCommand = new RelayCommand(ExecuteStopRecording, CanExecuteStopRecording);
            StartPlaybackCommand = new RelayCommand(ExecuteStartPlayback, CanExecuteStartPlayback);
            StopPlaybackCommand = new RelayCommand(ExecuteStopPlayback, CanExecuteStopPlayback);
            TogglePauseCommand = new RelayCommand(ExecuteTogglePause, CanExecuteTogglePause);
            SaveReplayCommand = new RelayCommand(ExecuteSaveReplay, CanExecuteSaveReplay);
            LoadReplayCommand = new RelayCommand(ExecuteLoadReplay, CanExecuteLoadReplay);
            RefreshReplaysCommand = new RelayCommand(ExecuteRefreshReplays);
            JumpToHighlightCommand = new RelayCommand<int>(ExecuteJumpToHighlight, CanExecuteJumpToHighlight);

            // 初期化
            PlaybackSpeed = 1.0f;
            RefreshSavedReplays();

            Debug.Log("[ReplayUsageExample] ViewModel initialized.");
        }

        #endregion

        #region Recording Methods

        private void ExecuteStartRecording()
        {
            Debug.Log("[ReplayUsageExample] Starting recording...");
            _replayRecorder.StartRecording("TestMap", "Deathmatch", "Player1");
        }

        private bool CanExecuteStartRecording()
        {
            return !_replayRecorder.IsRecording && !_replayPlayer.IsPlaying;
        }

        private void ExecuteStopRecording()
        {
            Debug.Log("[ReplayUsageExample] Stopping recording...");
            var replay = _replayRecorder.StopRecording();

            if (replay != null)
            {
                CurrentReplay = replay;
                Debug.Log($"[ReplayUsageExample] Recording stopped. {replay.Frames.Count} frames recorded.");
            }
        }

        private bool CanExecuteStopRecording()
        {
            return _replayRecorder.IsRecording;
        }

        /// <summary>
        /// エンティティを録画対象として登録する例
        /// </summary>
        /// <param name="entityId">エンティティID</param>
        /// <param name="entityType">エンティティタイプ</param>
        /// <param name="gameObject">GameObject</param>
        public void RegisterEntityForRecording(string entityId, EntityType entityType, GameObject gameObject)
        {
            _replayRecorder.RegisterEntity(entityId, entityType, gameObject);
            Debug.Log($"[ReplayUsageExample] Entity registered: {entityId}");
        }

        /// <summary>
        /// イベントを記録する例
        /// </summary>
        public void RecordScoreEvent(string scorerId)
        {
            _replayRecorder.RecordEvent(ReplayEventType.Score, scorerId, $"{scorerId} scored!");
            Debug.Log($"[ReplayUsageExample] Score event recorded for {scorerId}");
        }

        #endregion

        #region Playback Methods

        private void ExecuteStartPlayback()
        {
            if (CurrentReplay == null)
            {
                Debug.LogWarning("[ReplayUsageExample] No replay to play!");
                return;
            }

            Debug.Log("[ReplayUsageExample] Starting playback...");
            _replayPlayer.StartPlayback(CurrentReplay, 0f);

            // カメラコントローラーを初期化
            InitializeCameraController();
        }

        private bool CanExecuteStartPlayback()
        {
            return CurrentReplay != null && !_replayPlayer.IsPlaying;
        }

        private void ExecuteStopPlayback()
        {
            Debug.Log("[ReplayUsageExample] Stopping playback...");
            _replayPlayer.StopPlayback();

            // カメラコントローラーを破棄
            DisposeCameraController();
        }

        private bool CanExecuteStopPlayback()
        {
            return _replayPlayer.IsPlaying;
        }

        private void ExecuteTogglePause()
        {
            if (_isPaused)
            {
                Debug.Log("[ReplayUsageExample] Resuming playback...");
                _replayPlayer.ResumePlayback();
            }
            else
            {
                Debug.Log("[ReplayUsageExample] Pausing playback...");
                _replayPlayer.PausePlayback();
            }
        }

        private bool CanExecuteTogglePause()
        {
            return _replayPlayer.IsPlaying;
        }

        #endregion

        #region Save/Load Methods

        private void ExecuteSaveReplay()
        {
            if (CurrentReplay == null)
            {
                Debug.LogWarning("[ReplayUsageExample] No replay to save!");
                return;
            }

            Debug.Log("[ReplayUsageExample] Saving replay...");
            bool success = _replayRecorder.SaveReplay(CurrentReplay);

            if (success)
            {
                Debug.Log("[ReplayUsageExample] Replay saved successfully.");
                RefreshSavedReplays();
            }
            else
            {
                Debug.LogError("[ReplayUsageExample] Failed to save replay.");
            }
        }

        private bool CanExecuteSaveReplay()
        {
            return CurrentReplay != null;
        }

        private void ExecuteLoadReplay()
        {
            if (string.IsNullOrEmpty(SelectedReplayName))
            {
                Debug.LogWarning("[ReplayUsageExample] No replay selected!");
                return;
            }

            Debug.Log($"[ReplayUsageExample] Loading replay: {SelectedReplayName}");
            var replay = _replayPlayer.LoadReplay(SelectedReplayName);

            if (replay != null)
            {
                CurrentReplay = replay;
                Debug.Log($"[ReplayUsageExample] Replay loaded: {replay.Frames.Count} frames, {replay.Events.Count} events.");
            }
            else
            {
                Debug.LogError($"[ReplayUsageExample] Failed to load replay: {SelectedReplayName}");
            }
        }

        private bool CanExecuteLoadReplay()
        {
            return !string.IsNullOrEmpty(SelectedReplayName);
        }

        private void ExecuteRefreshReplays()
        {
            RefreshSavedReplays();
        }

        private void RefreshSavedReplays()
        {
            SavedReplays = _replayPlayer.GetSavedReplays();
            Debug.Log($"[ReplayUsageExample] Found {SavedReplays.Count} saved replays.");
        }

        #endregion

        #region Highlight Methods

        private void ExecuteJumpToHighlight(int highlightIndex)
        {
            if (CurrentReplay == null || highlightIndex < 0 || highlightIndex >= CurrentReplay.Highlights.Count)
            {
                Debug.LogWarning("[ReplayUsageExample] Invalid highlight index!");
                return;
            }

            var highlight = CurrentReplay.Highlights[highlightIndex];
            Debug.Log($"[ReplayUsageExample] Jumping to highlight: {highlight.Title}");
            _replayPlayer.JumpToHighlight(highlight);
        }

        private bool CanExecuteJumpToHighlight(int highlightIndex)
        {
            return _replayPlayer.IsPlaying && CurrentReplay != null && highlightIndex >= 0 && highlightIndex < CurrentReplay.Highlights.Count;
        }

        #endregion

        #region Camera Methods

        private void InitializeCameraController()
        {
            // リプレイカメラを取得（または作成）
            _replayCamera = Camera.main; // TODO: 専用のリプレイカメラを取得

            if (_replayCamera == null)
            {
                Debug.LogError("[ReplayUsageExample] No camera found!");
                return;
            }

            SwitchCameraMode(_currentCameraMode);
        }

        private void SwitchCameraMode(CameraMode mode)
        {
            // 既存のコントローラーを破棄
            DisposeCameraController();

            if (_replayCamera == null)
            {
                return;
            }

            // 新しいコントローラーを作成
            switch (mode)
            {
                case CameraMode.Free:
                    _currentCameraController = new FreeCameraController();
                    break;

                case CameraMode.FollowPlayer:
                    _currentCameraController = new CinematicCameraController(CinematicCameraController.CinematicMode.FollowPlayer);
                    break;

                case CameraMode.OrbitPlayer:
                    _currentCameraController = new CinematicCameraController(CinematicCameraController.CinematicMode.OrbitPlayer);
                    break;
            }

            _currentCameraController?.Initialize(_replayCamera);
            Debug.Log($"[ReplayUsageExample] Camera mode switched to: {mode}");
        }

        private void DisposeCameraController()
        {
            _currentCameraController?.Dispose();
            _currentCameraController = null;
        }

        /// <summary>
        /// カメラを更新します（MonoBehaviourのUpdateから呼ぶ必要があります）
        /// </summary>
        /// <param name="deltaTime">前フレームからの経過時間</param>
        public void UpdateCamera(float deltaTime)
        {
            if (_currentCameraController != null && _isPlaying)
            {
                var currentFrame = _replayPlayer.GetCurrentFrame();
                _currentCameraController.UpdateCamera(deltaTime, currentFrame);
            }
        }

        #endregion

        #region Event Handlers

        private void OnRecordingStarted(object? sender, EventArgs e)
        {
            IsRecording = true;
            Debug.Log("[ReplayUsageExample] Recording started event received.");
        }

        private void OnRecordingStopped(object? sender, ReplayRecordingStoppedEventArgs e)
        {
            IsRecording = false;
            RecordingTime = 0f;
            Debug.Log("[ReplayUsageExample] Recording stopped event received.");
        }

        private void OnPlaybackStarted(object? sender, ReplayPlaybackStartedEventArgs e)
        {
            IsPlaying = true;
            IsPaused = false;
            Debug.Log("[ReplayUsageExample] Playback started event received.");
        }

        private void OnPlaybackStopped(object? sender, EventArgs e)
        {
            IsPlaying = false;
            IsPaused = false;
            PlaybackTime = 0f;
            Debug.Log("[ReplayUsageExample] Playback stopped event received.");
        }

        private void OnPlaybackPaused(object? sender, EventArgs e)
        {
            IsPaused = true;
            Debug.Log("[ReplayUsageExample] Playback paused event received.");
        }

        private void OnPlaybackResumed(object? sender, EventArgs e)
        {
            IsPaused = false;
            Debug.Log("[ReplayUsageExample] Playback resumed event received.");
        }

        private void OnPlaybackTimeChanged(object? sender, ReplayTimeChangedEventArgs e)
        {
            PlaybackTime = e.CurrentTime;
            RecordingTime = _replayRecorder.RecordingTime;
        }

        #endregion

        #region Dispose

        /// <summary>
        /// リソースを解放します
        /// </summary>
        protected override void OnDispose()
        {
            // レコーダーのイベント購読を解除
            _replayRecorder.RecordingStarted -= OnRecordingStarted;
            _replayRecorder.RecordingStopped -= OnRecordingStopped;

            // プレイヤーのイベント購読を解除
            _replayPlayer.PlaybackStarted -= OnPlaybackStarted;
            _replayPlayer.PlaybackStopped -= OnPlaybackStopped;
            _replayPlayer.PlaybackPaused -= OnPlaybackPaused;
            _replayPlayer.PlaybackResumed -= OnPlaybackResumed;
            _replayPlayer.PlaybackTimeChanged -= OnPlaybackTimeChanged;

            // カメラコントローラーを破棄
            DisposeCameraController();

            Debug.Log("[ReplayUsageExample] ViewModel disposed.");
        }

        #endregion
    }
}
