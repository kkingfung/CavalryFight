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
        private Dictionary<string, RegisteredEntity> _registeredEntities = new();
        private int _currentPlayerScore = 0;
        private int _currentEnemyScore = 0;

        #endregion

        #region Nested Types

        /// <summary>
        /// 登録されたエンティティの情報
        /// </summary>
        private class RegisteredEntity
        {
            public EntityType Type;
            public GameObject GameObject = null!;
            public Rigidbody? Rigidbody;
            public Animator? Animator;

            // 騎手用の追加情報
            public string? MountEntityId;
            public Transform? MountPoint;

            // キャッシュされたコンポーネント参照（リフレクションを避けるため）
            public MonoBehaviour? MRiderComponent;
            public MonoBehaviour? MAnimalComponent;
        }

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

            var entity = new RegisteredEntity
            {
                Type = entityType,
                GameObject = gameObject,
                Rigidbody = gameObject.GetComponent<Rigidbody>(),
                Animator = gameObject.GetComponent<Animator>()
            };

            // Malbersコンポーネントをキャッシュ（名前で検索してリフレクションを最小化）
            CacheMalbersComponents(entity, gameObject);

            _registeredEntities[entityId] = entity;
            Debug.Log($"[ReplayRecorder] Entity registered: {entityId} ({entityType})");
        }

        /// <summary>
        /// 騎手エンティティを馬と関連付けて登録します
        /// </summary>
        /// <param name="riderEntityId">騎手のエンティティID</param>
        /// <param name="entityType">エンティティタイプ</param>
        /// <param name="riderGameObject">騎手のGameObject</param>
        /// <param name="mountEntityId">騎乗している馬のエンティティID</param>
        /// <param name="mountPoint">騎乗ポイントのTransform</param>
        public void RegisterRiderEntity(string riderEntityId, EntityType entityType, GameObject riderGameObject, string mountEntityId, Transform? mountPoint = null)
        {
            if (!_isRecording)
            {
                return;
            }

            var entity = new RegisteredEntity
            {
                Type = entityType,
                GameObject = riderGameObject,
                Rigidbody = riderGameObject.GetComponent<Rigidbody>(),
                Animator = riderGameObject.GetComponent<Animator>(),
                MountEntityId = mountEntityId,
                MountPoint = mountPoint
            };

            // Malbersコンポーネントをキャッシュ
            CacheMalbersComponents(entity, riderGameObject);

            _registeredEntities[riderEntityId] = entity;
            Debug.Log($"[ReplayRecorder] Rider entity registered: {riderEntityId} -> Mount: {mountEntityId}");
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
        /// 騎手の騎乗状態を更新します
        /// </summary>
        /// <param name="riderEntityId">騎手のエンティティID</param>
        /// <param name="mountEntityId">騎乗している馬のエンティティID（降りた場合はnull）</param>
        /// <param name="mountPoint">騎乗ポイントのTransform</param>
        public void UpdateRiderMountState(string riderEntityId, string? mountEntityId, Transform? mountPoint)
        {
            if (_registeredEntities.TryGetValue(riderEntityId, out var entity))
            {
                entity.MountEntityId = mountEntityId;
                entity.MountPoint = mountPoint;
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
                var entity = kvp.Value;

                if (entity.GameObject == null)
                {
                    continue;
                }

                var snapshot = CreateSnapshot(entityId, entity);
                frame.AddEntity(snapshot);
            }

            _currentRecording.AddFrame(frame);

            // イベントを発火
            FrameRecorded?.Invoke(this, new ReplayFrameRecordedEventArgs(frame));
        }

        /// <summary>
        /// エンティティからスナップショットを作成します
        /// </summary>
        private EntitySnapshot CreateSnapshot(string entityId, RegisteredEntity entity)
        {
            var snapshot = EntitySnapshot.FromGameObject(
                entityId,
                entity.Type,
                entity.GameObject,
                entity.Rigidbody,
                entity.Animator
            );

            // 騎乗状態を記録
            if (!string.IsNullOrEmpty(entity.MountEntityId))
            {
                // MRiderコンポーネントから騎乗状態を取得
                bool isRiding = GetMRiderIsRiding(entity.MRiderComponent);

                if (isRiding && entity.MountPoint != null)
                {
                    snapshot.IsMounted = true;
                    snapshot.MountedOnEntityId = entity.MountEntityId;
                    snapshot.LocalPositionOnMount = entity.MountPoint.InverseTransformPoint(entity.GameObject.transform.position);
                    snapshot.LocalRotationOnMount = Quaternion.Inverse(entity.MountPoint.rotation) * entity.GameObject.transform.rotation;
                }
            }

            // Malbers状態を記録
            if (entity.MAnimalComponent != null)
            {
                CaptureMalbersState(snapshot, entity.MAnimalComponent);
            }

            return snapshot;
        }

        /// <summary>
        /// MalbersコンポーネントをGameObjectから検索してキャッシュします
        /// </summary>
        private void CacheMalbersComponents(RegisteredEntity entity, GameObject gameObject)
        {
            var components = gameObject.GetComponents<MonoBehaviour>();
            foreach (var comp in components)
            {
                if (comp == null) continue;

                string typeName = comp.GetType().Name;
                if (typeName == "MRider" || typeName.Contains("MRider"))
                {
                    entity.MRiderComponent = comp;
                }
                else if (typeName == "MAnimal" || typeName.Contains("MAnimal"))
                {
                    entity.MAnimalComponent = comp;
                }
            }

            // 子オブジェクトも検索
            if (entity.MRiderComponent == null || entity.MAnimalComponent == null)
            {
                var childComponents = gameObject.GetComponentsInChildren<MonoBehaviour>(true);
                foreach (var comp in childComponents)
                {
                    if (comp == null) continue;

                    string typeName = comp.GetType().Name;
                    if (entity.MRiderComponent == null && (typeName == "MRider" || typeName.Contains("MRider")))
                    {
                        entity.MRiderComponent = comp;
                    }
                    else if (entity.MAnimalComponent == null && (typeName == "MAnimal" || typeName.Contains("MAnimal")))
                    {
                        entity.MAnimalComponent = comp;
                    }
                }
            }
        }

        /// <summary>
        /// MRiderのIsRiding状態を取得します
        /// </summary>
        private bool GetMRiderIsRiding(MonoBehaviour? mRider)
        {
            if (mRider == null) return false;

            try
            {
                var type = mRider.GetType();
                var property = type.GetProperty("IsRiding") ?? type.GetProperty("IsMounted") ?? type.GetProperty("Mounted");
                if (property != null)
                {
                    return (bool)property.GetValue(mRider);
                }

                var field = type.GetField("IsRiding") ?? type.GetField("IsMounted") ?? type.GetField("Mounted");
                if (field != null)
                {
                    return (bool)field.GetValue(mRider);
                }
            }
            catch
            {
                // リフレクションエラーは無視
            }

            return false;
        }

        /// <summary>
        /// MAnimalからMalbers状態をキャプチャします
        /// </summary>
        private void CaptureMalbersState(EntitySnapshot snapshot, MonoBehaviour mAnimal)
        {
            try
            {
                var type = mAnimal.GetType();

                // ActiveStateID を取得
                var stateIdProp = type.GetProperty("ActiveStateID");
                if (stateIdProp != null)
                {
                    var value = stateIdProp.GetValue(mAnimal);
                    if (value is int intValue)
                    {
                        snapshot.MalbersStateId = intValue;
                    }
                    else
                    {
                        // StateIDが独自の型の場合、ID プロパティを取得
                        var idProp = value?.GetType().GetProperty("ID");
                        if (idProp != null)
                        {
                            snapshot.MalbersStateId = (int)idProp.GetValue(value)!;
                        }
                    }
                }

                // ActiveMode を取得
                var modeProp = type.GetProperty("ActiveMode");
                if (modeProp != null)
                {
                    var mode = modeProp.GetValue(mAnimal);
                    if (mode != null)
                    {
                        var modeIdProp = mode.GetType().GetProperty("ID");
                        if (modeIdProp != null)
                        {
                            var modeIdValue = modeIdProp.GetValue(mode);
                            if (modeIdValue is int intModeId)
                            {
                                snapshot.MalbersModeId = intModeId;
                            }
                            else
                            {
                                var idProp = modeIdValue?.GetType().GetProperty("ID");
                                if (idProp != null)
                                {
                                    snapshot.MalbersModeId = (int)idProp.GetValue(modeIdValue)!;
                                }
                            }
                        }
                    }
                }

                // Stance を取得
                var stanceProp = type.GetProperty("Stance");
                if (stanceProp != null)
                {
                    var stanceValue = stanceProp.GetValue(mAnimal);
                    if (stanceValue is int intStance)
                    {
                        snapshot.MalbersStanceId = intStance;
                    }
                    else
                    {
                        var idProp = stanceValue?.GetType().GetProperty("ID");
                        if (idProp != null)
                        {
                            snapshot.MalbersStanceId = (int)idProp.GetValue(stanceValue)!;
                        }
                    }
                }

                // HorizontalSpeed (Forward Speed) を取得
                var speedProp = type.GetProperty("HorizontalSpeed") ?? type.GetProperty("ForwardSpeed") ?? type.GetProperty("Speed");
                if (speedProp != null)
                {
                    snapshot.ForwardSpeed = (float)speedProp.GetValue(mAnimal)!;
                }

                // 入力値を取得
                var verticalProp = type.GetProperty("VerticalSmooth") ?? type.GetProperty("Vertical");
                if (verticalProp != null)
                {
                    snapshot.VerticalInput = (float)verticalProp.GetValue(mAnimal)!;
                }

                var horizontalProp = type.GetProperty("HorizontalSmooth") ?? type.GetProperty("Horizontal");
                if (horizontalProp != null)
                {
                    snapshot.HorizontalInput = (float)horizontalProp.GetValue(mAnimal)!;
                }
            }
            catch
            {
                // リフレクションエラーは無視
            }
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
