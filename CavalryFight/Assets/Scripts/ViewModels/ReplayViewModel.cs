#nullable enable

using System;
using System.Collections.Generic;
using CavalryFight.Core.Commands;
using CavalryFight.Core.MVVM;
using CavalryFight.Core.Services;
using CavalryFight.Services.Replay;
using CavalryFight.Services.SceneManagement;
using UnityEngine;

namespace CavalryFight.ViewModels
{
    /// <summary>
    /// リプレイ視聴画面のViewModel
    /// </summary>
    /// <remarks>
    /// リプレイの再生制御、カメラ操作、ハイライト機能を提供します。
    /// </remarks>
    public class ReplayViewModel : ViewModelBase
    {
        #region Fields

        private readonly IReplayService _replayService;
        private readonly ISceneManagementService? _sceneManagementService;

        private ReplayData? _replayData;
        private bool _isPlaying;
        private bool _isPaused;
        private float _currentTime;
        private float _playbackSpeed = 1.0f;
        private int _selectedHighlightIndex = -1;
        private CameraMode _cameraMode = CameraMode.Follow;
        private bool _isUIVisible = true;

        #endregion

        #region Enums

        /// <summary>
        /// カメラモード
        /// </summary>
        public enum CameraMode
        {
            /// <summary>プレイヤー追従</summary>
            Follow,
            /// <summary>フリーカメラ</summary>
            Free,
            /// <summary>シネマティック</summary>
            Cinematic
        }

        #endregion

        #region Properties

        /// <summary>
        /// リプレイデータ
        /// </summary>
        public ReplayData? ReplayData
        {
            get => _replayData;
            private set
            {
                if (SetProperty(ref _replayData, value))
                {
                    OnPropertyChanged(nameof(HasReplay));
                    OnPropertyChanged(nameof(Duration));
                    OnPropertyChanged(nameof(DurationText));
                    OnPropertyChanged(nameof(Highlights));
                    OnPropertyChanged(nameof(HasHighlights));
                    OnPropertyChanged(nameof(MapName));
                    OnPropertyChanged(nameof(GameMode));
                    OnPropertyChanged(nameof(ScoreText));
                    OnPropertyChanged(nameof(ResultText));
                }
            }
        }

        /// <summary>
        /// リプレイがロードされているか
        /// </summary>
        public bool HasReplay => ReplayData != null;

        /// <summary>
        /// 再生中かどうか
        /// </summary>
        public bool IsPlaying
        {
            get => _isPlaying;
            private set
            {
                if (SetProperty(ref _isPlaying, value))
                {
                    OnPropertyChanged(nameof(PlayPauseButtonText));
                }
            }
        }

        /// <summary>
        /// 一時停止中かどうか
        /// </summary>
        public bool IsPaused
        {
            get => _isPaused;
            private set
            {
                if (SetProperty(ref _isPaused, value))
                {
                    OnPropertyChanged(nameof(PlayPauseButtonText));
                }
            }
        }

        /// <summary>
        /// 再生/一時停止ボタンのテキスト
        /// </summary>
        public string PlayPauseButtonText => (IsPaused || !IsPlaying) ? "Play" : "Pause";

        /// <summary>
        /// 現在の再生時間（秒）
        /// </summary>
        public float CurrentTime
        {
            get => _currentTime;
            set
            {
                float clampedValue = Mathf.Clamp(value, 0, Duration);
                if (SetProperty(ref _currentTime, clampedValue))
                {
                    OnPropertyChanged(nameof(CurrentTimeText));
                    OnPropertyChanged(nameof(Progress));
                    TimeChanged?.Invoke(this, _currentTime);
                }
            }
        }

        /// <summary>
        /// リプレイの総時間（秒）
        /// </summary>
        public float Duration => ReplayData?.MatchDuration ?? 0f;

        /// <summary>
        /// 再生進捗（0-1）
        /// </summary>
        public float Progress => Duration > 0 ? CurrentTime / Duration : 0f;

        /// <summary>
        /// 現在時間のテキスト表示
        /// </summary>
        public string CurrentTimeText
        {
            get
            {
                int minutes = (int)(CurrentTime / 60f);
                int seconds = (int)(CurrentTime % 60f);
                return $"{minutes}:{seconds:D2}";
            }
        }

