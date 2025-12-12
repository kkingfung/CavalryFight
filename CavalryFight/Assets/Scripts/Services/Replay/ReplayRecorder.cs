#nullable enable

using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using CavalryFight.Core.Services;

namespace CavalryFight.Services.Replay
{
    /// <summary>
    /// リプレイ録画サービスの実装
    /// </summary>
    /// <remarks>
    /// ゲームプレイの録画を管理します。
    /// キーフレーム（0.1秒ごと）とイベントを記録し、
    /// ReplayDataとして保存できます。
    /// </remarks>
    public class ReplayRecorder : IReplayRecorder
    {
        #region Constants

        private const float KEYFRAME_INTERVAL = 0.1f; // 10 FPS for keyframes
        private const string REPLAY_FOLDER = "Replays";
        private const string REPLAY_EXTENSION = ".replay";

        #endregion

        #region Fields

        private bool _isRecording = false;
        private ReplayData? _currentRecording = null;
        private float _recordingTime = 0f;
        private float _nextKeyframeTime = 0f;
        private Dictionary<string, (EntityType type, GameObject gameObject)> _registeredEntities = new Dictionary<string, (EntityType, GameObject)>();
        private int _currentPlayerScore = 0;
        private int _currentEnemyScore = 0;

        #endregion

        #region Properties

        /// <summary>
        /// 現在録画中かどうかを取得します
        /// </summary>
        public bool IsRecording => _isRecording;

        /// <summary>
        /// 現在の録画データを取得します
        /// </summary>
        public ReplayData? CurrentRecording => _currentRecording;

        /// <summary>
        /// 録画開始からの経過時間（秒）を取得します
        /// </summary>
        public float RecordingTime => _recordingTime;

        #endregion

        #region Events

        /// <summary>
        /// 録画が開始された時に発生します
        /// </summary>
        public event EventHandler? RecordingStarted;

        /// <summary>
        /// 録画が停止された時に発生します
        /// </summary>
        public event EventHandler<ReplayRecordingStoppedEventArgs>? RecordingStopped;

        /// <summary>
        /// フレームが記録された時に発生します
        /// </summary>
        public event EventHandler<ReplayFrameRecordedEventArgs>? FrameRecorded;

        /// <summary>
        /// イベントが記録された時に発生します
        /// </summary>
        public event EventHandler<ReplayEventRecordedEventArgs>? EventRecorded;

        #endregion

        #region IService Implementation

        /// <summary>
        /// サービスを初期化します
        /// </summary>
        /// <remarks>
        /// リプレイフォルダを作成します。
        /// </remarks>
        public void Initialize()
        {
            Debug.Log("[ReplayRecorder] Initializing...");

            // リプレイフォルダが存在しない場合は作成
            string replayPath = GetReplayFolderPath();
            if (!Directory.Exists(replayPath))
            {
                Directory.CreateDirectory(replayPath);
                Debug.Log($"[ReplayRecorder] Created replay folder: {replayPath}");
            }

            Debug.Log("[ReplayRecorder] Initialized.");
        }

