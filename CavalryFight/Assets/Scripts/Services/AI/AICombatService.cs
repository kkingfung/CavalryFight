#nullable enable

using System;
using System.Collections.Generic;
using CavalryFight.Core.Services;
using CavalryFight.Services.Lobby;
using CavalryFight.Services.Customization;
using UnityEngine;

namespace CavalryFight.Services.AI
{
    /// <summary>
    /// AI戦闘システムを管理するサービスの実装
    /// </summary>
    /// <remarks>
    /// BlazeAIを使用してAIプレイヤーの戦闘行動を制御します。
    /// AIプレイヤーは騎馬弓兵として動作し、ゲームモードに応じた戦術を取ります。
    /// </remarks>
    public class AICombatService : IAICombatService
    {
        #region Fields

        private readonly Dictionary<ulong, AIPlayerData> _aiPlayers = new Dictionary<ulong, AIPlayerData>();
        private readonly Dictionary<int, List<ulong>> _teamAIPlayers = new Dictionary<int, List<ulong>>();

        private GameMode _currentGameMode;
        private AIDifficulty _currentDifficulty;
        private DifficultySettings _difficultySettings;

        private AIServiceConfig? _serviceConfig;

        private ICustomizationService? _customizationService;

        private bool _isEnabled;
        private bool _isInitialized;
        private bool _isServiceInitialized;

        #endregion

        #region Properties

        /// <summary>
        /// アクティブなAIプレイヤーの数
        /// </summary>
        public int ActiveAICount => _aiPlayers.Count;

        /// <summary>
        /// AIが有効かどうか
        /// </summary>
        public bool IsEnabled => _isEnabled;

        #endregion

        #region Events

        /// <summary>
        /// AIがスポーンした時に発生します
        /// </summary>
        public event Action<ulong, GameObject>? AISpawned;

        /// <summary>
        /// AIが死亡した時に発生します
        /// </summary>
        public event Action<ulong, ulong>? AIDied;

        /// <summary>
        /// AIがスコアを獲得した時に発生します
        /// </summary>
        public event Action<ulong, int>? AIScored;

        /// <summary>
        /// AIが矢を発射した時に発生します
        /// </summary>
        public event Action<ulong>? AIFiredArrow;

        #endregion

        #region Constructor

        /// <summary>
        /// AICombatServiceのコンストラクタ
        /// </summary>
        public AICombatService()
        {
        }

        #endregion

        #region IService Implementation

        /// <summary>
        /// サービスを初期化します（IService）
        /// </summary>
        void IService.Initialize()
        {
            if (_isServiceInitialized)
            {
                return;
            }

            _customizationService = ServiceLocator.Instance.Get<ICustomizationService>();
            _isServiceInitialized = true;

            Debug.Log("[AICombatService] Service initialized");
        }

        #endregion

        #region AI Lifecycle

        /// <summary>
        /// AIシステムをゲームモードと難易度で初期化します
        /// </summary>
        /// <param name="gameMode">ゲームモード</param>
        /// <param name="difficulty">AI難易度</param>
        public void Initialize(GameMode gameMode, AIDifficulty difficulty)
        {
            _currentGameMode = gameMode;
            _currentDifficulty = difficulty;

            // サービス設定をロード
            LoadServiceConfig();

            // 難易度設定を適用
            LoadDifficultySettings();

            _isInitialized = true;
            _isEnabled = false;

            Debug.Log($"[AICombatService] Initialized. GameMode: {gameMode}, Difficulty: {difficulty}");
        }