        /// <summary>
        /// 総時間のテキスト表示
        /// </summary>
        public string DurationText
        {
            get
            {
                int minutes = (int)(Duration / 60f);
                int seconds = (int)(Duration % 60f);
                return $"{minutes}:{seconds:D2}";
            }
        }

        /// <summary>
        /// 再生速度
        /// </summary>
        public float PlaybackSpeed
        {
            get => _playbackSpeed;
            set
            {
                float clampedValue = Mathf.Clamp(value, 0.25f, 4.0f);
                if (SetProperty(ref _playbackSpeed, clampedValue))
                {
                    OnPropertyChanged(nameof(PlaybackSpeedText));
                    OnPropertyChanged(nameof(PlaybackSpeedIndex));
                    PlaybackSpeedChanged?.Invoke(this, _playbackSpeed);
                }
            }
        }

        /// <summary>
        /// 再生速度テキスト
        /// </summary>
        public string PlaybackSpeedText => $"{PlaybackSpeed:F2}x";

        /// <summary>
        /// 利用可能な再生速度リスト
        /// </summary>
        public List<float> AvailableSpeeds { get; } = new List<float>
        {
            0.25f, 0.5f, 1.0f, 1.5f, 2.0f, 4.0f
        };

        /// <summary>
        /// 現在の再生速度のインデックス
        /// </summary>
        public int PlaybackSpeedIndex
        {
            get
            {
                int index = AvailableSpeeds.IndexOf(PlaybackSpeed);
                return index >= 0 ? index : 2; // デフォルトは1.0x
            }
            set
            {
                if (value >= 0 && value < AvailableSpeeds.Count)
                {
                    PlaybackSpeed = AvailableSpeeds[value];
                }
            }
        }

        /// <summary>
        /// カメラモード
        /// </summary>
        public CameraMode CurrentCameraMode
        {
            get => _cameraMode;
            set
            {
                if (SetProperty(ref _cameraMode, value))
                {
                    OnPropertyChanged(nameof(CameraModeText));
                    CameraModeChanged?.Invoke(this, _cameraMode);
                }
            }
        }

        /// <summary>
        /// カメラモードのテキスト表示
        /// </summary>
        public string CameraModeText => CurrentCameraMode switch
        {
            CameraMode.Follow => "Follow",
            CameraMode.Free => "Free",
            CameraMode.Cinematic => "Cinematic",
            _ => "Follow"
        };

        /// <summary>
        /// ハイライトリスト
        /// </summary>
        public List<ReplayHighlight> Highlights => ReplayData?.Highlights ?? new List<ReplayHighlight>();

        /// <summary>
        /// ハイライトがあるかどうか
        /// </summary>
        public bool HasHighlights => Highlights.Count > 0;

        /// <summary>
        /// 選択中のハイライトインデックス
        /// </summary>
        public int SelectedHighlightIndex
        {
            get => _selectedHighlightIndex;
            set => SetProperty(ref _selectedHighlightIndex, value);
        }

        /// <summary>
        /// UIが表示されているか
        /// </summary>
        public bool IsUIVisible
        {
            get => _isUIVisible;
            set => SetProperty(ref _isUIVisible, value);
        }

        /// <summary>
        /// マップ名
        /// </summary>
        public string MapName => ReplayData?.MapName.ToString() ?? "";

        /// <summary>
        /// ゲームモード
        /// </summary>
        public string GameMode => ReplayData?.GameMode ?? "";

        /// <summary>
        /// スコアテキスト
        /// </summary>
        public string ScoreText => ReplayData != null
            ? $"{ReplayData.FinalPlayerScore} - {ReplayData.FinalEnemyScore}"
            : "";

        /// <summary>
        /// 結果テキスト
        /// </summary>
        public string ResultText
        {
            get
            {
                if (ReplayData == null)
                {
                    return "";
                }
                if (ReplayData.FinalPlayerScore > ReplayData.FinalEnemyScore)
                {
                    return "Victory";
                }
                if (ReplayData.FinalPlayerScore < ReplayData.FinalEnemyScore)
                {
                    return "Defeat";
                }
                return "Draw";
            }
        }

        #endregion

        #region Commands

        /// <summary>
        /// 再生/一時停止切り替えコマンド
        /// </summary>
        public ICommand TogglePlayPauseCommand { get; }