        /// <summary>
        /// サービスを破棄し、リソースを解放します
        /// </summary>
        /// <remarks>
        /// 録画中の場合は自動的に停止します。
        /// </remarks>
        public void Dispose()
        {
            Debug.Log("[ReplayRecorder] Disposing...");

            // 録画中の場合は停止
            if (_isRecording)
            {
                StopRecording();
            }

            // イベントハンドラをクリア
            RecordingStarted = null;
            RecordingStopped = null;
            FrameRecorded = null;
            EventRecorded = null;

            Debug.Log("[ReplayRecorder] Disposed.");
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// 録画を開始します
        /// </summary>
        /// <param name="mapName">マップ名</param>
        /// <param name="gameMode">ゲームモード</param>
        /// <param name="playerName">プレイヤー名</param>
        public void StartRecording(string mapName, string gameMode, string playerName = "Player")
        {
            if (_isRecording)
            {
                Debug.LogWarning("[ReplayRecorder] Already recording. Stop current recording first.");
                return;
            }

            Debug.Log($"[ReplayRecorder] Starting recording: {mapName} - {gameMode}");

            // 新しいリプレイデータを作成
            _currentRecording = new ReplayData
            {
                MapName = mapName,
                GameMode = gameMode,
                PlayerName = playerName,
                RecordedAt = DateTime.UtcNow.ToString("o")
            };

            _recordingTime = 0f;
            _nextKeyframeTime = 0f;
            _currentPlayerScore = 0;
            _currentEnemyScore = 0;
            _registeredEntities.Clear();
            _isRecording = true;

            // マッチ開始イベントを記録
            RecordEvent(ReplayEventType.MatchStart, "System", "Match started");

            // イベントを発火
            RecordingStarted?.Invoke(this, EventArgs.Empty);

            Debug.Log("[ReplayRecorder] Recording started.");
        }

        /// <summary>
        /// 録画を停止します
        /// </summary>
        /// <returns>録画されたリプレイデータ</returns>
        public ReplayData? StopRecording()
        {
            if (!_isRecording)
            {
                Debug.LogWarning("[ReplayRecorder] Not currently recording.");
                return null;
            }

            Debug.Log("[ReplayRecorder] Stopping recording...");

            _isRecording = false;

            if (_currentRecording != null)
            {
                // マッチ終了イベントを記録
                RecordEvent(ReplayEventType.MatchEnd, "System", "Match ended");

                // メタデータを更新
                _currentRecording.MatchDuration = _recordingTime;
                _currentRecording.FinalPlayerScore = _currentPlayerScore;
                _currentRecording.FinalEnemyScore = _currentEnemyScore;

                // ハイライトを自動生成
                GenerateHighlights(_currentRecording);

                Debug.Log($"[ReplayRecorder] Recording stopped. Frames: {_currentRecording.Frames.Count}, Events: {_currentRecording.Events.Count}");

                // イベントを発火
                RecordingStopped?.Invoke(this, new ReplayRecordingStoppedEventArgs(_currentRecording));
            }

            var result = _currentRecording;
            _currentRecording = null;
            _registeredEntities.Clear();

            return result;
        }

        /// <summary>
        /// エンティティを録画対象として登録します
        /// </summary>
        /// <param name="entityId">エンティティID</param>
        /// <param name="entityType">エンティティタイプ</param>
        /// <param name="gameObject">エンティティのGameObject</param>
        public void RegisterEntity(string entityId, EntityType entityType, GameObject gameObject)
        {
            if (!_isRecording)
            {
                return;
            }

            _registeredEntities[entityId] = (entityType, gameObject);
            Debug.Log($"[ReplayRecorder] Entity registered: {entityId} ({entityType})");
        }

        /// <summary>
        /// エンティティを録画対象から解除します
        /// </summary>
        /// <param name="entityId">エンティティID</param>
        public void UnregisterEntity(string entityId)
        {
            if (_registeredEntities.Remove(entityId))
            {
                Debug.Log($"[ReplayRecorder] Entity unregistered: {entityId}");
            }
        }

        /// <summary>
        /// イベントを記録します
        /// </summary>
        /// <param name="eventType">イベントタイプ</param>
        /// <param name="subjectEntityId">主体エンティティID</param>
        /// <param name="description">イベントの説明</param>
        /// <param name="targetEntityId">対象エンティティID（オプション）</param>
        public void RecordEvent(ReplayEventType eventType, string subjectEntityId, string description, string? targetEntityId = null)
        {
            if (!_isRecording || _currentRecording == null)
            {
                return;
            }

            var replayEvent = new ReplayEvent(_recordingTime, eventType, subjectEntityId, description)
            {
                TargetEntityId = targetEntityId
            };

            _currentRecording.AddEvent(replayEvent);

            // イベントを発火
            EventRecorded?.Invoke(this, new ReplayEventRecordedEventArgs(replayEvent));

            Debug.Log($"[ReplayRecorder] Event recorded: {eventType} - {description} at {_recordingTime:F2}s");
        }

        /// <summary>
        /// スコアを更新します
        /// </summary>
        /// <param name="playerScore">プレイヤースコア</param>
        /// <param name="enemyScore">敵スコア</param>
        public void UpdateScore(int playerScore, int enemyScore)
        {
            _currentPlayerScore = playerScore;
            _currentEnemyScore = enemyScore;
        }

        /// <summary>
        /// 録画のUpdate処理（MonoBehaviourのUpdateから呼ぶ必要があります）
        /// </summary>
        /// <param name="deltaTime">前フレームからの経過時間</param>
        public void UpdateRecording(float deltaTime)
        {
            if (!_isRecording || _currentRecording == null)
            {
                return;
            }

            _recordingTime += deltaTime;

            // キーフレーム記録時刻に達したか
            if (_recordingTime >= _nextKeyframeTime)
            {
                RecordFrame();
                _nextKeyframeTime = _recordingTime + KEYFRAME_INTERVAL;
            }
        }

        /// <summary>
        /// リプレイをファイルに保存します
        /// </summary>
        /// <param name="replay">保存するリプレイデータ</param>
        /// <param name="fileName">ファイル名（拡張子なし、省略時は自動生成）</param>
        /// <returns>保存に成功したかどうか</returns>
        public bool SaveReplay(ReplayData replay, string? fileName = null)
        {
            try
            {
                // ファイル名が指定されていない場合は自動生成
                if (string.IsNullOrEmpty(fileName))
                {
                    fileName = $"Replay_{DateTime.Now:yyyyMMdd_HHmmss}_{replay.ReplayId.Substring(0, 8)}";
                }

                string filePath = Path.Combine(GetReplayFolderPath(), fileName + REPLAY_EXTENSION);
                bool success = replay.SaveToFile(filePath);

                if (success)
                {
                    Debug.Log($"[ReplayRecorder] Replay saved: {fileName}");
                }

                return success;
            }
            catch (Exception ex)
            {
                Debug.LogError($"[ReplayRecorder] Failed to save replay: {ex.Message}");
                return false;
            }
        }

        #endregion

        #region Private Methods

        private void RecordFrame()
        {
            if (_currentRecording == null)
            {
                return;
            }

            var frame = new ReplayFrame(_recordingTime)
            {
                PlayerScore = _currentPlayerScore,
                EnemyScore = _currentEnemyScore,
                TimeRemaining = -1f,
                GameState = "Playing"
            };

            // 全エンティティのスナップショットを取得
            foreach (var kvp in _registeredEntities)
            {
                string entityId = kvp.Key;
                var (entityType, gameObject) = kvp.Value;

                if (gameObject == null)
                {
                    continue;
                }

                var rigidbody = gameObject.GetComponent<Rigidbody>();
                var animator = gameObject.GetComponent<Animator>();

                var snapshot = EntitySnapshot.FromGameObject(entityId, entityType, gameObject, rigidbody, animator);
                frame.AddEntity(snapshot);
            }

            _currentRecording.AddFrame(frame);

            // イベントを発火
            FrameRecorded?.Invoke(this, new ReplayFrameRecordedEventArgs(frame));
        }

        private void GenerateHighlights(ReplayData replayData)
        {
            replayData.GenerateHighlightsFromScoreEvents();
        }

        private string GetReplayFolderPath()
        {
            return Path.Combine(Application.persistentDataPath, REPLAY_FOLDER);
        }

        #endregion
    }
}