        /// <summary>
        /// AIプレイヤーをスポーンします
        /// </summary>
        /// <param name="spawnPoint">スポーン位置</param>
        /// <param name="rotation">スポーン時の回転</param>
        /// <param name="teamIndex">チームインデックス（-1の場合はチームなし）</param>
        /// <param name="aiId">AIのユニークID</param>
        /// <returns>スポーンしたAIプレイヤーのGameObject</returns>
        public GameObject? SpawnAIPlayer(Vector3 spawnPoint, Quaternion rotation, int teamIndex, ulong aiId)
        {
            if (!_isInitialized)
            {
                Debug.LogError("[AICombatService] Service not initialized!");
                return null;
            }

            if (_aiPlayers.ContainsKey(aiId))
            {
                Debug.LogWarning($"[AICombatService] AI with ID {aiId} already exists!");
                return null;
            }

            // AIプレイヤーを作成
            AIPlayerData? aiData = CreateAIPlayer(spawnPoint, rotation, teamIndex, aiId);
            if (aiData == null)
            {
                return null;
            }

            // 登録
            _aiPlayers[aiId] = aiData;

            // チーム登録
            if (teamIndex >= 0)
            {
                if (!_teamAIPlayers.ContainsKey(teamIndex))
                {
                    _teamAIPlayers[teamIndex] = new List<ulong>();
                }
                _teamAIPlayers[teamIndex].Add(aiId);
            }

            // イベント発火
            if (aiData.RootObject != null)
            {
                AISpawned?.Invoke(aiId, aiData.RootObject);
            }

            Debug.Log($"[AICombatService] AI Player spawned. ID: {aiId}, Team: {teamIndex}");

            return aiData.RootObject;
        }

        /// <summary>
        /// 指定したAIプレイヤーを削除します
        /// </summary>
        /// <param name="aiId">AIのユニークID</param>
        public void DespawnAIPlayer(ulong aiId)
        {
            if (!_aiPlayers.TryGetValue(aiId, out AIPlayerData? aiData))
            {
                return;
            }

            // チームから削除
            if (aiData.TeamIndex >= 0 && _teamAIPlayers.TryGetValue(aiData.TeamIndex, out List<ulong>? teamList))
            {
                teamList.Remove(aiId);
            }

            // GameObjectを削除
            if (aiData.RootObject != null)
            {
                UnityEngine.Object.Destroy(aiData.RootObject);
            }
            if (aiData.MountObject != null)
            {
                UnityEngine.Object.Destroy(aiData.MountObject);
            }

            _aiPlayers.Remove(aiId);

            Debug.Log($"[AICombatService] AI Player despawned. ID: {aiId}");
        }

        /// <summary>
        /// すべてのAIプレイヤーを削除します
        /// </summary>
        public void DespawnAllAIPlayers()
        {
            List<ulong> aiIds = new List<ulong>(_aiPlayers.Keys);
            foreach (ulong aiId in aiIds)
            {
                DespawnAIPlayer(aiId);
            }

            _aiPlayers.Clear();
            _teamAIPlayers.Clear();
        }

        /// <summary>
        /// AIシステムをクリーンアップします
        /// </summary>
        public void Cleanup()
        {
            DespawnAllAIPlayers();
            _isInitialized = false;
            _isEnabled = false;

            Debug.Log("[AICombatService] Cleaned up");
        }

        #endregion

        #region AI Control

        /// <summary>
        /// すべてのAIの行動を有効化します
        /// </summary>
        public void EnableAllAI()
        {
            _isEnabled = true;

            foreach (var kvp in _aiPlayers)
            {
                EnableAI(kvp.Value);
            }

            Debug.Log("[AICombatService] All AI enabled");
        }

        /// <summary>
        /// すべてのAIの行動を無効化します
        /// </summary>
        public void DisableAllAI()
        {
            _isEnabled = false;

            foreach (var kvp in _aiPlayers)
            {
                DisableAI(kvp.Value);
            }

            Debug.Log("[AICombatService] All AI disabled");
        }

        /// <summary>
        /// 指定したAIのターゲットを設定します
        /// </summary>
        /// <param name="aiId">AIのユニークID</param>
        /// <param name="target">ターゲットのGameObject</param>
        public void SetAITarget(ulong aiId, GameObject target)
        {
            if (!_aiPlayers.TryGetValue(aiId, out AIPlayerData? aiData))
            {
                return;
            }

            if (aiData.AIController != null)
            {
                aiData.AIController.SetTarget(target);
            }
        }

        /// <summary>
        /// 指定したAIを攻撃状態に遷移させます
        /// </summary>
        /// <param name="aiId">AIのユニークID</param>
        public void TriggerAIAttack(ulong aiId)
        {
            if (!_aiPlayers.TryGetValue(aiId, out AIPlayerData? aiData))
            {
                return;
            }

            if (aiData.AIController != null)
            {
                aiData.AIController.TriggerAttack();
            }
        }