        /// <summary>
        /// 停止コマンド
        /// </summary>
        public ICommand StopCommand { get; }

        /// <summary>
        /// 10秒戻るコマンド
        /// </summary>
        public ICommand SkipBackwardCommand { get; }

        /// <summary>
        /// 10秒進むコマンド
        /// </summary>
        public ICommand SkipForwardCommand { get; }

        /// <summary>
        /// 前のハイライトへコマンド
        /// </summary>
        public ICommand PreviousHighlightCommand { get; }

        /// <summary>
        /// 次のハイライトへコマンド
        /// </summary>
        public ICommand NextHighlightCommand { get; }

        /// <summary>
        /// カメラモード切り替えコマンド
        /// </summary>
        public ICommand CycleCameraModeCommand { get; }

        /// <summary>
        /// UI表示切り替えコマンド
        /// </summary>
        public ICommand ToggleUICommand { get; }

        /// <summary>
        /// 終了コマンド
        /// </summary>
        public ICommand ExitCommand { get; }

        #endregion

        #region Events

        /// <summary>
        /// 再生時間が変更された時
        /// </summary>
        public event EventHandler<float>? TimeChanged;

        /// <summary>
        /// 再生速度が変更された時
        /// </summary>
        public event EventHandler<float>? PlaybackSpeedChanged;

        /// <summary>
        /// カメラモードが変更された時
        /// </summary>
        public event EventHandler<CameraMode>? CameraModeChanged;

        /// <summary>
        /// 再生状態が変更された時
        /// </summary>
        public event EventHandler<bool>? PlaybackStateChanged;

        /// <summary>
        /// 終了リクエスト
        /// </summary>
        public event EventHandler? ExitRequested;

        #endregion

        #region Constructor

