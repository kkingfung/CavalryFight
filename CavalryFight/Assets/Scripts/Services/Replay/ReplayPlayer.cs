#nullable enable

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;

namespace CavalryFight.Services.Replay
{
    /// <summary>
    /// リプレイ再生サービスの実装
    /// </summary>
    /// <remarks>
    /// 録画されたリプレイの再生を管理します。
    /// ファイルからの読込、再生制御、シーク、ハイライトジャンプ等を提供します。
    /// </remarks>
    public class ReplayPlayer : IReplayPlayer
    {
        #region Constants

        private const string REPLAY_FOLDER = "Replays";
        private const string REPLAY_EXTENSION = ".replay";

        #endregion

        #region Fields

        private bool _isPlaying = false;
        private bool _isPaused = false;
        private ReplayData? _currentPlayback = null;
        private float _playbackTime = 0f;
        private float _playbackSpeed = 1.0f;
        private Dictionary<string, GameObject> _playbackEntities = new Dictionary<string, GameObject>();

        #endregion

        #region Properties

        /// <summary>
        /// 現在再生中かどうかを取得します
        /// </summary>
        public bool IsPlaying => _isPlaying;

        /// <summary>
        /// 再生が一時停止中かどうかを取得します
        /// </summary>
        public bool IsPaused => _isPaused;

        /// <summary>
        /// 現在再生中のリプレイデータを取得します
        /// </summary>
        public ReplayData? CurrentPlayback => _currentPlayback;

        /// <summary>
        /// 現在の再生時刻（秒）を取得します
        /// </summary>
        public float PlaybackTime => _playbackTime;

        /// <summary>
        /// 再生速度を取得または設定します（1.0が通常速度）
        /// </summary>
        public float PlaybackSpeed
        {
            get { return _playbackSpeed; }
            set { _playbackSpeed = Mathf.Clamp(value, 0.1f, 10f); }
        }

        #endregion

        #region Events

        /// <summary>
        /// 再生が開始された時に発生します
        /// </summary>
        public event EventHandler<ReplayPlaybackStartedEventArgs>? PlaybackStarted;

        /// <summary>
        /// 再生が停止された時に発生します
        /// </summary>
        public event EventHandler? PlaybackStopped;

        /// <summary>
        /// 再生が一時停止された時に発生します
        /// </summary>
        public event EventHandler? PlaybackPaused;

        /// <summary>
        /// 再生が再開された時に発生します
        /// </summary>
        public event EventHandler? PlaybackResumed;

        /// <summary>
        /// 再生時刻が変更された時に発生します（シークを含む）
        /// </summary>
        public event EventHandler<ReplayTimeChangedEventArgs>? PlaybackTimeChanged;

        #endregion

        #region Constructor