        /// <summary>
        /// 指定したAIにダメージを与えます
        /// </summary>
        /// <param name="aiId">AIのユニークID</param>
        /// <param name="damage">ダメージ量</param>
        /// <param name="attacker">攻撃者のGameObject</param>
        public void DamageAI(ulong aiId, int damage, GameObject? attacker)
        {
            if (!_aiPlayers.TryGetValue(aiId, out AIPlayerData? aiData))
            {
                return;
            }

            if (aiData.AIController != null)
            {
                aiData.AIController.TakeDamage(damage, attacker);
            }
        }

        /// <summary>
        /// 指定したAIを死亡させます
        /// </summary>
        /// <param name="aiId">AIのユニークID</param>
        /// <param name="killer">キルしたプレイヤーのGameObject</param>
        public void KillAI(ulong aiId, GameObject? killer)
        {
            if (!_aiPlayers.TryGetValue(aiId, out AIPlayerData? aiData))
            {
                return;
            }

            aiData.IsAlive = false;

            if (aiData.AIController != null)
            {
                aiData.AIController.Die(killer);
            }

            // キラーのIDを取得（プレイヤーまたはAI）
            ulong killerId = GetKillerId(killer);

            AIDied?.Invoke(aiId, killerId);

            Debug.Log($"[AICombatService] AI {aiId} killed by {killerId}");
        }

        #endregion

        #region AI State

        /// <summary>
        /// 指定したAIが生存しているかどうかを取得します
        /// </summary>
        /// <param name="aiId">AIのユニークID</param>
        /// <returns>生存している場合はtrue</returns>
        public bool IsAIAlive(ulong aiId)
        {
            if (_aiPlayers.TryGetValue(aiId, out AIPlayerData? aiData))
            {
                return aiData.IsAlive;
            }
            return false;
        }

        /// <summary>
        /// 指定したAIのGameObjectを取得します
        /// </summary>
        /// <param name="aiId">AIのユニークID</param>
        /// <returns>AIのGameObject（存在しない場合はnull）</returns>
        public GameObject? GetAIGameObject(ulong aiId)
        {
            if (_aiPlayers.TryGetValue(aiId, out AIPlayerData? aiData))
            {
                return aiData.RootObject;
            }
            return null;
        }

        /// <summary>
        /// すべてのAI IDを取得します
        /// </summary>
        /// <returns>AI IDのコレクション</returns>
        public IReadOnlyCollection<ulong> GetAllAIIds()
        {
            return _aiPlayers.Keys;
        }

        /// <summary>
        /// 指定したチームのAI IDを取得します
        /// </summary>
        /// <param name="teamIndex">チームインデックス</param>
        /// <returns>AI IDのコレクション</returns>
        public IReadOnlyCollection<ulong> GetAIIdsByTeam(int teamIndex)
        {
            if (_teamAIPlayers.TryGetValue(teamIndex, out List<ulong>? teamList))
            {
                return teamList;
            }
            return Array.Empty<ulong>();
        }

        #endregion

        #region Internal Methods

        /// <summary>
        /// AIがスコアを獲得したことを通知します
        /// </summary>
        /// <param name="aiId">AIのユニークID</param>
        /// <param name="score">獲得スコア</param>
        internal void NotifyAIScored(ulong aiId, int score)
        {
            AIScored?.Invoke(aiId, score);
        }

        /// <summary>
        /// AIが矢を発射したことを通知します
        /// </summary>
        /// <param name="aiId">AIのユニークID</param>
        internal void NotifyAIFiredArrow(ulong aiId)
        {
            AIFiredArrow?.Invoke(aiId);
        }

        #endregion

        #region Private Methods

        /// <summary>
        /// サービス設定をロードします
        /// </summary>
        private void LoadServiceConfig()
        {
            _serviceConfig = Resources.Load<AIServiceConfig>("Settings/AIServiceConfig");

            if (_serviceConfig == null)
            {
                Debug.LogError("[AICombatService] AIServiceConfig not found at Resources/Settings/AIServiceConfig. Please create this asset.");
                return;
            }

            // 設定を検証
            _serviceConfig.Validate();
        }

