#nullable enable

using System;
using System.Collections.Generic;
using CavalryFight.Core.Services;
using CavalryFight.Services.AI;
using CavalryFight.Services.Lobby;
using CavalryFight.Services.Replay;
using UnityEngine;

// SpawnPointはCavalryFight.Gameplayの既存クラスを使用
using SpawnPointComponent = CavalryFight.Gameplay.SpawnPoint;

namespace CavalryFight.Gameplay.Match
{
    /// <summary>
    /// マッチ中のAIプレイヤーのスポーンと管理を行うコンポーネント
    /// </summary>
    /// <remarks>
    /// MatchManagerと連携してAIプレイヤーをスポーンし、
    /// ゲームモードに応じた初期設定を行います。
    /// </remarks>
    public class AISpawner : MonoBehaviour
    {
        #region Serialized Fields

        [Header("AI Settings")]
        [Tooltip("AIプレイヤーのプレハブ（騎手）")]
        [SerializeField] private GameObject? _aiRiderPrefab;

        [Tooltip("AI馬のプレハブ")]
        [SerializeField] private GameObject? _aiMountPrefab;

        [Tooltip("AI難易度設定")]
        [SerializeField] private AIDifficultyConfig? _difficultyConfig;

        [Tooltip("ゲームモード行動設定")]
        [SerializeField] private AIGameModeBehavior? _gameModeBehavior;

        [Header("Spawn Settings")]
        [Tooltip("AIスポーンポイント（未設定の場合はSpawnManagerから取得）")]
        [SerializeField] private List<SpawnPointComponent> _aiSpawnPoints = new List<SpawnPointComponent>();

        [Tooltip("デバッグログを出力するか")]
        [SerializeField] private bool _debugLog = true;

        #endregion

        #region Private Fields

        private IAICombatService? _aiCombatService;
        private IReplayRecorder? _replayRecorder;
        private MatchManager? _matchManager;
        private readonly List<ulong> _spawnedAIIds = new List<ulong>();
        private readonly Dictionary<ulong, (string mountId, string riderId)> _aiEntityIds = new();
        private ulong _nextAIId = 1000; // AIのIDは1000から開始（プレイヤーと区別）

        #endregion

        #region Properties

        /// <summary>
        /// シングルトンインスタンス
        /// </summary>
        public static AISpawner? Instance { get; private set; }

        /// <summary>
        /// スポーン済みのAI数
        /// </summary>
        public int SpawnedAICount => _spawnedAIIds.Count;

        #endregion

        #region Events

        /// <summary>
        /// AIがスポーンした時に発生します
        /// </summary>
        public event Action<ulong, GameObject>? AISpawned;

        /// <summary>
        /// AIが削除された時に発生します
        /// </summary>
        public event Action<ulong>? AIDespawned;

        #endregion

        #region Unity Lifecycle

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Debug.LogWarning("[AISpawner] Duplicate instance detected. Destroying this one.");
                Destroy(gameObject);
                return;
            }

