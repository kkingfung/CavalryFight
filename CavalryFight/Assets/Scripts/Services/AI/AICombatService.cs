#nullable enable

using System;
using System.Collections.Generic;
using CavalryFight.Core.Services;
using CavalryFight.Services.Lobby;
using CavalryFight.Services.Customization;
using UnityEngine;
using UnityEngine.SceneManagement;

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

        // AIをスポーンするターゲットシーン
        private Scene _targetScene;

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
            // 既に初期化済みで、設定が変わらない場合はスキップ
            if (_isInitialized && _currentGameMode == gameMode && _currentDifficulty == difficulty && _serviceConfig != null)
            {
                Debug.Log($"[AICombatService] Already initialized with same settings. Skipping re-initialization.");
                return;
            }

            _currentGameMode = gameMode;
            _currentDifficulty = difficulty;

            // サービス設定をロード（初回のみ、または失敗後の再試行）
            if (_serviceConfig == null)
            {
                LoadServiceConfig();
            }

            // 設定の検証（初期化時に早期に失敗させる）
            if (_serviceConfig == null)
            {
                Debug.LogError("[AICombatService] Initialize FAILED: AIServiceConfig could not be loaded from Resources/Settings/AIServiceConfig");
                Debug.LogError("[AICombatService] Please create the AIServiceConfig asset at: Assets/Resources/Settings/AIServiceConfig.asset");
                _isInitialized = false;
                return;
            }

            if (!_serviceConfig.IsValid)
            {
                Debug.LogError($"[AICombatService] Initialize FAILED: AIServiceConfig is invalid!");
                Debug.LogError($"[AICombatService] AIRiderPrefab: {(_serviceConfig.AIRiderPrefab != null ? _serviceConfig.AIRiderPrefab.name : "NULL")}");
                Debug.LogError($"[AICombatService] AIMountPrefab: {(_serviceConfig.AIMountPrefab != null ? _serviceConfig.AIMountPrefab.name : "NULL")}");
                Debug.LogError("[AICombatService] Please assign both AIRiderPrefab and AIMountPrefab in the AIServiceConfig asset.");
                _isInitialized = false;
                return;
            }

            // 難易度設定を適用
            LoadDifficultySettings();

            _isInitialized = true;
            _isEnabled = false;

            Debug.Log($"[AICombatService] Initialized successfully. GameMode: {gameMode}, Difficulty: {difficulty}");
            Debug.Log($"[AICombatService] Config: RiderPrefab={_serviceConfig.AIRiderPrefab!.name}, MountPrefab={_serviceConfig.AIMountPrefab!.name}");
        }

        /// <summary>
        /// AIをスポーンするターゲットシーンを設定します
        /// </summary>
        /// <remarks>
        /// ローディング画面表示中にAIをスポーンする場合、AIオブジェクトがローディングシーンに
        /// 作成されてしまう問題を回避するため、事前にターゲットシーンを設定する必要があります。
        /// </remarks>
        /// <param name="scene">AIをスポーンするシーン</param>
        public void SetTargetScene(Scene scene)
        {
            _targetScene = scene;
            Debug.Log($"[AICombatService] Target scene set to: {scene.name}");
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
            Debug.Log($"[AI-SPAWN-DEBUG] SpawnAIPlayer called - aiId:{aiId}, pos:{spawnPoint}, team:{teamIndex}");

            if (!_isInitialized)
            {
                Debug.LogError("[AICombatService] SpawnAIPlayer FAILED: Service not initialized! Call Initialize() first.");
                return null;
            }

            if (_aiPlayers.ContainsKey(aiId))
            {
                Debug.LogWarning($"[AICombatService] SpawnAIPlayer FAILED: AI with ID {aiId} already exists!");
                return null;
            }

            // 設定の最終確認（Initialize後でも何らかの理由でnullになっている可能性）
            if (_serviceConfig == null || !_serviceConfig.IsValid)
            {
                Debug.LogError($"[AICombatService] SpawnAIPlayer FAILED: Config is invalid!");
                Debug.LogError($"[AI-SPAWN-DEBUG] _serviceConfig={((_serviceConfig != null) ? "exists" : "NULL")}, IsValid={(_serviceConfig?.IsValid ?? false)}");
                Debug.LogError($"[AI-SPAWN-DEBUG] RiderPrefab={(_serviceConfig?.AIRiderPrefab != null ? _serviceConfig.AIRiderPrefab.name : "NULL")}, MountPrefab={(_serviceConfig?.AIMountPrefab != null ? _serviceConfig.AIMountPrefab.name : "NULL")}");
                Debug.LogError("[AICombatService] This should not happen if Initialize() succeeded. Check if the config asset or prefabs were destroyed.");
                return null;
            }

            Debug.Log($"[AI-SPAWN-DEBUG] Config OK: RiderPrefab={_serviceConfig.AIRiderPrefab!.name}, MountPrefab={_serviceConfig.AIMountPrefab!.name}");

            // AIプレイヤーを作成
            AIPlayerData? aiData = CreateAIPlayer(spawnPoint, rotation, teamIndex, aiId);
            if (aiData == null)
            {
                Debug.LogError($"[AICombatService] SpawnAIPlayer FAILED: CreateAIPlayer returned null for aiId {aiId}");
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
            int count = _aiPlayers.Count;
            Debug.Log($"[AICombatService] DespawnAllAIPlayers called. Despawning {count} AI players.");

            List<ulong> aiIds = new List<ulong>(_aiPlayers.Keys);
            foreach (ulong aiId in aiIds)
            {
                DespawnAIPlayer(aiId);
            }

            _aiPlayers.Clear();
            _teamAIPlayers.Clear();

            Debug.Log($"[AICombatService] All AI players despawned. _aiPlayers.Count={_aiPlayers.Count}");
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
            Debug.Log($"[AI-COMBAT-DEBUG] ========== EnableAllAI() START ==========");
            Debug.Log($"[AI-COMBAT-DEBUG] _aiPlayers.Count={_aiPlayers.Count}");

            _isEnabled = true;

            int enabledCount = 0;
            foreach (var kvp in _aiPlayers)
            {
                Debug.Log($"[AI-COMBAT-DEBUG] Enabling AI {kvp.Key} ({enabledCount + 1}/{_aiPlayers.Count})...");
                EnableAI(kvp.Value);
                enabledCount++;
            }

            Debug.Log($"[AICombatService] All AI enabled ({enabledCount} total)");
            Debug.Log($"[AI-COMBAT-DEBUG] ========== EnableAllAI() END ==========");
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

            // 既に死亡している場合はスキップ
            if (!aiData.IsAlive)
            {
                return;
            }

            aiData.IsAlive = false;

            if (aiData.AIController != null)
            {
                // notifyService: falseで呼び出して二重通知を防ぐ
                aiData.AIController.Die(killer, notifyService: false);
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

        /// <summary>
        /// AIが死亡したことを通知します（AIPlayerController.TakeDamageから呼ばれる）
        /// </summary>
        /// <param name="aiId">AIのユニークID</param>
        /// <param name="killer">キルしたGameObject</param>
        internal void NotifyAIDeath(ulong aiId, GameObject? killer)
        {
            if (!_aiPlayers.TryGetValue(aiId, out AIPlayerData? aiData))
            {
                Debug.LogWarning($"[AICombatService] NotifyAIDeath called for unknown AI {aiId}");
                return;
            }

            // 既に死亡している場合はスキップ
            if (!aiData.IsAlive)
            {
                return;
            }

            aiData.IsAlive = false;

            // キラーのIDを取得
            ulong killerId = GetKillerId(killer);

            // AIDiedイベントを発火
            AIDied?.Invoke(aiId, killerId);

            Debug.Log($"[AICombatService] AI {aiId} death notified, killed by {killerId}");
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
        /// <remarks>
        /// 正しく設定されたプレハブを使用する場合、PlayerSpawnerと同様に
        /// 単純なInstantiateで動作するはずです。
        /// AI Brainのみカウントダウン後まで無効化します。
        /// </remarks>
        private AIPlayerData? CreateAIPlayer(Vector3 spawnPoint, Quaternion rotation, int teamIndex, ulong aiId)
        {
            if (_serviceConfig == null || !_serviceConfig.IsValid)
            {
                Debug.LogError("[AICombatService] AI prefabs not configured! Check AIServiceConfig asset.");
                return null;
            }

            // マウントをインスタンス化（スポーンポイントをそのまま使用）
            // SpawnManagerから提供される位置は既に正しい地面位置
            Debug.Log($"[AICombatService] AI {aiId} spawn at: {spawnPoint}");
            GameObject mount = UnityEngine.Object.Instantiate(_serviceConfig.AIMountPrefab!, spawnPoint, rotation);
            mount.name = $"AIMount_{aiId}";

            // ターゲットシーンが設定されている場合、オブジェクトを移動
            // これにより、ローディング画面シーンではなくゲームプレイシーンにスポーンされる
            if (_targetScene.IsValid() && mount.scene != _targetScene)
            {
                SceneManager.MoveGameObjectToScene(mount, _targetScene);
                Debug.Log($"[AICombatService] Moved mount to scene: {_targetScene.name}");
            }

            // MAnimalを取得（AI Brain関連コンポーネントは無効化しない - 浮遊の原因になる可能性）
            var mAnimal = mount.GetComponentInChildren<MalbersAnimations.Controller.MAnimal>();
            // 注意: NavMeshAgent, MAnimalBrain, MAnimalAIControl の無効化は削除
            // これらを無効化するとMAnimalの接地処理に影響する可能性がある

            // 騎手をスポーン
            Transform? mountPoint = FindMountPoint(mount);
            Vector3 riderPosition = mountPoint != null ? mountPoint.position : spawnPoint + Vector3.up * 1.5f;
            Quaternion riderRotation = mountPoint != null ? mountPoint.rotation : rotation;

            // MRider.Awake()の前にRigidbodyが必要なため、非アクティブ状態でインスタンス化
            bool prefabWasActive = _serviceConfig.AIRiderPrefab!.activeSelf;
            _serviceConfig.AIRiderPrefab!.SetActive(false);
            GameObject rider = UnityEngine.Object.Instantiate(_serviceConfig.AIRiderPrefab!, riderPosition, riderRotation);
            _serviceConfig.AIRiderPrefab!.SetActive(prefabWasActive); // プレハブを元に戻す
            rider.name = $"AIRider_{aiId}";

            // ターゲットシーンが設定されている場合、騎手も移動（親設定前に必要）
            if (_targetScene.IsValid() && rider.scene != _targetScene)
            {
                SceneManager.MoveGameObjectToScene(rider, _targetScene);
                Debug.Log($"[AICombatService] Moved rider to scene: {_targetScene.name}");
            }

            // MRider用のRigidbodyを追加（PlayerSpawnerと同様、Awake前に追加）
            // RiderController.DisablePhysics()と同様の設定を適用
            Rigidbody? riderRb = rider.GetComponent<Rigidbody>();
            if (riderRb == null)
            {
                riderRb = rider.AddComponent<Rigidbody>();
            }
            // プレイヤーのDisablePhysics()と同様の設定
            riderRb.useGravity = false;
            riderRb.isKinematic = true; // 騎乗時はkinematic
            riderRb.detectCollisions = false; // 馬との衝突を完全に無効化

            // CharacterControllerがあれば無効化（プレイヤーと同様）
            var cc = rider.GetComponent<CharacterController>();
            if (cc != null)
            {
                cc.enabled = false;
            }

            // GameObjectをアクティブ化（これによりAwake()が呼ばれる）
            rider.SetActive(true);

            // 騎手を馬のマウントポイントに配置（RiderController.MountTo()と同様の処理）
            if (mountPoint != null)
            {
                rider.transform.SetParent(mountPoint);
                rider.transform.localPosition = Vector3.zero;
                rider.transform.localRotation = Quaternion.identity;
            }
            else
            {
                rider.transform.SetParent(mount.transform);
                rider.transform.localPosition = Vector3.up * 1.5f;
                rider.transform.localRotation = Quaternion.identity;
            }

            // ライダーとマウントのコライダー間の衝突を無視する（物理的な反発を防ぐ）
            IgnoreCollisionsBetweenRiderAndMount(rider, mount);

            // AIPlayerControllerを取得（プレハブの子オブジェクトにある場合も検索、非アクティブも含む）
            AIPlayerController? aiController = rider.GetComponentInChildren<AIPlayerController>(true);
            if (aiController != null)
            {
                Debug.Log($"[AICombatService] AIPlayerController found on: {aiController.gameObject.name}");
            }
            else
            {
                // フォールバック: ルートに追加（プレハブに設定されていない場合）
                Debug.LogWarning("[AICombatService] AIPlayerController not found in prefab, adding to root");
                aiController = rider.AddComponent<AIPlayerController>();
            }

            // AIControllerを初期化
            Debug.Log($"[AICombatService] Calling Initialize on aiController (type={aiController.GetType().Name}, gameObject={aiController.gameObject.name})...");
            aiController.Initialize(aiId, teamIndex, _currentGameMode, _difficultySettings, mount, this);
            Debug.Log($"[AICombatService] Initialize completed for AI {aiId}");

            // カスタマイズを適用
            try
            {
                ApplyRandomCustomization(mount, rider);
            }
            catch (System.Exception e)
            {
                Debug.LogError($"[AI-VISIBILITY] ApplyRandomCustomization failed: {e.Message}");
            }

            // ライダーのみレイヤーをDefaultに設定（Preview レイヤーからの変更）
            // マウントのレイヤーは変更しない - Horse RealisticはAnimalレイヤーである必要がある
            int defaultLayer = LayerMask.NameToLayer("Default");
            SetLayerRecursively(rider, defaultLayer);
            // SetLayerRecursively(mount, defaultLayer); // マウントのレイヤーは変更しない

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
        /// 地面の位置を検出します
        /// </summary>
        /// <param name="spawnPoint">元のスポーン位置</param>
        /// <returns>地面に調整されたスポーン位置</returns>
        private Vector3 FindGroundPosition(Vector3 spawnPoint)
        {
            // 元のスポーンポイントをそのまま使用
            // SpawnManagerから提供されるスポーンポイントは既に正しい地面位置にあるため、
            // 余計なレイキャスト調整は不要（むしろ間違った位置を検出する可能性がある）
            return spawnPoint;
        }

        /// <summary>
        /// MAnimalの物理状態を安定化させます
        /// </summary>
        /// <remarks>
        /// スポーン直後のRigidbody速度をリセットし、重力を有効化します。
        /// 重要: MAnimalのtransform.positionを直接変更してはいけません。
        /// MAnimalは内部で位置を管理しており、直接変更すると物理状態が不安定になります。
        /// プレハブオフセットはスポーン位置の事前調整で対応済みです。
        /// </remarks>
        private void StabilizeMAnimalPhysics(GameObject mount, MalbersAnimations.Controller.MAnimal? mAnimal)
        {
            if (mAnimal == null) return;

            // MAnimalが持つRigidbodyの速度をリセット
            var rb = mAnimal.GetComponent<Rigidbody>();
            if (rb != null)
            {
                rb.linearVelocity = Vector3.zero;
                rb.angularVelocity = Vector3.zero;

                // 重力を有効化（kinematicではない）
                rb.useGravity = true;
                rb.isKinematic = false;

                Debug.Log($"[AICombatService] Mount Rigidbody: useGravity={rb.useGravity}, isKinematic={rb.isKinematic}");
            }

            // MAnimalの重力設定を確認・有効化
            // MAnimal.UseGravity プロパティを使用して重力を有効化
            if (!mAnimal.UseGravity)
            {
                Debug.LogWarning($"[AICombatService] MAnimal UseGravity was false, enabling...");
                mAnimal.UseGravity = true;
            }

            // MAnimal.Grounded を確認
            Debug.Log($"[AICombatService] MAnimal: UseGravity={mAnimal.UseGravity}, Grounded={mAnimal.Grounded}");
        }

        /// <summary>
        /// スポーン後に物理状態を監視するコルーチン
        /// </summary>
        /// <remarks>
        /// AIマウントの物理設定を確認し、重力が正常に機能しているかを監視します。
        /// </remarks>
        private System.Collections.IEnumerator StabilizePhysicsAfterSpawn(GameObject mount, MalbersAnimations.Controller.MAnimal mAnimal, ulong aiId)
        {
            // 数フレーム待って物理が安定するのを待つ
            yield return new WaitForSeconds(0.5f);

            if (mount == null || mAnimal == null)
            {
                yield break;
            }

            // Rigidbodyの設定を再確認・強制設定
            var rb = mAnimal.GetComponent<Rigidbody>();
            if (rb != null)
            {
                Debug.Log($"[AICombatService] AI {aiId} Rigidbody: useGravity={rb.useGravity}, isKinematic={rb.isKinematic}, drag={rb.linearDamping}, constraints={rb.constraints}");

                // 重力を強制有効化
                if (!rb.useGravity || rb.isKinematic)
                {
                    Debug.LogWarning($"[AICombatService] AI {aiId} fixing Rigidbody settings for gravity...");
                    rb.useGravity = true;
                    rb.isKinematic = false;
                }
            }

            // MAnimalの重力を再確認
            if (!mAnimal.UseGravity)
            {
                Debug.LogWarning($"[AICombatService] AI {aiId} MAnimal.UseGravity was false, enabling...");
                mAnimal.UseGravity = true;
            }

            // 現在の状態をログ
            Debug.Log($"[AICombatService] AI {aiId} physics check - Position Y: {mount.transform.position.y}, Grounded: {mAnimal.Grounded}, UseGravity: {mAnimal.UseGravity}");

            // 2秒後に接地確認
            yield return new WaitForSeconds(2f);

            if (mount != null && mAnimal != null)
            {
                Debug.Log($"[AICombatService] AI {aiId} final check - Position Y: {mount.transform.position.y}, Grounded: {mAnimal.Grounded}");

                if (!mAnimal.Grounded)
                {
                    Debug.LogError($"[AICombatService] AI {aiId} is NOT grounded! Check prefab Rigidbody/MAnimal settings.");
                }
            }
        }

        /// <summary>
        /// ライダーとマウントのコライダー間の衝突を無視する
        /// </summary>
        /// <remarks>
        /// ライダーが馬に騎乗した際に物理的な反発が起きないようにするため、
        /// 両方のGameObjectに含まれる全てのCollider間の衝突を無視します。
        /// </remarks>
        private void IgnoreCollisionsBetweenRiderAndMount(GameObject rider, GameObject mount)
        {
            // ライダーの全コライダーを取得
            Collider[] riderColliders = rider.GetComponentsInChildren<Collider>(true);

            // マウントの全コライダーを取得
            Collider[] mountColliders = mount.GetComponentsInChildren<Collider>(true);

            int ignoredCount = 0;

            // 全ての組み合わせで衝突を無視
            foreach (var riderCollider in riderColliders)
            {
                foreach (var mountCollider in mountColliders)
                {
                    if (riderCollider != null && mountCollider != null)
                    {
                        Physics.IgnoreCollision(riderCollider, mountCollider, true);
                        ignoredCount++;
                    }
                }
            }

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
            // カスタマイズサービスを取得（まだ取得していない場合）
            if (_customizationService == null)
            {
                _customizationService = ServiceLocator.Instance.Get<ICustomizationService>();
            }

            if (_customizationService == null)
            {
                Debug.LogWarning("[AI-VISIBILITY] CustomizationService is NULL, cannot apply customization");
                return;
            }

            // ランダムなカスタマイズを生成
            var randomCharacter = GenerateRandomCharacterCustomization();
            var randomMount = GenerateRandomMountCustomization();

            // Applierを取得して直接カスタマイズを適用
            var characterApplier = _customizationService.GetP09CharacterApplier();
            var mountApplier = _customizationService.GetMalbersHorseApplier();

            if (characterApplier != null)
            {
                // P09モデルを子から検索（RiderController.GetCustomizationTarget()と同様）
                GameObject riderTarget = FindP09ModelInChildren(rider) ?? rider;
                characterApplier.Apply(riderTarget, randomCharacter);

                // 弓オブジェクトをセットアップ
                SetupBowForAI(riderTarget, randomCharacter.BowId);
            }

            // AIPlayerControllerに矢タイプを設定（カスタマイズと同じ矢プレハブを使用）
            var aiController = rider.GetComponentInChildren<AIPlayerController>(true);
            if (aiController != null)
            {
                aiController.SetArrowType(randomCharacter.ArrowType);
                Debug.Log($"[AI-VISIBILITY] Set ArrowType to {randomCharacter.ArrowType} for AI");
            }
            else
            {
                Debug.LogWarning("[AI-VISIBILITY] AIPlayerController not found on rider, cannot set ArrowType");
            }

            if (mountApplier != null)
            {
                GameObject mountTarget = FindHorseRealisticInChildren(mount) ?? mount;
                mountApplier.Apply(mountTarget, randomMount);
            }
        }

        /// <summary>
        /// 子オブジェクトからP09モデルを探します
        /// </summary>
        /// <param name="parent">親GameObject</param>
        /// <returns>P09モデルのGameObject（見つからない場合はnull）</returns>
        private GameObject? FindP09ModelInChildren(GameObject parent)
        {
            // 直接の子を検索
            foreach (Transform child in parent.transform)
            {
                if (child.name.Contains("P09"))
                {
                    return child.gameObject;
                }
            }

            // 再帰的に検索（Track/Effect/Particleは除外）
            foreach (Transform child in parent.transform)
            {
                if (child.name.Contains("Track") ||
                    child.name.Contains("Effect") ||
                    child.name.Contains("Particle"))
                {
                    continue;
                }

                var found = FindP09ModelInChildren(child.gameObject);
                if (found != null)
                {
                    return found;
                }
            }

            return null;
        }

        /// <summary>
        /// AI用の弓をセットアップします
        /// </summary>
        /// <param name="riderTarget">P09モデルのGameObject</param>
        /// <param name="bowId">弓のID</param>
        private void SetupBowForAI(GameObject riderTarget, int bowId)
        {
            // すべての弓オブジェクトを取得（Bow_001, Bow_002, etc.）
            var allChildren = riderTarget.GetComponentsInChildren<Transform>(true);
            List<GameObject> allBows = new List<GameObject>();

            foreach (var child in allChildren)
            {
                // Bow_001, Bow_002, Bow_003, Bow_004 形式を検索
                if (System.Text.RegularExpressions.Regex.IsMatch(child.name, @"^Bow_\d{3}$"))
                {
                    allBows.Add(child.gameObject);
                }
            }

            if (allBows.Count == 0)
            {
                Debug.LogWarning("[AI-VISIBILITY] No bow meshes (Bow_00X) found for AI");
                return;
            }

            // bowId に対応する弓名を生成
            // P09のWeapon IDs: 10-13=弓、メッシュ名はBow_001-004
            // BowId 10 → Bow_001, BowId 11 → Bow_002, etc.
            int bowMeshIndex = bowId >= 10 ? bowId - 9 : bowId; // 10→1, 11→2, 12→3, 13→4
            string targetBowName = $"Bow_{bowMeshIndex:D3}";
            GameObject? targetBow = null;

            // すべての弓を無効化し、対象の弓を見つける
            foreach (var bow in allBows)
            {
                if (bow.name == targetBowName)
                {
                    targetBow = bow;
                }
                else
                {
                    bow.SetActive(false);
                }
            }

            // ターゲット弓が見つからない場合は最初の弓を使用
            if (targetBow == null && allBows.Count > 0)
            {
                targetBow = allBows[0];
                Debug.LogWarning($"[AI-VISIBILITY] Target bow '{targetBowName}' not found, using {targetBow.name}");
            }

            if (targetBow == null)
            {
                Debug.LogError("[AI-VISIBILITY] No bow could be activated");
                return;
            }

            // 弓をアクティブ化
            targetBow.SetActive(true);

            // 弓が手の下にない場合は左手に配置
            var animator = riderTarget.GetComponentInChildren<Animator>();
            if (animator != null)
            {
                Transform? leftHand = animator.GetBoneTransform(HumanBodyBones.LeftHand);
                if (leftHand != null && !IsUnderHandBone(targetBow.transform.parent))
                {
                    targetBow.transform.SetParent(leftHand);
                    targetBow.transform.localPosition = new Vector3(0.05f, 0.02f, 0f);
                    targetBow.transform.localRotation = Quaternion.Euler(0f, -90f, -90f);
                }
            }
        }

        /// <summary>
        /// 弓オブジェクトを検索します
        /// </summary>
        private GameObject? FindBowObject(Transform parent)
        {
            var allChildren = parent.GetComponentsInChildren<Transform>(true);

            // P09の実際の弓メッシュを探す（P09_Bow_XX形式）
            foreach (var child in allChildren)
            {
                // P09_Bow で始まる実際の弓オブジェクトを探す
                if (child.name.StartsWith("P09_Bow") && child.gameObject.activeSelf)
                {
                    return child.gameObject;
                }
            }

            // アクティブなP09弓が見つからない場合、非アクティブも検索
            foreach (var child in allChildren)
            {
                if (child.name.StartsWith("P09_Bow"))
                {
                    return child.gameObject;
                }
            }

            // P09弓が見つからない場合、一般的な弓を探す（Bow_Target等を除外）
            foreach (var child in allChildren)
            {
                if (child.name.Contains("Bow") &&
                    !child.name.Contains("Sword") &&
                    !child.name.Contains("Target") &&
                    !child.name.Contains("Holder") &&
                    child.gameObject.activeSelf)
                {
                    return child.gameObject;
                }
            }

            return null;
        }

        /// <summary>
        /// 指定されたTransformが手のボーンの下にあるかチェックします
        /// </summary>
        private bool IsUnderHandBone(Transform? transform)
        {
            if (transform == null) return false;

            string[] handBonePatterns = { "LeftHand", "Hand_L", "L_Hand", "Left Hand" };

            Transform? current = transform;
            while (current != null)
            {
                foreach (var pattern in handBonePatterns)
                {
                    if (current.name.Contains(pattern))
                    {
                        return true;
                    }
                }
                current = current.parent;
            }

            return false;
        }

        /// <summary>
        /// GameObjectとすべての子のレイヤーを再帰的に設定します
        /// </summary>
        /// <param name="obj">対象のGameObject</param>
        /// <param name="layer">設定するレイヤー</param>
        private void SetLayerRecursively(GameObject obj, int layer)
        {
            obj.layer = layer;
            foreach (Transform child in obj.transform)
            {
                SetLayerRecursively(child.gameObject, layer);
            }
        }

        /// <summary>
        /// 子オブジェクトからHorse Realisticモデルを探します
        /// </summary>
        /// <param name="parent">親GameObject</param>
        /// <returns>Horse RealisticのGameObject（見つからない場合はnull）</returns>
        private GameObject? FindHorseRealisticInChildren(GameObject parent)
        {
            // "Horse Realistic" または "Horse" を含む子を検索
            foreach (Transform child in parent.transform)
            {
                if (child.name.Contains("Horse Realistic") || child.name == "Horse")
                {
                    return child.gameObject;
                }
            }

            // 再帰的に検索
            foreach (Transform child in parent.transform)
            {
                var found = FindHorseRealisticInChildren(child.gameObject);
                if (found != null)
                {
                    return found;
                }
            }

            return null;
        }

        /// <summary>
        /// ランダムなキャラクターカスタマイズを生成します
        /// </summary>
        /// <remarks>
        /// CharacterCustomization.csのコメントに従った正しい範囲を使用:
        /// - FaceType: 1-3 (P09は1-based indexing)
        /// - HairstyleId: 0-14 (0=なし)
        /// - HairColorId: 1-9
        /// - EyeColorId: 1-5
        /// - SkinToneId: 1-3
        /// - BustSize: 1-3
        /// - HeadArmorId: 0=なし, 2-12
        /// - ChestArmorId: 0=素体, 1-12
        /// - BowId: 10-13 (P09のWeapon IDs: 1-5=剣, 6-9=杖, 10-13=弓)
        /// </remarks>
        private CharacterCustomization GenerateRandomCharacterCustomization()
        {
            // ArrowType enumの数を取得
            int arrowTypeCount = System.Enum.GetValues(typeof(ArrowType)).Length;

            var customization = new CharacterCustomization
            {
                Gender = (Gender)UnityEngine.Random.Range(0, 2),
                FaceType = UnityEngine.Random.Range(1, 4),       // 1-3 (P09は1-based)
                HairstyleId = UnityEngine.Random.Range(0, 15),   // 0-14 (0=なし)
                HairColorId = UnityEngine.Random.Range(1, 10),   // 1-9
                EyeColorId = UnityEngine.Random.Range(1, 6),     // 1-5
                FacialHairId = UnityEngine.Random.Range(0, 9),   // 0-8 (0=なし)
                SkinToneId = UnityEngine.Random.Range(1, 4),     // 1-3
                BustSize = UnityEngine.Random.Range(1, 4),       // 1-3
                HeadArmorId = UnityEngine.Random.Range(0, 13),   // 0, 2-12 (0=なし)
                ChestArmorId = UnityEngine.Random.Range(0, 13),  // 0-12 (0=素体)
                ArmsArmorId = UnityEngine.Random.Range(0, 13),   // 0-12 (0=素体)
                WaistArmorId = UnityEngine.Random.Range(1, 13),  // 1-12
                LegsArmorId = UnityEngine.Random.Range(0, 13),   // 0-12 (0=素体)
                BowId = UnityEngine.Random.Range(10, 14),        // 10-13 (弓のWeapon ID)
                ArrowType = (ArrowType)UnityEngine.Random.Range(0, arrowTypeCount)  // ランダムな矢タイプ
            };
            return customization;
        }

        /// <summary>
        /// ランダムな馬カスタマイズを生成します
        /// </summary>
        private MountCustomization GenerateRandomMountCustomization()
        {
            var customization = new MountCustomization
            {
                MountType = (MountType)UnityEngine.Random.Range(0, System.Enum.GetValues(typeof(MountType)).Length),
                CoatColor = (HorseColor)UnityEngine.Random.Range(0, System.Enum.GetValues(typeof(HorseColor)).Length),
                ManeStyle = (ManeStyle)UnityEngine.Random.Range(0, System.Enum.GetValues(typeof(ManeStyle)).Length),
                ManeColor = (ManeColor)UnityEngine.Random.Range(0, System.Enum.GetValues(typeof(ManeColor)).Length),
                ArmorId = UnityEngine.Random.Range(0, 4),
                HasSaddle = UnityEngine.Random.value > 0.5f
            };
            return customization;
        }

        /// <summary>
        /// AIを有効化します
        /// </summary>
        /// <remarks>
        /// 重要: 馬のAI Brain関連コンポーネントを先に有効化してから
        /// AIControllerを有効化する必要があります。
        /// AIControllerはEnable時にMAnimalAIControlにターゲットを設定するため、
        /// MAnimalAIControlが有効でないとターゲット設定が機能しません。
        /// </remarks>
        private void EnableAI(AIPlayerData aiData)
        {
            Debug.Log($"[AI-ENABLE-DEBUG] ========== EnableAI START for AI {aiData.AIId} ==========");

            // 重要: 馬のAI Brain関連コンポーネントを先に有効化
            // AIControllerはEnable時にMAnimalAIControlにターゲットを設定するため
            if (aiData.MountObject != null)
            {
                Debug.Log($"[AI-ENABLE-DEBUG] AI {aiData.AIId}: MountObject = {aiData.MountObject.name}");

                // NavMeshAgent
                var navAgent = aiData.MountObject.GetComponentInChildren<UnityEngine.AI.NavMeshAgent>();
                if (navAgent != null)
                {
                    bool wasEnabled = navAgent.enabled;
                    navAgent.enabled = true;
                    Debug.Log($"[AI-ENABLE-DEBUG] AI {aiData.AIId}: NavMeshAgent - wasEnabled={wasEnabled}, nowEnabled={navAgent.enabled}, isOnNavMesh={navAgent.isOnNavMesh}, position={navAgent.transform.position}");

                    if (!navAgent.isOnNavMesh)
                    {
                        Debug.LogError($"[AI-ENABLE-DEBUG] AI {aiData.AIId}: NavMeshAgent is NOT on NavMesh! AI will NOT be able to move!");

                        // 最寄りのNavMesh位置を検索
                        if (UnityEngine.AI.NavMesh.SamplePosition(navAgent.transform.position, out UnityEngine.AI.NavMeshHit hit, 50f, UnityEngine.AI.NavMesh.AllAreas))
                        {
                            Debug.Log($"[AI-ENABLE-DEBUG] AI {aiData.AIId}: Nearest NavMesh position found at {hit.position}, distance={hit.distance:F2}m. Attempting warp...");
                            navAgent.Warp(hit.position);
                            Debug.Log($"[AI-ENABLE-DEBUG] AI {aiData.AIId}: After warp - isOnNavMesh={navAgent.isOnNavMesh}");
                        }
                        else
                        {
                            Debug.LogError($"[AI-ENABLE-DEBUG] AI {aiData.AIId}: No NavMesh found within 50m! Check if NavMeshSurface is baked.");
                        }
                    }
                }
                else
                {
                    Debug.LogError($"[AI-ENABLE-DEBUG] AI {aiData.AIId}: NavMeshAgent NOT FOUND on mount!");
                }

                // MAnimalAIControl (true = 非アクティブなGameObjectも含める)
                var aiControl = aiData.MountObject.GetComponentInChildren<MalbersAnimations.Controller.AI.MAnimalAIControl>(true);
                if (aiControl != null)
                {
                    // GameObjectがアクティブでない場合は先にアクティブにする
                    if (!aiControl.gameObject.activeInHierarchy)
                    {
                        Debug.Log($"[AI-ENABLE-DEBUG] AI {aiData.AIId}: MAnimalAIControl GameObject was inactive, activating...");
                        aiControl.gameObject.SetActive(true);
                    }
                    bool wasEnabled = aiControl.enabled;
                    aiControl.enabled = true;
                    Debug.Log($"[AI-ENABLE-DEBUG] AI {aiData.AIId}: MAnimalAIControl - wasEnabled={wasEnabled}, nowEnabled={aiControl.enabled}, gameObject={aiControl.gameObject.name}");
                }
                else
                {
                    Debug.LogError($"[AI-ENABLE-DEBUG] AI {aiData.AIId}: MAnimalAIControl NOT FOUND on mount (even including inactive objects)!");
                }

                // MAnimalBrain (true = 非アクティブなGameObjectも含める)
                var aiBrain = aiData.MountObject.GetComponentInChildren<MalbersAnimations.Controller.AI.MAnimalBrain>(true);
                if (aiBrain != null)
                {
                    // GameObjectがアクティブでない場合は先にアクティブにする
                    if (!aiBrain.gameObject.activeInHierarchy)
                    {
                        Debug.Log($"[AI-ENABLE-DEBUG] AI {aiData.AIId}: MAnimalBrain GameObject was inactive, activating...");
                        aiBrain.gameObject.SetActive(true);
                    }
                    bool wasEnabled = aiBrain.enabled;
                    aiBrain.enabled = true;
                    Debug.Log($"[AI-ENABLE-DEBUG] AI {aiData.AIId}: MAnimalBrain - wasEnabled={wasEnabled}, nowEnabled={aiBrain.enabled}, gameObject={aiBrain.gameObject.name}");
                }
                else
                {
                    Debug.LogWarning($"[AI-ENABLE-DEBUG] AI {aiData.AIId}: MAnimalBrain NOT FOUND on mount (may be optional)");
                }

                // MAnimal 状態確認
                var mAnimal = aiData.MountObject.GetComponentInChildren<MalbersAnimations.Controller.MAnimal>();
                if (mAnimal != null)
                {
                    Debug.Log($"[AI-ENABLE-DEBUG] AI {aiData.AIId}: MAnimal - enabled={mAnimal.enabled}, Grounded={mAnimal.Grounded}, UseGravity={mAnimal.UseGravity}, ActiveState={mAnimal.ActiveState?.name ?? "NULL"}");
                }

                Debug.Log($"[AICombatService] Mount AI components enabled for AI {aiData.AIId}");
            }
            else
            {
                Debug.LogError($"[AI-ENABLE-DEBUG] AI {aiData.AIId}: MountObject is NULL!");
            }

            // AIControllerを有効化（MAnimalAIControlが有効になった後）
            if (aiData.AIController != null)
            {
                Debug.Log($"[AI-ENABLE-DEBUG] AI {aiData.AIId}: Calling AIController.Enable()...");
                aiData.AIController.Enable();
            }
            else
            {
                Debug.LogError($"[AI-ENABLE-DEBUG] AI {aiData.AIId}: AIController is NULL!");
            }

            Debug.Log($"[AI-ENABLE-DEBUG] ========== EnableAI END for AI {aiData.AIId} ==========");
        }

        /// <summary>
        /// AIを無効化します
        /// </summary>
        /// <remarks>
        /// AI Brain関連コンポーネントを無効化するだけ。
        /// MAnimalの状態は特に変更しない。
        /// </remarks>
        private void DisableAI(AIPlayerData aiData)
        {
            if (aiData.AIController != null)
            {
                aiData.AIController.Disable();
            }

            if (aiData.MountObject != null)
            {
                // AI Brainを無効化 (true = 非アクティブなGameObjectも含める)
                var aiBrain = aiData.MountObject.GetComponentInChildren<MalbersAnimations.Controller.AI.MAnimalBrain>(true);
                if (aiBrain != null)
                {
                    aiBrain.enabled = false;
                }

                // MAnimalAIControlを無効化 (true = 非アクティブなGameObjectも含める)
                var aiControl = aiData.MountObject.GetComponentInChildren<MalbersAnimations.Controller.AI.MAnimalAIControl>(true);
                if (aiControl != null)
                {
                    aiControl.Stop(); // 移動も停止
                    aiControl.enabled = false;
                }

                // NavMeshAgentを無効化
                var navAgent = aiData.MountObject.GetComponentInChildren<UnityEngine.AI.NavMeshAgent>();
                if (navAgent != null)
                {
                    navAgent.enabled = false;
                }
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
                    // 基本パラメータ
                    ReactionTime = 1.5f,
                    AimAccuracy = 0.3f,
                    AttackInterval = new Vector2(3f, 5f),
                    VisionRange = 15f,
                    VisionAngle = 60f,
                    MoveSpeed = 3f,
                    TurnSpeed = 3f,
                    ChargeTimeMultiplier = 0.5f,
                    MissChance = 0.4f,
                    StrafeChance = 0.2f,
                    // 拡張パラメータ
                    LeadTargetFactor = 0.1f,
                    FeintChance = 0f,
                    DodgeEffectiveness = 0.2f,
                    DodgeTriggerDistance = 5f,
                    MaxSimultaneousTargets = 1,
                    ThreatAssessmentInterval = 2.0f,
                    CounterPlayChance = 0f,
                    MinFireCharge = 0.2f,
                    TerrainAwarenessChance = 0f,
                    CoverUsageChance = 0f
                },
                AIDifficulty.Normal => new DifficultySettings
                {
                    // 基本パラメータ
                    ReactionTime = 1.0f,
                    AimAccuracy = 0.5f,
                    AttackInterval = new Vector2(2f, 4f),
                    VisionRange = 20f,
                    VisionAngle = 80f,
                    MoveSpeed = 4f,
                    TurnSpeed = 4f,
                    ChargeTimeMultiplier = 0.7f,
                    MissChance = 0.25f,
                    StrafeChance = 0.4f,
                    // 拡張パラメータ
                    LeadTargetFactor = 0.4f,
                    FeintChance = 0.1f,
                    DodgeEffectiveness = 0.4f,
                    DodgeTriggerDistance = 10f,
                    MaxSimultaneousTargets = 2,
                    ThreatAssessmentInterval = 1.0f,
                    CounterPlayChance = 0.2f,
                    MinFireCharge = 0.3f,
                    TerrainAwarenessChance = 0.2f,
                    CoverUsageChance = 0.15f
                },
                AIDifficulty.Hard => new DifficultySettings
                {
                    // 基本パラメータ
                    ReactionTime = 0.5f,
                    AimAccuracy = 0.75f,
                    AttackInterval = new Vector2(1f, 3f),
                    VisionRange = 25f,
                    VisionAngle = 100f,
                    MoveSpeed = 5f,
                    TurnSpeed = 5f,
                    ChargeTimeMultiplier = 0.85f,
                    MissChance = 0.1f,
                    StrafeChance = 0.6f,
                    // 拡張パラメータ
                    LeadTargetFactor = 0.7f,
                    FeintChance = 0.25f,
                    DodgeEffectiveness = 0.7f,
                    DodgeTriggerDistance = 15f,
                    MaxSimultaneousTargets = 3,
                    ThreatAssessmentInterval = 0.5f,
                    CounterPlayChance = 0.5f,
                    MinFireCharge = 0.5f,
                    TerrainAwarenessChance = 0.5f,
                    CoverUsageChance = 0.4f
                },
                AIDifficulty.Expert => new DifficultySettings
                {
                    // 基本パラメータ
                    ReactionTime = 0.2f,
                    AimAccuracy = 0.95f,
                    AttackInterval = new Vector2(0.5f, 2f),
                    VisionRange = 30f,
                    VisionAngle = 120f,
                    MoveSpeed = 6f,
                    TurnSpeed = 6f,
                    ChargeTimeMultiplier = 1.0f,
                    MissChance = 0.02f,
                    StrafeChance = 0.8f,
                    // 拡張パラメータ
                    LeadTargetFactor = 0.95f,
                    FeintChance = 0.4f,
                    DodgeEffectiveness = 0.9f,
                    DodgeTriggerDistance = 20f,
                    MaxSimultaneousTargets = 5,
                    ThreatAssessmentInterval = 0.2f,
                    CounterPlayChance = 0.8f,
                    MinFireCharge = 0.6f,
                    TerrainAwarenessChance = 0.8f,
                    CoverUsageChance = 0.7f
                },
                _ => new DifficultySettings
                {
                    // 基本パラメータ（Normal相当）
                    ReactionTime = 1.0f,
                    AimAccuracy = 0.5f,
                    AttackInterval = new Vector2(2f, 4f),
                    VisionRange = 20f,
                    VisionAngle = 80f,
                    MoveSpeed = 4f,
                    TurnSpeed = 4f,
                    ChargeTimeMultiplier = 0.7f,
                    MissChance = 0.25f,
                    StrafeChance = 0.4f,
                    // 拡張パラメータ
                    LeadTargetFactor = 0.4f,
                    FeintChance = 0.1f,
                    DodgeEffectiveness = 0.4f,
                    DodgeTriggerDistance = 10f,
                    MaxSimultaneousTargets = 2,
                    ThreatAssessmentInterval = 1.0f,
                    CounterPlayChance = 0.2f,
                    MinFireCharge = 0.3f,
                    TerrainAwarenessChance = 0.2f,
                    CoverUsageChance = 0.15f
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