        /// <summary>
        /// 難易度設定を適用します
        /// </summary>
        private void LoadDifficultySettings()
        {
            if (_serviceConfig?.DifficultyConfig != null)
            {
                _difficultySettings = _serviceConfig.DifficultyConfig.GetSettings(_currentDifficulty);
            }
            else
            {
                Debug.LogWarning("[AICombatService] DifficultyConfig not found. Using default settings.");
                _difficultySettings = GetDefaultDifficultySettings(_currentDifficulty);
            }
        }

        /// <summary>
        /// AIプレイヤーを作成します
        /// </summary>
        private AIPlayerData? CreateAIPlayer(Vector3 spawnPoint, Quaternion rotation, int teamIndex, ulong aiId)
        {
            if (_serviceConfig == null || !_serviceConfig.IsValid)
            {
                Debug.LogError("[AICombatService] AI prefabs not configured! Check AIServiceConfig asset.");
                return null;
            }

            // 馬をスポーン
            GameObject mount = UnityEngine.Object.Instantiate(_serviceConfig.AIMountPrefab!, spawnPoint, rotation);
            mount.name = $"AIMount_{aiId}";

            // 騎手をスポーン
            Transform? mountPoint = FindMountPoint(mount);
            Vector3 riderPosition = mountPoint != null ? mountPoint.position : spawnPoint + Vector3.up * 1.5f;
            Quaternion riderRotation = mountPoint != null ? mountPoint.rotation : rotation;

            GameObject rider = UnityEngine.Object.Instantiate(_serviceConfig.AIRiderPrefab!, riderPosition, riderRotation);
            rider.name = $"AIRider_{aiId}";

            // AIPlayerControllerを取得または追加
            AIPlayerController? aiController = rider.GetComponent<AIPlayerController>();
            if (aiController == null)
            {
                aiController = rider.AddComponent<AIPlayerController>();
            }

            // AIControllerを初期化
            aiController.Initialize(aiId, teamIndex, _currentGameMode, _difficultySettings, mount, this);

            // カスタマイズを適用
            ApplyRandomCustomization(mount, rider);

            // AIPlayerDataを作成
            AIPlayerData aiData = new AIPlayerData
            {
                AIId = aiId,
                TeamIndex = teamIndex,
                RootObject = rider,
                MountObject = mount,
                AIController = aiController,
                IsAlive = true
            };

            return aiData;
        }