            Instance = this;
        }

        private void Start()
        {
            Debug.Log($"[AI-SPAWN] AISpawner.Start() called. GameObject: {gameObject.name}");

            // サービスを取得
            _aiCombatService = ServiceLocator.Instance.Get<IAICombatService>();
            _replayRecorder = ServiceLocator.Instance.Get<IReplayRecorder>();

            Debug.Log($"[AI-SPAWN] AISpawner: IAICombatService={(_aiCombatService != null ? "found" : "NULL")}, IReplayRecorder={(_replayRecorder != null ? "found" : "NULL")}");

            if (_aiCombatService == null)
            {
                Debug.LogWarning("[AI-SPAWN] IAICombatService が登録されていません。AICombatServiceを作成します。");
                // サービスが登録されていない場合は新規作成
                var combatService = new AICombatService();
                ServiceLocator.Instance.Register<IAICombatService>(combatService);
                _aiCombatService = combatService;
                Debug.Log($"[AI-SPAWN] AISpawner: Created and registered new AICombatService");
            }

            // MatchManagerを取得
            _matchManager = MatchManager.Instance;
            Debug.Log($"[AI-SPAWN] AISpawner: MatchManager.Instance={(_matchManager != null ? "found" : "NULL")}");

            // イベントを購読
            if (_aiCombatService != null)
            {
                _aiCombatService.AISpawned += OnAISpawnedInternal;
                _aiCombatService.AIDied += OnAIDied;
            }

            // プレハブの確認
            Debug.Log($"[AI-SPAWN] AISpawner: _aiRiderPrefab={(_aiRiderPrefab != null ? _aiRiderPrefab.name : "NULL")}, _aiMountPrefab={(_aiMountPrefab != null ? _aiMountPrefab.name : "NULL")}");
        }

        private void OnDestroy()
        {
            if (Instance == this)
            {
                Instance = null;
            }

            // イベント購読解除
            if (_aiCombatService != null)
            {
                _aiCombatService.AISpawned -= OnAISpawnedInternal;
                _aiCombatService.AIDied -= OnAIDied;
            }
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// AIシステムを初期化します
        /// </summary>
        /// <param name="gameMode">ゲームモード</param>
        /// <param name="difficulty">AI難易度</param>
        public void Initialize(GameMode gameMode, AIDifficulty difficulty)
        {
            _aiCombatService?.Initialize(gameMode, difficulty);

            if (_debugLog)
            {
                Debug.Log($"[AISpawner] Initialized. Mode: {gameMode}, Difficulty: {difficulty}");
            }
        }

        /// <summary>
        /// 指定した数のAIプレイヤーをスポーンします
        /// </summary>
        /// <param name="count">スポーンするAI数</param>
        /// <param name="teamIndex">チームインデックス（-1でチームなし）</param>
        /// <returns>スポーンしたAIのID一覧</returns>
        public List<ulong> SpawnAIPlayers(int count, int teamIndex = -1)
        {
            Debug.Log($"[AI-SPAWN-DEBUG] ========== SpawnAIPlayers START ==========");
            Debug.Log($"[AI-SPAWN-DEBUG] SpawnAIPlayers() called. count={count}, teamIndex={teamIndex}");
            Debug.Log($"[AI-SPAWN-DEBUG] _aiCombatService={(_aiCombatService != null ? "exists" : "NULL")}");

            List<ulong> spawnedIds = new List<ulong>();

            if (_aiCombatService == null)
            {
                Debug.LogError("[AI-SPAWN-DEBUG] AIコンバットサービスがnull！Returning empty list.");
                return spawnedIds;
            }

            // AISpawnerが存在するシーンをターゲットシーンとして設定
            // これにより、ローディング画面シーンではなくゲームプレイシーンにAIがスポーンされる
            _aiCombatService.SetTargetScene(gameObject.scene);
            Debug.Log($"[AI-SPAWN-DEBUG] Target scene set to: {gameObject.scene.name}");

            // スポーンポイントを取得
            List<SpawnPointComponent> availablePoints = GetAvailableSpawnPoints(count, teamIndex);
            Debug.Log($"[AI-SPAWN-DEBUG] availablePoints.Count={availablePoints.Count}");

            for (int i = 0; i < count && i < availablePoints.Count; i++)
            {
                SpawnPointComponent point = availablePoints[i];
                ulong aiId = GenerateAIId();

                Debug.Log($"[AI-SPAWN] Spawning AI #{i} at SpawnPoint position={point.Position}, rotation={point.Rotation.eulerAngles}");

                GameObject? aiObject = _aiCombatService.SpawnAIPlayer(
                    point.Position,
                    point.Rotation,
                    teamIndex,
                    aiId
                );

                if (aiObject != null)
                {
                    _spawnedAIIds.Add(aiId);
                    spawnedIds.Add(aiId);

                    Debug.Log($"[AI-SPAWN] AI spawned OK. ID={aiId}, Name={aiObject.name}, FinalPos={aiObject.transform.position}");
                }
                else
                {
                    Debug.LogError($"[AI-SPAWN] AI spawn FAILED! aiObject is null. aiId={aiId}");
                }
            }

            Debug.Log($"[AI-SPAWN] SpawnAIPlayers() completed. spawnedIds.Count={spawnedIds.Count}");
            return spawnedIds;
        }

        /// <summary>
        /// チーム別にAIプレイヤーをスポーンします（チームモード用）
        /// </summary>
        /// <param name="team0Count">チーム0のAI数</param>
        /// <param name="team1Count">チーム1のAI数</param>
        /// <returns>チーム別のAI ID一覧</returns>
        public (List<ulong> team0, List<ulong> team1) SpawnTeamAIPlayers(int team0Count, int team1Count)
        {
            List<ulong> team0Ids = SpawnAIPlayers(team0Count, 0);
            List<ulong> team1Ids = SpawnAIPlayers(team1Count, 1);

            return (team0Ids, team1Ids);
        }

        /// <summary>
        /// 指定したAIを削除します
        /// </summary>
        /// <param name="aiId">AI ID</param>
        public void DespawnAI(ulong aiId)
        {
            // リプレイレコーダーから解除
            UnregisterAIFromReplay(aiId);

            _aiCombatService?.DespawnAIPlayer(aiId);
            _spawnedAIIds.Remove(aiId);

            AIDespawned?.Invoke(aiId);

            if (_debugLog)
            {
                Debug.Log($"[AISpawner] AI despawned. ID: {aiId}");
            }
        }

        /// <summary>
        /// すべてのAIを削除します
        /// </summary>
        public void DespawnAllAI()
        {
            // リプレイレコーダーから全AI解除
            foreach (ulong aiId in _spawnedAIIds)
            {
                UnregisterAIFromReplay(aiId);
            }

            _aiCombatService?.DespawnAllAIPlayers();

            foreach (ulong aiId in _spawnedAIIds)
            {
                AIDespawned?.Invoke(aiId);
            }

            _spawnedAIIds.Clear();
            _aiEntityIds.Clear();

            if (_debugLog)
            {
                Debug.Log("[AISpawner] All AI despawned");
            }
        }

        /// <summary>
        /// すべてのAIを有効化します（マッチ開始時）
        /// </summary>
        public void EnableAllAI()
        {
            _aiCombatService?.EnableAllAI();

            if (_debugLog)
            {
                Debug.Log("[AISpawner] All AI enabled");
            }
        }

        /// <summary>
        /// すべてのAIを無効化します（マッチ終了時）
        /// </summary>
        public void DisableAllAI()
        {
            _aiCombatService?.DisableAllAI();

            if (_debugLog)
            {
                Debug.Log("[AISpawner] All AI disabled");
            }
        }

        /// <summary>
        /// 指定したAIをリスポーンします
        /// </summary>
        /// <param name="aiId">AI ID</param>
        /// <param name="teamIndex">チームインデックス</param>
        public void RespawnAI(ulong aiId, int teamIndex = -1)
        {
            // まず削除
            _aiCombatService?.DespawnAIPlayer(aiId);
            _spawnedAIIds.Remove(aiId);

            // 新しいスポーンポイントを取得
            List<SpawnPointComponent> points = GetAvailableSpawnPoints(1, teamIndex);
            if (points.Count == 0)
            {
                Debug.LogWarning("[AISpawner] No spawn points available for respawn!");
                return;
            }

            SpawnPointComponent point = points[0];

            // 再スポーン
            GameObject? aiObject = _aiCombatService?.SpawnAIPlayer(
                point.Position,
                point.Rotation,
                teamIndex,
                aiId
            );

            if (aiObject != null)
            {
                _spawnedAIIds.Add(aiId);

                if (_debugLog)
                {
                    Debug.Log($"[AISpawner] AI respawned. ID: {aiId}");
                }
            }
        }

        /// <summary>
        /// 指定したIDがAIかどうかを判定します
        /// </summary>
        /// <param name="id">判定するID</param>
        /// <returns>AIの場合はtrue</returns>
        public bool IsAI(ulong id)
        {
            return id >= 1000 && _spawnedAIIds.Contains(id);
        }

        /// <summary>
        /// AIの残り数を取得します
        /// </summary>
        /// <param name="teamIndex">チームインデックス（-1で全体）</param>
        /// <returns>生存AI数</returns>
        public int GetAliveAICount(int teamIndex = -1)
        {
            if (_aiCombatService == null)
            {
                return 0;
            }

            int count = 0;
            foreach (ulong aiId in _spawnedAIIds)
            {
                if (_aiCombatService.IsAIAlive(aiId))
                {
                    if (teamIndex < 0)
                    {
                        count++;
                    }
                    else
                    {
                        var aiIds = _aiCombatService.GetAIIdsByTeam(teamIndex);
                        if (((ICollection<ulong>)aiIds).Contains(aiId))
                        {
                            count++;
                        }
                    }
                }
            }

            return count;
        }

        #endregion

        #region Private Methods

        /// <summary>
        /// 利用可能なスポーンポイントを取得します
        /// </summary>
        private List<SpawnPointComponent> GetAvailableSpawnPoints(int count, int teamIndex)
        {
            List<SpawnPointComponent> points = new List<SpawnPointComponent>();

            Debug.Log($"[AI-SPAWN-DEBUG] GetAvailableSpawnPoints - requested:{count}, team:{teamIndex}, _aiSpawnPoints.Count:{_aiSpawnPoints.Count}, SpawnManager={(SpawnManager.Instance != null ? "exists" : "NULL")}");

            // AISpawnerに設定されたスポーンポイントを優先
            if (_aiSpawnPoints.Count > 0)
            {
                points.AddRange(_aiSpawnPoints);
                Debug.Log($"[AI-SPAWN-DEBUG] Using serialized spawn points: {points.Count}");
            }
            else if (SpawnManager.Instance != null)
            {
                // SpawnManagerからスポーンポイントを取得
                Debug.Log($"[AI-SPAWN-DEBUG] Trying SpawnManager.Instance...");
                for (int i = 0; i < count; i++)
                {
                    SpawnPointComponent? point = teamIndex >= 0
                        ? SpawnManager.Instance.GetSpawnPointForTeam(teamIndex)
                        : SpawnManager.Instance.GetRandomSpawnPoint();

                    if (point != null)
                    {
                        points.Add(point);
                        Debug.Log($"[AI-SPAWN-DEBUG] Got spawn point [{i}]: pos={point.Position}");
                    }
                    else
                    {
                        Debug.LogWarning($"[AI-SPAWN-DEBUG] SpawnManager returned NULL for spawn point [{i}]");
                    }
                }
            }
            else
            {
                Debug.LogError("[AI-SPAWN-DEBUG] No spawn points available! _aiSpawnPoints is empty and SpawnManager.Instance is NULL");
            }

            // スポーンポイントが足りない場合は警告を出す
            if (points.Count < count)
            {
                Debug.LogWarning($"[AISpawner] Not enough spawn points. Requested: {count}, Available: {points.Count}");
            }

            return points;
        }

        /// <summary>
        /// ユニークなAI IDを生成します
        /// </summary>
        private ulong GenerateAIId()
        {
            return _nextAIId++;
        }

        /// <summary>
        /// AIスポーン時の内部ハンドラ
        /// </summary>
        private void OnAISpawnedInternal(ulong aiId, GameObject aiObject)
        {
            // リプレイレコーダーにAIエンティティを登録
            RegisterAIForReplay(aiId, aiObject);

            AISpawned?.Invoke(aiId, aiObject);
        }

        /// <summary>
        /// AIエンティティをリプレイレコーダーに登録します
        /// </summary>
        private void RegisterAIForReplay(ulong aiId, GameObject aiObject)
        {
            if (_replayRecorder == null || !_replayRecorder.IsRecording)
            {
                return;
            }

            string mountEntityId = $"ai_mount_{aiId}";
            string riderEntityId = $"ai_rider_{aiId}";

            // AIオブジェクトの構造を確認（馬と騎手を分離）
            // AICombatServiceのSpawnAIPlayerが返すのはルートオブジェクト
            // 馬は通常ルートオブジェクト自体、騎手はMRiderを持つ子オブジェクト

            // 馬（ルートまたはMAnimalを持つオブジェクト）を登録
            var mAnimal = aiObject.GetComponent<MonoBehaviour>();
            GameObject? mountObject = null;
            GameObject? riderObject = null;

            // MAnimalを探す（馬）
            foreach (var comp in aiObject.GetComponents<MonoBehaviour>())
            {
                if (comp != null && comp.GetType().Name.Contains("MAnimal"))
                {
                    mountObject = aiObject;
                    break;
                }
            }

            // MRiderを探す（騎手）
            foreach (var comp in aiObject.GetComponentsInChildren<MonoBehaviour>(true))
            {
                if (comp != null && comp.GetType().Name.Contains("MRider"))
                {
                    riderObject = comp.gameObject;
                    break;
                }
            }

            // 馬が見つからない場合、AIオブジェクト自体を馬として扱う
            if (mountObject == null)
            {
                mountObject = aiObject;
            }

            // 馬を登録
            _replayRecorder.RegisterEntity(mountEntityId, EntityType.Mount, mountObject);

            // 騎手が見つかった場合は登録
            if (riderObject != null)
            {
                // MountPointを探す
                Transform? mountPoint = FindMountPoint(mountObject);

                _replayRecorder.RegisterRiderEntity(
                    riderEntityId,
                    EntityType.Enemy,
                    riderObject,
                    mountEntityId,
                    mountPoint
                );

                if (_debugLog)
                {
                    Debug.Log($"[AISpawner] AI騎手をリプレイに登録: {riderEntityId} -> {mountEntityId}");
                }
            }

            // エンティティIDを保存
            _aiEntityIds[aiId] = (mountEntityId, riderEntityId);

            if (_debugLog)
            {
                Debug.Log($"[AISpawner] AI馬をリプレイに登録: {mountEntityId}");
            }
        }

        /// <summary>
        /// AIエンティティをリプレイレコーダーから解除します
        /// </summary>
        private void UnregisterAIFromReplay(ulong aiId)
        {
            if (_replayRecorder == null)
            {
                return;
            }

            if (_aiEntityIds.TryGetValue(aiId, out var entityIds))
            {
                _replayRecorder.UnregisterEntity(entityIds.riderId);
                _replayRecorder.UnregisterEntity(entityIds.mountId);
                _aiEntityIds.Remove(aiId);
            }
        }

        /// <summary>
        /// 馬のMountPointを探します
        /// </summary>
        private Transform? FindMountPoint(GameObject mount)
        {
            string[] mountPointNames = { "MountPoint", "Mount Point", "Seat", "RiderSeat", "Sit Point", "SitPoint" };

            foreach (string name in mountPointNames)
            {
                Transform? point = mount.transform.Find(name);
                if (point != null)
                {
                    return point;
                }
            }

            // 再帰的に検索
            foreach (string name in mountPointNames)
            {
                Transform? point = FindChildRecursive(mount.transform, name);
                if (point != null)
                {
                    return point;
                }
            }

            return null;
        }

        /// <summary>
        /// 子オブジェクトを再帰的に検索します
        /// </summary>
        private Transform? FindChildRecursive(Transform parent, string name)
        {
            foreach (Transform child in parent)
            {
                if (child.name.Contains(name))
                {
                    return child;
                }

                Transform? found = FindChildRecursive(child, name);
                if (found != null)
                {
                    return found;
                }
            }

            return null;
        }

        /// <summary>
        /// AI死亡時のハンドラ
        /// </summary>
        private void OnAIDied(ulong aiId, ulong killerId)
        {
            if (_debugLog)
            {
                Debug.Log($"[AISpawner] AI {aiId} died, killed by {killerId}");
            }

            // MatchManagerに通知
            if (_matchManager != null && _matchManager.IsServer)
            {
                _matchManager.RecordPlayerDeathRpc(aiId, killerId);
            }

            // リスポーンをスケジュール
            ScheduleRespawn(aiId);
        }

        /// <summary>
        /// AIのリスポーンをスケジュールします
        /// </summary>
        /// <param name="aiId">AI ID</param>
        private void ScheduleRespawn(ulong aiId)
        {
            // ゲームモードがリスポーンを許可しているか確認
            if (_matchManager == null || !_matchManager.CanRespawn(aiId))
            {
                if (_debugLog)
                {
                    Debug.Log($"[AISpawner] Respawn not allowed for AI {aiId}");
                }
                return;
            }

            // リスポーン遅延を取得
            float respawnDelay = _matchManager.GetRespawnDelay();

            if (_debugLog)
            {
                Debug.Log($"[AISpawner] Scheduling respawn for AI {aiId} in {respawnDelay} seconds");
            }

            // 遅延後にリスポーン
            StartCoroutine(RespawnAfterDelay(aiId, respawnDelay));
        }

        /// <summary>
        /// 遅延後にAIをリスポーンします
        /// </summary>
        private System.Collections.IEnumerator RespawnAfterDelay(ulong aiId, float delay)
        {
            yield return new WaitForSeconds(delay);

            // マッチがまだ進行中か確認
            if (_matchManager == null || _matchManager.CurrentState != MatchState.InProgress)
            {
                yield break;
            }

            // リスポーンを実行
            // チームインデックスを取得（AIは基本的にチームなし、または敵チーム）
            int teamIndex = GetAITeamIndex(aiId);
            RespawnAI(aiId, teamIndex);

            // リスポーン後にAIを有効化
            if (_aiCombatService != null && _aiCombatService.IsEnabled)
            {
                // AIは既にSpawnAIPlayer内で初期化されているので、追加の有効化は不要
            }
        }

        /// <summary>
        /// AIのチームインデックスを取得します
        /// </summary>
        private int GetAITeamIndex(ulong aiId)
        {
            // チーム戦モードの場合、AIのチームインデックスを返す
            // デフォルトでは-1（チームなし）
            if (_aiCombatService != null)
            {
                // チーム0とチーム1をチェック
                for (int i = 0; i <= 1; i++)
                {
                    var teamAIs = _aiCombatService.GetAIIdsByTeam(i);
                    if (((ICollection<ulong>)teamAIs).Contains(aiId))
                    {
                        return i;
                    }
                }
            }
            return -1;
        }

        #endregion
    }
}