        /// <summary>
        /// ReplayPlayerの新しいインスタンスを初期化します
        /// </summary>
        public ReplayPlayer()
        {
            Debug.Log("[ReplayPlayer] Instance created.");
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// リプレイの再生を開始します
        /// </summary>
        /// <param name="replay">再生するリプレイデータ</param>
        /// <param name="startTime">開始時刻（秒、デフォルトは0）</param>
        public void StartPlayback(ReplayData replay, float startTime = 0f)
        {
            if (_isPlaying)
            {
                Debug.LogWarning("[ReplayPlayer] Already playing. Stop current playback first.");
                return;
            }

            Debug.Log($"[ReplayPlayer] Starting playback: {replay.ReplayId}");

            _currentPlayback = replay;
            _playbackTime = startTime;
            _isPlaying = true;
            _isPaused = false;

            // イベントを発火
            PlaybackStarted?.Invoke(this, new ReplayPlaybackStartedEventArgs(replay, startTime));

            Debug.Log("[ReplayPlayer] Playback started.");
        }

        /// <summary>
        /// リプレイの再生を停止します
        /// </summary>
        public void StopPlayback()
        {
            if (!_isPlaying)
            {
                return;
            }

            Debug.Log("[ReplayPlayer] Stopping playback...");

            _isPlaying = false;
            _isPaused = false;
            _currentPlayback = null;
            _playbackTime = 0f;
            _playbackEntities.Clear();

            // イベントを発火
            PlaybackStopped?.Invoke(this, EventArgs.Empty);

            Debug.Log("[ReplayPlayer] Playback stopped.");
        }

        /// <summary>
        /// リプレイの再生を一時停止します
        /// </summary>
        public void PausePlayback()
        {
            if (!_isPlaying || _isPaused)
            {
                return;
            }

            Debug.Log("[ReplayPlayer] Pausing playback...");

            _isPaused = true;

            // イベントを発火
            PlaybackPaused?.Invoke(this, EventArgs.Empty);

            Debug.Log("[ReplayPlayer] Playback paused.");
        }

        /// <summary>
        /// リプレイの再生を再開します
        /// </summary>
        public void ResumePlayback()
        {
            if (!_isPlaying || !_isPaused)
            {
                return;
            }

            Debug.Log("[ReplayPlayer] Resuming playback...");

            _isPaused = false;

            // イベントを発火
            PlaybackResumed?.Invoke(this, EventArgs.Empty);

            Debug.Log("[ReplayPlayer] Playback resumed.");
        }

        /// <summary>
        /// 指定した時刻にシークします
        /// </summary>
        /// <param name="time">シーク先の時刻（秒）</param>
        public void SeekTo(float time)
        {
            if (!_isPlaying || _currentPlayback == null)
            {
                return;
            }

            _playbackTime = Mathf.Clamp(time, 0f, _currentPlayback.MatchDuration);

            // イベントを発火
            PlaybackTimeChanged?.Invoke(this, new ReplayTimeChangedEventArgs(_playbackTime));

            Debug.Log($"[ReplayPlayer] Seeked to {_playbackTime:F2}s");
        }

        /// <summary>
        /// ハイライトの開始位置にジャンプします
        /// </summary>
        /// <param name="highlight">ジャンプ先のハイライト</param>
        public void JumpToHighlight(ReplayHighlight highlight)
        {
            if (!_isPlaying)
            {
                return;
            }

            SeekTo(highlight.StartTimestamp);

            Debug.Log($"[ReplayPlayer] Jumped to highlight: {highlight.Title}");
        }

        /// <summary>
        /// 現在の再生フレームを取得します
        /// </summary>
        /// <returns>現在のフレーム（再生中でない場合はnull）</returns>
        public ReplayFrame? GetCurrentFrame()
        {
            if (!_isPlaying || _currentPlayback == null)
            {
                return null;
            }

            return _currentPlayback.GetInterpolatedFrame(_playbackTime);
        }

        /// <summary>
        /// 再生の更新処理（MonoBehaviourのUpdateから呼ぶ必要があります）
        /// </summary>
        /// <param name="deltaTime">前フレームからの経過時間</param>
        public void UpdatePlayback(float deltaTime)
        {
            if (!_isPlaying || _isPaused || _currentPlayback == null)
            {
                return;
            }

            // 再生時刻を進める
            _playbackTime += deltaTime * _playbackSpeed;

            // 終端に達したら停止
            if (_playbackTime >= _currentPlayback.MatchDuration)
            {
                _playbackTime = _currentPlayback.MatchDuration;
                StopPlayback();
                return;
            }

            // イベントを発火
            PlaybackTimeChanged?.Invoke(this, new ReplayTimeChangedEventArgs(_playbackTime));
        }

        /// <summary>
        /// ファイルからリプレイを読み込みます
        /// </summary>
        /// <param name="fileName">ファイル名（拡張子なし）</param>
        /// <returns>読み込まれたリプレイデータ（失敗時はnull）</returns>
        public ReplayData? LoadReplay(string fileName)
        {
            try
            {
                string filePath = Path.Combine(GetReplayFolderPath(), fileName + REPLAY_EXTENSION);
                var replay = ReplayData.LoadFromFile(filePath);

                if (replay != null)
                {
                    Debug.Log($"[ReplayPlayer] Replay loaded: {fileName}");
                }

                return replay;
            }
            catch (Exception ex)
            {
                Debug.LogError($"[ReplayPlayer] Failed to load replay: {ex.Message}");
                return null;
            }
        }

        /// <summary>
        /// 保存されているリプレイのリストを取得します
        /// </summary>
        /// <returns>リプレイファイル名のリスト（拡張子なし）</returns>
        public List<string> GetSavedReplays()
        {
            try
            {
                string replayPath = GetReplayFolderPath();
                if (!Directory.Exists(replayPath))
                {
                    return new List<string>();
                }

                var files = Directory.GetFiles(replayPath, "*" + REPLAY_EXTENSION);
                var fileNames = files.Select(f => Path.GetFileNameWithoutExtension(f)).ToList();

                Debug.Log($"[ReplayPlayer] Found {fileNames.Count} saved replays.");
                return fileNames;
            }
            catch (Exception ex)
            {
                Debug.LogError($"[ReplayPlayer] Failed to get saved replays: {ex.Message}");
                return new List<string>();
            }
        }

        /// <summary>
        /// リプレイファイルを削除します
        /// </summary>
        /// <param name="fileName">ファイル名（拡張子なし）</param>
        /// <returns>削除に成功したかどうか</returns>
        public bool DeleteReplay(string fileName)
        {
            try
            {
                string filePath = Path.Combine(GetReplayFolderPath(), fileName + REPLAY_EXTENSION);
                if (File.Exists(filePath))
                {
                    File.Delete(filePath);
                    Debug.Log($"[ReplayPlayer] Replay deleted: {fileName}");
                    return true;
                }
                else
                {
                    Debug.LogWarning($"[ReplayPlayer] Replay file not found: {fileName}");
                    return false;
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"[ReplayPlayer] Failed to delete replay: {ex.Message}");
                return false;
            }
        }

        #endregion

        #region Private Methods

        private string GetReplayFolderPath()
        {
            return Path.Combine(Application.persistentDataPath, REPLAY_FOLDER);
        }

        #endregion
    }
}