        /// <summary>
        /// 馬のMountPointを探します
        /// </summary>
        private Transform? FindMountPoint(GameObject mount)
        {
            string[] mountPointNames = { "MountPoint", "Mount Point", "Seat", "RiderSeat" };

            foreach (string name in mountPointNames)
            {
                Transform? point = mount.transform.Find(name);
                if (point != null)
                {
                    return point;
                }

                // 再帰検索
                point = FindChildRecursive(mount.transform, name);
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
        /// ランダムなカスタマイズを適用します
        /// </summary>
        private void ApplyRandomCustomization(GameObject mount, GameObject rider)
        {
            if (_customizationService == null)
            {
                return;
            }

            // ランダムなカスタマイズを生成（将来的にはAI用のカスタマイズプールから選択）
            // 現在はデフォルトのカスタマイズを適用
            _customizationService.ApplyMountCustomization(mount);
            _customizationService.ApplyCharacterCustomization(rider);
        }

        /// <summary>
        /// AIを有効化します
        /// </summary>
        private void EnableAI(AIPlayerData aiData)
        {
            if (aiData.AIController != null)
            {
                aiData.AIController.Enable();
            }
        }

        /// <summary>
        /// AIを無効化します
        /// </summary>
        private void DisableAI(AIPlayerData aiData)
        {
            if (aiData.AIController != null)
            {
                aiData.AIController.Disable();
            }
        }

        /// <summary>
        /// キラーのIDを取得します
        /// </summary>
        /// <param name="killer">キラーのGameObject</param>
        /// <returns>キラーのID（0 = 不明）</returns>
        private ulong GetKillerId(GameObject? killer)
        {
            if (killer == null)
            {
                return 0;
            }

            // AIの場合
            var aiController = killer.GetComponentInParent<AIPlayerController>();
            if (aiController != null)
            {
                return aiController.AIId;
            }

            // プレイヤーの場合（NetworkBehaviourからClientIdを取得）
            var networkBehaviour = killer.GetComponentInParent<Unity.Netcode.NetworkBehaviour>();
            if (networkBehaviour != null && networkBehaviour.IsSpawned)
            {
                return networkBehaviour.OwnerClientId;
            }

            return 0;
        }

        /// <summary>
        /// デフォルトの難易度設定を取得します
        /// </summary>
        private DifficultySettings GetDefaultDifficultySettings(AIDifficulty difficulty)
        {
            return difficulty switch
            {
                AIDifficulty.Easy => new DifficultySettings
                {
                    ReactionTime = 1.5f,
                    AimAccuracy = 0.3f,
                    AttackInterval = new Vector2(3f, 5f),
                    VisionRange = 15f,
                    VisionAngle = 60f,
                    MoveSpeed = 3f,
                    TurnSpeed = 3f,
                    ChargeTimeMultiplier = 0.5f,
                    MissChance = 0.4f,
                    StrafeChance = 0.2f
                },
                AIDifficulty.Normal => new DifficultySettings
                {
                    ReactionTime = 1.0f,
                    AimAccuracy = 0.5f,
                    AttackInterval = new Vector2(2f, 4f),
                    VisionRange = 20f,
                    VisionAngle = 80f,
                    MoveSpeed = 4f,
                    TurnSpeed = 4f,
                    ChargeTimeMultiplier = 0.7f,
                    MissChance = 0.25f,
                    StrafeChance = 0.4f
                },
                AIDifficulty.Hard => new DifficultySettings
                {
                    ReactionTime = 0.5f,
                    AimAccuracy = 0.75f,
                    AttackInterval = new Vector2(1f, 3f),
                    VisionRange = 25f,
                    VisionAngle = 100f,
                    MoveSpeed = 5f,
                    TurnSpeed = 5f,
                    ChargeTimeMultiplier = 0.85f,
                    MissChance = 0.1f,
                    StrafeChance = 0.6f
                },
                AIDifficulty.Expert => new DifficultySettings
                {
                    ReactionTime = 0.2f,
                    AimAccuracy = 0.95f,
                    AttackInterval = new Vector2(0.5f, 2f),
                    VisionRange = 30f,
                    VisionAngle = 120f,
                    MoveSpeed = 6f,
                    TurnSpeed = 6f,
                    ChargeTimeMultiplier = 1.0f,
                    MissChance = 0.02f,
                    StrafeChance = 0.8f
                },
                _ => new DifficultySettings
                {
                    ReactionTime = 1.0f,
                    AimAccuracy = 0.5f,
                    AttackInterval = new Vector2(2f, 4f),
                    VisionRange = 20f,
                    VisionAngle = 80f,
                    MoveSpeed = 4f,
                    TurnSpeed = 4f,
                    ChargeTimeMultiplier = 0.7f,
                    MissChance = 0.25f,
                    StrafeChance = 0.4f
                }
            };
        }

        #endregion

        #region IDisposable

        /// <summary>
        /// リソースを解放します
        /// </summary>
        public void Dispose()
        {
            Cleanup();
        }

        #endregion
    }

    /// <summary>
    /// AIプレイヤーのデータ
    /// </summary>
    internal class AIPlayerData
    {
        /// <summary>AIのユニークID</summary>
        public ulong AIId { get; set; }

        /// <summary>チームインデックス</summary>
        public int TeamIndex { get; set; }

        /// <summary>AIプレイヤーのルートGameObject（騎手）</summary>
        public GameObject? RootObject { get; set; }

        /// <summary>馬のGameObject</summary>
        public GameObject? MountObject { get; set; }

        /// <summary>AIコントローラー</summary>
        public AIPlayerController? AIController { get; set; }

        /// <summary>生存状態</summary>
        public bool IsAlive { get; set; }
    }
}