        /// <summary>
        /// ReplayViewModelの新しいインスタンスを初期化します
        /// </summary>
        /// <param name="replayService">リプレイサービス</param>
        public ReplayViewModel(IReplayService replayService)
        {
            _replayService = replayService ?? throw new ArgumentNullException(nameof(replayService));
            _sceneManagementService = ServiceLocator.Instance.Get<ISceneManagementService>();

            // コマンド初期化
            TogglePlayPauseCommand = new RelayCommand(ExecuteTogglePlayPause, () => HasReplay);
            StopCommand = new RelayCommand(ExecuteStop, () => HasReplay);
            SkipBackwardCommand = new RelayCommand(() => Seek(-10f), () => HasReplay);
            SkipForwardCommand = new RelayCommand(() => Seek(10f), () => HasReplay);
            PreviousHighlightCommand = new RelayCommand(ExecutePreviousHighlight, () => HasHighlights);
            NextHighlightCommand = new RelayCommand(ExecuteNextHighlight, () => HasHighlights);
            CycleCameraModeCommand = new RelayCommand(ExecuteCycleCameraMode);
            ToggleUICommand = new RelayCommand(() => IsUIVisible = !IsUIVisible);
            ExitCommand = new RelayCommand(ExecuteExit);

            // 現在選択されているリプレイをロード
            LoadCurrentReplay();

            Debug.Log("[ReplayViewModel] Initialized");
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// 現在のリプレイをロード
        /// </summary>
        public void LoadCurrentReplay()
        {
            ReplayData = _replayService.CurrentReplay;
            if (ReplayData != null)
            {
                Debug.Log($"[ReplayViewModel] Loaded replay: {ReplayData.ReplayId} ({Duration:F1}s)");
            }
            else
            {
                Debug.LogWarning("[ReplayViewModel] No replay data available");
            }
        }

        /// <summary>
        /// 再生開始
        /// </summary>
        public void Play()
        {
            if (!HasReplay)
            {
                return;
            }

            IsPlaying = true;
            IsPaused = false;
            PlaybackStateChanged?.Invoke(this, true);
            Debug.Log("[ReplayViewModel] Playing");
        }

        /// <summary>
        /// 一時停止
        /// </summary>
        public void Pause()
        {
            IsPaused = true;
            PlaybackStateChanged?.Invoke(this, false);
            Debug.Log("[ReplayViewModel] Paused");
        }

        /// <summary>
        /// シーク（相対）
        /// </summary>
        /// <param name="deltaSeconds">移動する秒数（負の値で戻る）</param>
        public void Seek(float deltaSeconds)
        {
            CurrentTime += deltaSeconds;
        }

        /// <summary>
        /// 指定時間にジャンプ
        /// </summary>
        /// <param name="time">ジャンプ先の時間（秒）</param>
        public void SeekTo(float time)
        {
            CurrentTime = time;
        }

        /// <summary>
        /// 進捗でシーク（0-1）
        /// </summary>
        /// <param name="progress">進捗（0-1）</param>
        public void SeekToProgress(float progress)
        {
            CurrentTime = progress * Duration;
        }

        /// <summary>
        /// フレーム更新（Viewから呼ばれる）
        /// </summary>
        /// <param name="deltaTime">経過時間</param>
        public void Tick(float deltaTime)
        {
            if (!IsPlaying || IsPaused || !HasReplay)
            {
                return;
            }

            CurrentTime += deltaTime * PlaybackSpeed;

            // 終端に達したら一時停止
            if (CurrentTime >= Duration)
            {
                CurrentTime = Duration;
                Pause();
                Debug.Log("[ReplayViewModel] Reached end of replay");
            }
        }

        /// <summary>
        /// 現在のフレームを取得
        /// </summary>
        /// <returns>現在のリプレイフレーム</returns>
        public ReplayFrame? GetCurrentFrame()
        {
            return ReplayData?.GetInterpolatedFrame(CurrentTime);
        }

        #endregion

        #region Command Methods

        private void ExecuteTogglePlayPause()
        {
            if (IsPaused || !IsPlaying)
            {
                Play();
            }
            else
            {
                Pause();
            }
        }

        private void ExecuteStop()
        {
            IsPlaying = false;
            IsPaused = false;
            CurrentTime = 0;
            PlaybackStateChanged?.Invoke(this, false);
            Debug.Log("[ReplayViewModel] Stopped");
        }

        private void ExecutePreviousHighlight()
        {
            if (!HasHighlights)
            {
                return;
            }

            // 現在時間より前のハイライトを探す
            for (int i = Highlights.Count - 1; i >= 0; i--)
            {
                if (Highlights[i].StartTimestamp < CurrentTime - 0.5f)
                {
                    SelectedHighlightIndex = i;
                    SeekTo(Highlights[i].StartTimestamp);
                    Debug.Log($"[ReplayViewModel] Jump to highlight {i}: {Highlights[i].Description}");
                    return;
                }
            }

            // 見つからなければ最後のハイライトへ
            SelectedHighlightIndex = Highlights.Count - 1;
            SeekTo(Highlights[SelectedHighlightIndex].StartTimestamp);
        }

        private void ExecuteNextHighlight()
        {
            if (!HasHighlights)
            {
                return;
            }

            // 現在時間より後のハイライトを探す
            for (int i = 0; i < Highlights.Count; i++)
            {
                if (Highlights[i].StartTimestamp > CurrentTime + 0.5f)
                {
                    SelectedHighlightIndex = i;
                    SeekTo(Highlights[i].StartTimestamp);
                    Debug.Log($"[ReplayViewModel] Jump to highlight {i}: {Highlights[i].Description}");
                    return;
                }
            }

            // 見つからなければ最初のハイライトへ
            SelectedHighlightIndex = 0;
            SeekTo(Highlights[0].StartTimestamp);
        }

        private void ExecuteCycleCameraMode()
        {
            CurrentCameraMode = CurrentCameraMode switch
            {
                CameraMode.Follow => CameraMode.Free,
                CameraMode.Free => CameraMode.Cinematic,
                CameraMode.Cinematic => CameraMode.Follow,
                _ => CameraMode.Follow
            };
            Debug.Log($"[ReplayViewModel] Camera mode: {CameraModeText}");
        }

        private void ExecuteExit()
        {
            Debug.Log("[ReplayViewModel] Exiting replay");
            ExitRequested?.Invoke(this, EventArgs.Empty);
            _sceneManagementService?.LoadMainMenu();
        }

        #endregion

        #region Dispose

        /// <summary>
        /// リソースを解放します
        /// </summary>
        protected override void OnDispose()
        {
            base.OnDispose();
            Debug.Log("[ReplayViewModel] Disposed");
        }

        #endregion
    }
}
