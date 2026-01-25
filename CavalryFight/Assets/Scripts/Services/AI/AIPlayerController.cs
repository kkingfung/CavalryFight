#nullable enable

using System;
using System.Collections;
using System.Text.RegularExpressions;
using CavalryFight.Core.Services;
using CavalryFight.Services.Audio;
using CavalryFight.Services.Lobby;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.Animations;

namespace CavalryFight.Services.AI
{
    /// <summary>
    /// AIプレイヤーの行動を制御するコントローラー
    /// </summary>
    /// <remarks>
    /// BlazeAIと連携して騎馬弓兵AIの戦闘行動を実装します。
    /// 敵の検出、追跡、攻撃、回避などの行動を管理します。
    /// </remarks>
    [RequireComponent(typeof(Animator))]
    public class AIPlayerController : MonoBehaviour
    {
        #region Serialized Fields

        [Header("Combat Settings")]
        [SerializeField] private Transform? _bowFirePoint;
        [SerializeField] private GameObject? _arrowPrefab;
        [SerializeField] private float _minArrowSpeed = 15f;
        [SerializeField] private float _maxArrowSpeed = 50f;
        [SerializeField] private float _maxChargeTime = 2f;
        [SerializeField] private float _attackRange = 25f;

        [Header("Health")]
        [SerializeField] private int _maxHealth = 100;

        [Header("Detection")]
        [SerializeField] private LayerMask _enemyLayers;
        [SerializeField] private LayerMask _obstacleLayers;

        [Header("Audio")]
        [SerializeField] private AudioClip? _shootSfx;

        #endregion

        #region Private Fields

        private ulong _aiId;
        private int _teamIndex;
        private GameMode _gameMode;
        private DifficultySettings _difficultySettings;
        private IModeBehavior? _modeBehavior;
        private AIGameModeBehavior? _gameModeBehaviorConfig;
        private GameObject? _mountObject;
        private AICombatService? _combatService;

        private Animator? _animator;
        private NavMeshAgent? _navAgent;
        private IAudioService? _audioService;

        // BlazeAI関連（グローバル名前空間、存在しない場合はnull）
        private MonoBehaviour? _blazeAI;

        // Malbers MAnimal（馬の移動制御）
        private MalbersAnimations.Controller.MAnimal? _mAnimal;
        private MalbersAnimations.Controller.AI.MAnimalAIControl? _mAnimalAIControl;

        // MAnimalBrain（Malbers AI）が存在する場合、移動はMAnimalBrainに委譲
        private MonoBehaviour? _mAnimalBrain;
        private bool _useMAnimalBrainForMovement;

        // 上半身のエイム回転用（PlayerControllerと同様）
        private Transform? _spineTransform;
        private Transform? _headTransform;
        private float _currentSpineYRotation = 0f;
        private float _currentSpineXRotation = 0f;
        private float _aimRotationSpeed = 10f;
        private bool _hairReparented = false;

        // 上半身の回転制限（RiderArcherControllerと同様）
        private const float MaxHorizontalRotation = 70f;
        private const float MaxVerticalRotation = 45f;

        // 状態
        private AIState _currentState = AIState.Idle;
        private bool _isEnabled;
        private bool _isAlive = true;
        private int _currentHealth;

        // ターゲット
        private GameObject? _currentTarget;
        private GameObject? _lastAttacker; // 最後に攻撃してきた敵（TargetPriority.Attacker用）
        private Vector3 _lastKnownTargetPosition;
        private float _targetLostTime;

        // 攻撃
        private float _nextAttackTime;
        private bool _isCharging;
        private float _chargeStartTime;
        private float _currentCharge;

        // 移動
        private float _strafeDirection;
        private float _strafeEndTime;

        // P09弓オブジェクトのキャッシュ（PlayerControllerと同様）
        private GameObject? _p09BowObject;
        private ParentConstraint? _bowParentConstraint;
        private Animator? _humanoidAnimator;

        // Animatorパラメータ（P09 Riderに合わせる）
        private static readonly int SpeedParam = Animator.StringToHash("Speed");
        private static readonly int ShootParam = Animator.StringToHash("Shoot");
        private static readonly int ChargeParam = Animator.StringToHash("Charge");
        private static readonly int ChargeAmountParam = Animator.StringToHash("ChargeAmount");
        private static readonly int HitParam = Animator.StringToHash("Hit");
        private static readonly int DeathParam = Animator.StringToHash("Death");
        private static readonly int IsMountedParam = Animator.StringToHash("IsMounted");
        private static readonly int IsAimingParam = Animator.StringToHash("IsAiming");

        #endregion

        #region Properties

        /// <summary>
        /// AIのユニークID
        /// </summary>
        public ulong AIId => _aiId;

        /// <summary>
        /// チームインデックス
        /// </summary>
        public int TeamIndex => _teamIndex;

        /// <summary>
        /// 現在の状態
        /// </summary>
        public AIState CurrentState => _currentState;

        /// <summary>
        /// 生存しているかどうか
        /// </summary>
        public bool IsAlive => _isAlive;

        /// <summary>
        /// 現在のターゲット
        /// </summary>
        public GameObject? CurrentTarget => _currentTarget;

        /// <summary>
        /// 現在のゲームモード行動設定
        /// </summary>
        public IModeBehavior? ModeBehavior => _modeBehavior;

        #endregion

        #region Unity Lifecycle

        private void Awake()
        {
            _animator = GetComponent<Animator>();
            _navAgent = GetComponent<NavMeshAgent>();

            // BlazeAIコンポーネントを取得（任意、リフレクションで型を検索）
            TryGetBlazeAI();
        }

        private void Start()
        {
            // AudioServiceを取得
            _audioService = ServiceLocator.Instance.Get<IAudioService>();
        }

        /// <summary>
        /// BlazeAIコンポーネントを安全に取得します
        /// </summary>
        private void TryGetBlazeAI()
        {
            // BlazeAI型をリフレクションで検索
            var blazeAIType = System.Type.GetType("BlazeAI, Assembly-CSharp");
            if (blazeAIType != null)
            {
                _blazeAI = GetComponent(blazeAIType) as MonoBehaviour;
            }
        }

        /// <summary>
        /// マウントからMAnimalBrainを取得して移動制御を委譲するかどうかを判定します
        /// </summary>
        private void TryGetMAnimalBrain()
        {
            if (_mountObject == null)
            {
                return;
            }

            // MAnimalBrain型をリフレクションで検索
            var brainType = System.Type.GetType("MalbersAnimations.Controller.AI.MAnimalBrain, Assembly-CSharp");
            if (brainType != null)
            {
                _mAnimalBrain = _mountObject.GetComponentInChildren(brainType) as MonoBehaviour;
                if (_mAnimalBrain != null)
                {
                    _useMAnimalBrainForMovement = true;
                    Debug.Log($"[AIPlayerController] AI {_aiId}: MAnimalBrain detected on mount, delegating movement control");
                }
            }
        }

        private void Update()
        {
            if (!_isEnabled || !_isAlive)
            {
                return;
            }

            UpdateStateMachine();
        }

        private void LateUpdate()
        {
            // 弓を常に左手に固定（isEnabledに関わらず実行）
            ForceBowToLeftHand(_isCharging);

            if (!_isEnabled || !_isAlive)
            {
                return;
            }

            // エイム中（攻撃状態）は上半身をターゲット方向に回転
            if (_currentState == AIState.Attack && _currentTarget != null)
            {
                RotateSpineTowardTarget();
            }
            else
            {
                // 非エイム時は徐々にリセット
                ResetSpineRotation();
            }
        }

        /// <summary>
        /// 上半身をターゲット方向に回転させます（PlayerControllerと同様の処理）
        /// </summary>
        /// <remarks>
        /// RiderArcherControllerと同じパターンで上半身を回転させます。
        /// 回転は制限内に収め、ターゲットが後方にいる場合は回転しません。
        /// </remarks>
        private void RotateSpineTowardTarget()
        {
            if (_spineTransform == null || _currentTarget == null || _mountObject == null)
            {
                return;
            }

            // ターゲットへの方向を計算
            Vector3 targetPos = _currentTarget.transform.position;
            Vector3 myPos = transform.position;
            Vector3 direction = targetPos - myPos;

            // 水平方向のみ
            Vector3 horizontalDir = direction;
            horizontalDir.y = 0;

            if (horizontalDir.sqrMagnitude < 0.001f)
            {
                return;
            }

            horizontalDir.Normalize();

            // 馬の向きを取得
            Vector3 mountForward = _mountObject.transform.forward;
            mountForward.y = 0;
            mountForward.Normalize();

            // 馬の向きとターゲットの向きの角度差を計算
            float horizontalAngle = Vector3.SignedAngle(mountForward, horizontalDir, Vector3.up);

            // 角度を制限内に収める（後方のターゲットには回転しない）
            horizontalAngle = Mathf.Clamp(horizontalAngle, -MaxHorizontalRotation, MaxHorizontalRotation);

            // 垂直角度を計算（高低差）
            float distance = new Vector3(direction.x, 0, direction.z).magnitude;
            float heightDiff = targetPos.y - myPos.y;
            float verticalAngle = -Mathf.Atan2(heightDiff, distance) * Mathf.Rad2Deg;
            verticalAngle = Mathf.Clamp(verticalAngle, -MaxVerticalRotation, MaxVerticalRotation);

            // RiderArcherControllerと同じ乗数を使用（部分的な回転）
            float targetAngleY = horizontalAngle * 0.5f;
            float targetAngleX = verticalAngle * 0.3f;

            // スムーズに目標角度に近づける
            _currentSpineYRotation = Mathf.Lerp(_currentSpineYRotation, targetAngleY, _aimRotationSpeed * Time.deltaTime);
            _currentSpineXRotation = Mathf.Lerp(_currentSpineXRotation, targetAngleX, _aimRotationSpeed * Time.deltaTime);

            // アニメーションの回転に追加の回転を乗せる（localRotationを使用）
            Quaternion horizontalRot = Quaternion.AngleAxis(_currentSpineYRotation, Vector3.up);
            Quaternion verticalRot = Quaternion.AngleAxis(_currentSpineXRotation, Vector3.right);
            Quaternion additionalRotation = horizontalRot * verticalRot;

            // ローカル回転として適用（RiderArcherControllerと同様）
            _spineTransform.localRotation = _spineTransform.localRotation * additionalRotation;
        }

        /// <summary>
        /// 上半身の回転を徐々にリセットします
        /// </summary>
        private void ResetSpineRotation()
        {
            if (_spineTransform == null)
            {
                return;
            }

            // 徐々に0に戻す
            _currentSpineYRotation = Mathf.Lerp(_currentSpineYRotation, 0f, _aimRotationSpeed * 2f * Time.deltaTime);
            _currentSpineXRotation = Mathf.Lerp(_currentSpineXRotation, 0f, _aimRotationSpeed * 2f * Time.deltaTime);

            // まだ角度がある場合は適用
            if (Mathf.Abs(_currentSpineYRotation) > 0.5f || Mathf.Abs(_currentSpineXRotation) > 0.5f)
            {
                Quaternion horizontalRot = Quaternion.AngleAxis(_currentSpineYRotation, Vector3.up);
                Quaternion verticalRot = Quaternion.AngleAxis(_currentSpineXRotation, Vector3.right);
                Quaternion additionalRotation = horizontalRot * verticalRot;
                _spineTransform.localRotation = _spineTransform.localRotation * additionalRotation;
            }
            else
            {
                _currentSpineYRotation = 0f;
                _currentSpineXRotation = 0f;
            }
        }

        #endregion

        #region Initialization

        /// <summary>
        /// AIコントローラーを初期化します
        /// </summary>
        public void Initialize(ulong aiId, int teamIndex, GameMode gameMode,
            DifficultySettings difficultySettings, GameObject mount, AICombatService combatService)
        {
            _aiId = aiId;
            _teamIndex = teamIndex;
            _gameMode = gameMode;
            _difficultySettings = difficultySettings;
            _mountObject = mount;
            _combatService = combatService;

            _currentHealth = _maxHealth;
            _isAlive = true;

            // ゲームモード行動設定を取得
            InitializeGameModeBehavior();

            // NavMeshAgentは使用しない（馬のMAnimalBrainが移動を制御）
            // ライダー上のNavMeshAgentは無効化
            if (_navAgent != null)
            {
                _navAgent.enabled = false;
            }

            // MAnimal/MAnimalAIControlを取得（馬の移動制御用）
            TryGetMAnimalComponents();

            // MAnimalBrainを検出（マウントに存在する場合、移動を委譲）
            TryGetMAnimalBrain();

            // BlazeAIの設定（存在する場合）
            ConfigureBlazeAI();

            // Spineボーンを取得（エイム時の上半身回転用）
            InitializeSpineTransform();

            // P09のAnimator（AnimatorController付き）を使用
            // AIRiderのルートAnimatorではなく、P09モデルのAnimatorを使用する必要がある
            if (_humanoidAnimator != null)
            {
                _animator = _humanoidAnimator;
                Debug.Log($"[AIPlayerController] AI {_aiId}: Using P09 humanoid animator: {_humanoidAnimator.gameObject.name}");
            }

            // 矢プレハブを自動設定（未設定の場合）
            InitializeArrowPrefab();

            // 騎乗状態に設定
            _animator?.SetBool(IsMountedParam, true);

            // 弓を手に配置（遅延実行でカスタマイズが適用されるのを待つ）
            StartCoroutine(SetupBowToHandDelayed());

            // 初期化状態をログ出力
            LogInitializationStatus();

            Debug.Log($"[AIPlayerController] Initialized. ID: {aiId}, Team: {teamIndex}, Mode: {gameMode}");
        }

        /// <summary>
        /// 馬のMAnimalコンポーネントを取得します
        /// </summary>
        private void TryGetMAnimalComponents()
        {
            Debug.Log($"[AIPlayerController] AI {_aiId}: TryGetMAnimalComponents called, mount={(_mountObject != null ? _mountObject.name : "NULL")}");

            if (_mountObject == null)
            {
                Debug.LogError($"[AIPlayerController] AI {_aiId}: Mount object is NULL!");
                return;
            }

            // MAnimalを検索
            _mAnimal = _mountObject.GetComponentInChildren<MalbersAnimations.Controller.MAnimal>(true);
            if (_mAnimal != null)
            {
                Debug.Log($"[AIPlayerController] AI {_aiId}: MAnimal FOUND on {_mAnimal.gameObject.name}");
            }
            else
            {
                Debug.LogWarning($"[AIPlayerController] AI {_aiId}: MAnimal NOT FOUND on mount!");
                // 全ての子オブジェクトを列挙してデバッグ
                var allComponents = _mountObject.GetComponentsInChildren<MonoBehaviour>(true);
                Debug.Log($"[AIPlayerController] AI {_aiId}: Mount has {allComponents.Length} MonoBehaviours");
                foreach (var comp in allComponents)
                {
                    if (comp != null && comp.GetType().Name.Contains("Animal"))
                    {
                        Debug.Log($"[AIPlayerController] AI {_aiId}: Found Animal-related component: {comp.GetType().FullName} on {comp.gameObject.name}");
                    }
                }
            }

            // MAnimalAIControlを検索
            _mAnimalAIControl = _mountObject.GetComponentInChildren<MalbersAnimations.Controller.AI.MAnimalAIControl>(true);
            if (_mAnimalAIControl != null)
            {
                Debug.Log($"[AIPlayerController] AI {_aiId}: MAnimalAIControl FOUND on {_mAnimalAIControl.gameObject.name}");
            }
            else
            {
                Debug.LogWarning($"[AIPlayerController] AI {_aiId}: MAnimalAIControl NOT FOUND on mount!");
                // AIControl関連のコンポーネントを列挙
                var allComponents = _mountObject.GetComponentsInChildren<MonoBehaviour>(true);
                foreach (var comp in allComponents)
                {
                    if (comp != null && (comp.GetType().Name.Contains("AIControl") || comp.GetType().Name.Contains("Brain")))
                    {
                        Debug.Log($"[AIPlayerController] AI {_aiId}: Found AI-related component: {comp.GetType().FullName} on {comp.gameObject.name}");
                    }
                }
            }

            // NavMeshAgentの状態をチェック
            var navAgent = _mountObject.GetComponentInChildren<NavMeshAgent>(true);
            if (navAgent != null)
            {
                Debug.Log($"[AIPlayerController] AI {_aiId}: NavMeshAgent found on {navAgent.gameObject.name}, enabled={navAgent.enabled}, isOnNavMesh={navAgent.isOnNavMesh}");
                if (!navAgent.isOnNavMesh)
                {
                    // NavMesh上にない場合、最寄りのNavMesh位置にワープ
                    Debug.LogWarning($"[AIPlayerController] AI {_aiId}: NavMeshAgent is NOT on NavMesh! Attempting to warp to nearest NavMesh position...");
                    WarpToNavMesh(navAgent);
                }
            }
            else
            {
                Debug.LogWarning($"[AIPlayerController] AI {_aiId}: NavMeshAgent NOT FOUND on mount!");
            }
        }

        /// <summary>
        /// NavMeshAgentを最寄りのNavMesh位置にワープします
        /// </summary>
        private void WarpToNavMesh(NavMeshAgent agent)
        {
            // 現在位置から最寄りのNavMesh位置を検索
            Vector3 currentPos = agent.transform.position;
            float searchRadius = 50f; // 検索範囲を拡大

            // エージェントのAgent Typeを確認
            Debug.Log($"[AIPlayerController] AI {_aiId}: NavMeshAgent agentTypeID={agent.agentTypeID}, position={currentPos}");

            // NavMesh全体の状態を確認
            var triangulation = NavMesh.CalculateTriangulation();
            Debug.Log($"[AIPlayerController] AI {_aiId}: NavMesh has {triangulation.vertices.Length} vertices, {triangulation.indices.Length / 3} triangles");

            if (triangulation.vertices.Length == 0)
            {
                Debug.LogError($"[AIPlayerController] AI {_aiId}: NavMesh is EMPTY! No walkable area exists.");
                return;
            }

            // エージェントのAgent Typeに対応するNavMeshを検索
            int agentTypeArea = 1 << NavMesh.GetAreaFromName("Walkable");
            if (NavMesh.SamplePosition(currentPos, out NavMeshHit hit, searchRadius, NavMesh.AllAreas))
            {
                Debug.Log($"[AIPlayerController] AI {_aiId}: Found NavMesh position at {hit.position}, mask={hit.mask}, distance={hit.distance:F2}m");

                // エージェントを一度無効化してワープ
                bool wasEnabled = agent.enabled;
                agent.enabled = false;

                // 位置をワープ（transformとWarp両方）
                agent.transform.position = hit.position;

                // 再有効化してからWarp
                agent.enabled = true;
                agent.Warp(hit.position);

                Debug.Log($"[AIPlayerController] AI {_aiId}: After warp - position={agent.transform.position}, isOnNavMesh={agent.isOnNavMesh}");

                // ワープ後の状態を確認
                if (agent.isOnNavMesh)
                {
                    Debug.Log($"[AIPlayerController] AI {_aiId}: NavMeshAgent is now ON NavMesh!");
                }
                else
                {
                    // Agent Typeが一致しない可能性
                    Debug.LogError($"[AIPlayerController] AI {_aiId}: Warp failed - still NOT on NavMesh! Agent Type mismatch? NavMeshAgent.agentTypeID={agent.agentTypeID}. Check NavMeshSurface Agent Type setting.");
                }
            }
            else
            {
                Debug.LogError($"[AIPlayerController] AI {_aiId}: Could not find NavMesh position within {searchRadius}m of {currentPos}. NavMesh may not cover this area.");
            }
        }

        /// <summary>
        /// SpineボーンとHeadボーンを取得します（エイム時の上半身回転用）
        /// </summary>
        /// <remarks>
        /// P09モデルはAIRiderの子オブジェクトにあるため、
        /// ルートのAnimatorではなく、P09モデルのAnimatorを検索します。
        /// </remarks>
        private void InitializeSpineTransform()
        {
            // P09モデルのAnimatorを探す（Humanoid Avatarを持つもの）
            Animator? humanoidAnimator = FindHumanoidAnimator();
            if (humanoidAnimator == null)
            {
                Debug.LogWarning($"[AIPlayerController] AI {_aiId}: Humanoid Animator not found");
                return;
            }

            // Humanoid AnimatorからSpineボーンとHeadボーンを取得
            try
            {
                _spineTransform = humanoidAnimator.GetBoneTransform(HumanBodyBones.Spine);
                if (_spineTransform == null)
                {
                    _spineTransform = humanoidAnimator.GetBoneTransform(HumanBodyBones.Chest);
                }

                _headTransform = humanoidAnimator.GetBoneTransform(HumanBodyBones.Head);

                if (_spineTransform != null)
                {
                    Debug.Log($"[AIPlayerController] AI {_aiId}: Spine transform found: {_spineTransform.name}");
                }
                if (_headTransform != null)
                {
                    Debug.Log($"[AIPlayerController] AI {_aiId}: Head transform found: {_headTransform.name}");
                }
            }
            catch (System.InvalidOperationException)
            {
                Debug.LogWarning($"[AIPlayerController] AI {_aiId}: Avatar is null, cannot get bone transforms");
            }
        }

        /// <summary>
        /// Humanoid Avatarを持つAnimatorを検索します
        /// </summary>
        /// <returns>Humanoid Animator（見つからない場合はnull）</returns>
        private Animator? FindHumanoidAnimator()
        {
            // 自身のAnimatorをチェック
            if (_animator != null && _animator.avatar != null && _animator.avatar.isHuman)
            {
                return _animator;
            }

            // 子オブジェクトからHumanoid Animatorを検索
            var animators = GetComponentsInChildren<Animator>(true);
            foreach (var anim in animators)
            {
                if (anim.avatar != null && anim.avatar.isHuman)
                {
                    return anim;
                }
            }

            return null;
        }

        /// <summary>
        /// BlazeAIコンポーネントを設定します（リフレクション使用）
        /// </summary>
        private void ConfigureBlazeAI()
        {
            if (_blazeAI == null)
            {
                return;
            }

            // BlazeAIの設定はリフレクションで行う（型安全性のため）
            // BlazeAIがプレハブに設定されている場合、Inspectorから設定を行う
            Debug.Log($"[AIPlayerController] BlazeAI detected on AI {_aiId}, using BlazeAI for behavior control");
        }

        /// <summary>
        /// 矢プレハブを自動設定します（未設定の場合）
        /// </summary>
        /// <remarks>
        /// ArrowTypeConfigから矢プレハブを取得します（PlayerControllerと同じ方式）。
        /// </remarks>
        private void InitializeArrowPrefab()
        {
            // 既に設定されている場合はスキップ
            if (_arrowPrefab != null)
            {
                return;
            }

            // ArrowTypeConfigから取得（PlayerControllerと同じ方式）
            var arrowTypeConfig = Resources.Load<CavalryFight.Services.Customization.ArrowTypeConfig>("Settings/ArrowTypeConfig");
            if (arrowTypeConfig != null)
            {
                // デフォルトの矢タイプ（Arrow = 0）のプレハブを取得
                _arrowPrefab = arrowTypeConfig.GetArrowPrefab(CavalryFight.Services.Customization.ArrowType.Arrow);
                if (_arrowPrefab != null)
                {
                    Debug.Log($"[AIPlayerController] AI {_aiId}: Arrow prefab loaded from ArrowTypeConfig: {_arrowPrefab.name}");
                }
                else
                {
                    Debug.LogWarning($"[AIPlayerController] AI {_aiId}: Arrow prefab not set in ArrowTypeConfig for ArrowType.Arrow!");
                }
            }
            else
            {
                Debug.LogWarning($"[AIPlayerController] AI {_aiId}: ArrowTypeConfig not found at Resources/Settings/ArrowTypeConfig!");
            }
        }

        /// <summary>
        /// ゲームモード行動設定を初期化します
        /// </summary>
        /// <remarks>
        /// AIServiceConfigからAIGameModeBehaviorを取得し、
        /// 現在のゲームモードに応じた行動設定を取得します。
        /// </remarks>
        private void InitializeGameModeBehavior()
        {
            // AIServiceConfigから取得
            var aiServiceConfig = Resources.Load<AIServiceConfig>("Settings/AIServiceConfig");
            if (aiServiceConfig != null && aiServiceConfig.GameModeBehavior != null)
            {
                _gameModeBehaviorConfig = aiServiceConfig.GameModeBehavior;
                _modeBehavior = _gameModeBehaviorConfig.GetBehavior(_gameMode);
                Debug.Log($"[AIPlayerController] AI {_aiId}: GameModeBehavior loaded for mode {_gameMode}");
                Debug.Log($"[AIPlayerController] AI {_aiId}: TargetPriority={_modeBehavior.TargetPriority}, Aggression={_modeBehavior.AggressionLevel:F2}, CoordinateWithTeam={_modeBehavior.CoordinateWithTeam}");
            }
            else
            {
                Debug.LogWarning($"[AIPlayerController] AI {_aiId}: AIServiceConfig or GameModeBehavior not found, using default behavior");
            }
        }

        /// <summary>
        /// 初期化状態をログ出力します
        /// </summary>
        private void LogInitializationStatus()
        {
            Debug.Log($"[AIPlayerController] AI {_aiId} Setup Status:");
            Debug.Log($"  - Mount: {(_mountObject != null ? _mountObject.name : "NULL")}");
            Debug.Log($"  - MAnimal: {(_mAnimal != null ? "Found" : "NOT FOUND")}");
            Debug.Log($"  - MAnimalAIControl: {(_mAnimalAIControl != null ? "Found" : "NOT FOUND")}");
            Debug.Log($"  - Spine Transform: {(_spineTransform != null ? _spineTransform.name : "NOT FOUND")}");
            Debug.Log($"  - Arrow Prefab: {(_arrowPrefab != null ? _arrowPrefab.name : "NOT SET")}");
            Debug.Log($"  - Bow Fire Point: {(_bowFirePoint != null ? _bowFirePoint.name : "NOT SET (will be auto-detected)")}");
            Debug.Log($"  - GameMode: {_gameMode}");
            Debug.Log($"  - ModeBehavior: {(_modeBehavior != null ? $"TargetPriority={_modeBehavior.TargetPriority}, Aggression={_modeBehavior.AggressionLevel:F2}" : "NOT SET")}");
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// AIを有効化します
        /// </summary>
        public void Enable()
        {
            _isEnabled = true;

            if (_blazeAI != null)
            {
                _blazeAI.enabled = true;
            }

            // NavMeshAgentは使用しない（馬のMAnimalBrainが移動を制御）
            // _navAgentは無効のまま

            // プレイヤーを自動的にターゲットとして探す
            // ターゲットが見つかった場合はChaseまたはAttack状態になる
            // 見つからなかった場合のみPatrol状態にする
            if (!FindAndSetPlayerTarget())
            {
                SetState(AIState.Patrol);
            }

            // MAnimalBrainにもターゲットを設定
            if (_useMAnimalBrainForMovement && _currentTarget != null)
            {
                SetMAnimalBrainTarget(_currentTarget);
            }
        }

        /// <summary>
        /// プレイヤーをターゲットとして設定します
        /// </summary>
        /// <returns>ターゲットが見つかった場合はtrue</returns>
        private bool FindAndSetPlayerTarget()
        {
            // 1. プレイヤータグでプレイヤーを探す
            GameObject? player = GameObject.FindGameObjectWithTag("Player");
            if (player != null && IsValidTarget(player))
            {
                Debug.Log($"[AIPlayerController] AI {_aiId} found target by Player tag: {player.name}");
                SetTarget(player);
                return true;
            }

            // 2. PlayerControllerコンポーネントでプレイヤーを探す
            var playerControllerType = System.Type.GetType("CavalryFight.Gameplay.Player.PlayerController, Assembly-CSharp");
            if (playerControllerType != null)
            {
                var playerController = UnityEngine.Object.FindFirstObjectByType(playerControllerType) as MonoBehaviour;
                if (playerController != null && IsValidTarget(playerController.gameObject))
                {
                    Debug.Log($"[AIPlayerController] AI {_aiId} found target by PlayerController: {playerController.gameObject.name}");
                    SetTarget(playerController.gameObject);
                    return true;
                }
            }

            // 3. RiderControllerコンポーネントでプレイヤーを探す
            var riderControllerType = System.Type.GetType("CavalryFight.Gameplay.Player.RiderController, Assembly-CSharp");
            if (riderControllerType != null)
            {
                var riderController = UnityEngine.Object.FindFirstObjectByType(riderControllerType) as MonoBehaviour;
                if (riderController != null && IsValidTarget(riderController.gameObject))
                {
                    Debug.Log($"[AIPlayerController] AI {_aiId} found target by RiderController: {riderController.gameObject.name}");
                    SetTarget(riderController.gameObject);
                    return true;
                }
            }

            // 4. MRiderコンポーネントで騎手を探す（プレイヤーもAIも含む）
            var allRiders = UnityEngine.Object.FindObjectsByType<MonoBehaviour>(FindObjectsSortMode.None);
            foreach (var rider in allRiders)
            {
                if (rider != null && rider.GetType().Name.Contains("MRider") && IsValidTarget(rider.gameObject))
                {
                    Debug.Log($"[AIPlayerController] AI {_aiId} found target by MRider: {rider.gameObject.name}");
                    SetTarget(rider.gameObject);
                    return true;
                }
            }

            Debug.LogWarning($"[AIPlayerController] AI {_aiId} could not find any valid target");
            return false;
        }

        /// <summary>
        /// ターゲットが有効かどうかを判定します
        /// </summary>
        private bool IsValidTarget(GameObject target)
        {
            if (target == null)
            {
                return false;
            }

            // 自分自身は除外
            if (target == gameObject || target.transform.IsChildOf(transform))
            {
                return false;
            }

            // マウント（馬）も除外
            if (_mountObject != null && (target == _mountObject || target.transform.IsChildOf(_mountObject.transform)))
            {
                return false;
            }

            // チームメイトは除外
            var otherAI = target.GetComponentInParent<AIPlayerController>();
            if (otherAI != null && otherAI.TeamIndex == _teamIndex)
            {
                return false;
            }

            return true;
        }

        /// <summary>
        /// AIを無効化します
        /// </summary>
        public void Disable()
        {
            _isEnabled = false;

            if (_blazeAI != null)
            {
                _blazeAI.enabled = false;
            }

            if (_navAgent != null)
            {
                _navAgent.isStopped = true;
            }

            SetState(AIState.Idle);
        }

        /// <summary>
        /// ターゲットを設定します
        /// </summary>
        public void SetTarget(GameObject target)
        {
            _currentTarget = target;

            if (target != null)
            {
                _lastKnownTargetPosition = target.transform.position;

                // BlazeAIにターゲットを設定（リフレクション使用）
                SetBlazeAIEnemy(target);

                // MAnimalBrainにターゲットを設定
                SetMAnimalBrainTarget(target);

                SetState(AIState.Chase);
            }
            else
            {
                SetMAnimalBrainTarget(null);
                SetState(AIState.Patrol);
            }
        }

        /// <summary>
        /// BlazeAIに敵を設定します（リフレクション使用）
        /// </summary>
        private void SetBlazeAIEnemy(GameObject target)
        {
            if (_blazeAI == null)
            {
                return;
            }

            var setEnemyMethod = _blazeAI.GetType().GetMethod("SetEnemy");
            if (setEnemyMethod != null)
            {
                setEnemyMethod.Invoke(_blazeAI, new object[] { target, true });
            }
        }

        /// <summary>
        /// 攻撃をトリガーします
        /// </summary>
        public void TriggerAttack()
        {
            if (!_isAlive || _currentTarget == null)
            {
                return;
            }

            SetState(AIState.Attack);
        }

        /// <summary>
        /// ダメージを受けます
        /// </summary>
        public void TakeDamage(int damage, GameObject? attacker)
        {
            if (!_isAlive)
            {
                return;
            }

            _currentHealth -= damage;

            // ヒットアニメーション
            _animator?.SetTrigger(HitParam);

            // BlazeAIにヒットを通知（リフレクション使用）
            NotifyBlazeAIHit(attacker);

            // 最後の攻撃者を記録（TargetPriority.Attacker用）
            if (attacker != null)
            {
                _lastAttacker = attacker;
            }

            // 攻撃者をターゲットに設定（ターゲット優先度がAttackerの場合、またはターゲットがいない場合）
            if (attacker != null)
            {
                bool shouldTargetAttacker = _currentTarget == null;
                if (_modeBehavior != null && _modeBehavior.TargetPriority == TargetPriority.Attacker)
                {
                    shouldTargetAttacker = true;
                }
                if (shouldTargetAttacker)
                {
                    SetTarget(attacker);
                }
            }

            // 死亡判定
            if (_currentHealth <= 0)
            {
                Die(attacker);
                return;
            }

            // 体力が低い場合、確率で逃走する
            float healthPercent = (float)_currentHealth / _maxHealth;

            // 撤退判定の閾値をゲームモードに応じて設定
            float retreatThreshold = 0.3f; // デフォルト: 体力30%以下
            if (_modeBehavior != null)
            {
                // デスマッチの場合は専用の閾値を使用
                if (_modeBehavior is DeathmatchBehavior deathmatch)
                {
                    retreatThreshold = deathmatch.RetreatHealthThreshold;
                }
                // リスポーンがないモードは慎重に
                if (!_modeBehavior.CanRespawn)
                {
                    retreatThreshold *= 1.5f; // 閾値を上げて早めに撤退
                }
            }

            if (healthPercent < retreatThreshold)
            {
                // 攻撃性が低いほど逃げやすい（攻撃性が高いAIは戦い続ける）
                float aggressionFactor = _modeBehavior?.AggressionLevel ?? 0.5f;
                float retreatChance = (1f - aggressionFactor) * (1f - _difficultySettings.AimAccuracy) * 0.8f;

                if (UnityEngine.Random.value < retreatChance)
                {
                    Debug.Log($"[AIPlayerController] AI {_aiId} health low ({healthPercent:P0}), retreating! (Aggression: {aggressionFactor:F2})");
                    _strafeEndTime = Time.time + 3f; // 3秒間逃走
                    SetState(AIState.Retreat);
                }
            }

            Debug.Log($"[AIPlayerController] AI {_aiId} took {damage} damage. Health: {_currentHealth}/{_maxHealth} ({healthPercent:P0})");
        }

        /// <summary>
        /// BlazeAIにヒットを通知します（リフレクション使用）
        /// </summary>
        private void NotifyBlazeAIHit(GameObject? attacker)
        {
            if (_blazeAI == null || attacker == null)
            {
                return;
            }

            var hitMethod = _blazeAI.GetType().GetMethod("Hit");
            if (hitMethod != null)
            {
                hitMethod.Invoke(_blazeAI, new object[] { attacker, true });
            }
        }

        /// <summary>
        /// 死亡します
        /// </summary>
        public void Die(GameObject? killer)
        {
            Die(killer, notifyService: true);
        }

        /// <summary>
        /// 死亡します（サービス通知オプション付き）
        /// </summary>
        /// <param name="killer">キルしたGameObject</param>
        /// <param name="notifyService">AICombatServiceに通知するかどうか（KillAIから呼ばれる場合はfalse）</param>
        internal void Die(GameObject? killer, bool notifyService)
        {
            if (!_isAlive)
            {
                return;
            }

            _isAlive = false;
            _isEnabled = false;

            SetState(AIState.Dead);

            // 死亡アニメーション
            _animator?.SetTrigger(DeathParam);

            // BlazeAIの死亡処理（リフレクション使用）
            NotifyBlazeAIDeath(killer);

            // NavMeshAgentを無効化
            if (_navAgent != null)
            {
                _navAgent.enabled = false;
            }

            Debug.Log($"[AIPlayerController] AI {_aiId} died");

            // AICombatServiceに通知（TakeDamageから呼ばれた場合のみ）
            // KillAIから呼ばれた場合は二重通知を避けるためスキップ
            if (notifyService && _combatService != null)
            {
                _combatService.NotifyAIDeath(_aiId, killer);
            }
        }

        /// <summary>
        /// BlazeAIに死亡を通知します（リフレクション使用）
        /// </summary>
        private void NotifyBlazeAIDeath(GameObject? killer)
        {
            if (_blazeAI == null)
            {
                return;
            }

            var deathMethod = _blazeAI.GetType().GetMethod("Death");
            if (deathMethod != null)
            {
                deathMethod.Invoke(_blazeAI, new object?[] { true, killer });
            }
        }

        #endregion

        #region State Machine

        /// <summary>
        /// 状態を設定します
        /// </summary>
        private void SetState(AIState newState)
        {
            if (_currentState == newState)
            {
                return;
            }

            AIState previousState = _currentState;
            _currentState = newState;

            OnStateExit(previousState);
            OnStateEnter(newState);

            Debug.Log($"[AIPlayerController] AI {_aiId} state: {previousState} -> {newState}");
        }

        /// <summary>
        /// 状態に入った時の処理
        /// </summary>
        private void OnStateEnter(AIState state)
        {
            switch (state)
            {
                case AIState.Patrol:
                    StartPatrol();
                    break;
                case AIState.Chase:
                    StartChase();
                    break;
                case AIState.Attack:
                    StartAttack();
                    break;
                case AIState.Strafe:
                    StartStrafe();
                    break;
                case AIState.Retreat:
                    StartRetreat();
                    break;
            }
        }

        /// <summary>
        /// 状態から出た時の処理
        /// </summary>
        private void OnStateExit(AIState state)
        {
            switch (state)
            {
                case AIState.Attack:
                    CancelCharge();
                    break;
            }
        }

        /// <summary>
        /// 状態マシンを更新します
        /// </summary>
        private void UpdateStateMachine()
        {
            // ターゲット検出
            UpdateTargetDetection();

            switch (_currentState)
            {
                case AIState.Idle:
                    UpdateIdle();
                    break;
                case AIState.Patrol:
                    UpdatePatrol();
                    break;
                case AIState.Chase:
                    UpdateChase();
                    break;
                case AIState.Attack:
                    UpdateAttack();
                    break;
                case AIState.Strafe:
                    UpdateStrafe();
                    break;
                case AIState.Retreat:
                    UpdateRetreat();
                    break;
            }

            // Animator更新
            UpdateAnimator();
        }

        #endregion

        #region Target Detection

        /// <summary>
        /// ターゲット検出を更新します
        /// </summary>
        private void UpdateTargetDetection()
        {
            // BlazeAIを使用している場合はBlazeAIに任せる
            if (_blazeAI != null && _blazeAI.enabled)
            {
                var enemy = GetBlazeAIEnemy();
                if (enemy != null)
                {
                    _currentTarget = enemy;
                    _lastKnownTargetPosition = _currentTarget.transform.position;
                }
                return;
            }

            // 自前でターゲット検出
            if (_currentTarget != null)
            {
                // ターゲットが視界内か確認
                if (CanSeeTarget(_currentTarget))
                {
                    _lastKnownTargetPosition = _currentTarget.transform.position;
                    _targetLostTime = 0f;
                }
                else
                {
                    _targetLostTime += Time.deltaTime;

                    // 一定時間ターゲットを見失ったら解除
                    if (_targetLostTime > 5f)
                    {
                        _currentTarget = null;
                        SetState(AIState.Patrol);
                    }
                }
            }
            else
            {
                // 新しいターゲットを探す
                GameObject? newTarget = FindNearestEnemy();
                if (newTarget != null)
                {
                    SetTarget(newTarget);
                }
            }
        }

        /// <summary>
        /// BlazeAIから敵を取得します（リフレクション使用）
        /// </summary>
        private GameObject? GetBlazeAIEnemy()
        {
            if (_blazeAI == null)
            {
                return null;
            }

            var enemyProperty = _blazeAI.GetType().GetProperty("enemyToAttack");
            if (enemyProperty != null)
            {
                return enemyProperty.GetValue(_blazeAI) as GameObject;
            }

            // プロパティがない場合はフィールドを試す
            var enemyField = _blazeAI.GetType().GetField("enemyToAttack");
            if (enemyField != null)
            {
                return enemyField.GetValue(_blazeAI) as GameObject;
            }

            return null;
        }

        /// <summary>
        /// ターゲットが見えるかどうか
        /// </summary>
        private bool CanSeeTarget(GameObject target)
        {
            if (target == null)
            {
                return false;
            }

            Vector3 directionToTarget = target.transform.position - transform.position;
            float distanceToTarget = directionToTarget.magnitude;

            // 距離チェック
            if (distanceToTarget > _difficultySettings.VisionRange)
            {
                return false;
            }

            // 角度チェック
            float angle = Vector3.Angle(transform.forward, directionToTarget);
            if (angle > _difficultySettings.VisionAngle / 2f)
            {
                return false;
            }

            // 視線チェック（障害物）
            Ray ray = new Ray(transform.position + Vector3.up, directionToTarget.normalized);
            if (Physics.Raycast(ray, distanceToTarget, _obstacleLayers))
            {
                return false;
            }

            return true;
        }

        /// <summary>
        /// ターゲット優先度に基づいて敵を探します
        /// </summary>
        private GameObject? FindNearestEnemy()
        {
            // ターゲット優先度を取得
            TargetPriority priority = _modeBehavior?.TargetPriority ?? TargetPriority.Nearest;

            // Attackerモードで最後の攻撃者がいる場合、それを優先
            if (priority == TargetPriority.Attacker && _lastAttacker != null && CanSeeTarget(_lastAttacker))
            {
                return _lastAttacker;
            }

            // _enemyLayersが設定されていない場合はすべてのレイヤーで検索
            int searchLayers = _enemyLayers.value != 0 ? _enemyLayers.value : ~0;

            Collider[] colliders = Physics.OverlapSphere(
                transform.position,
                _difficultySettings.VisionRange,
                searchLayers
            );

            // 有効なターゲットをリストアップ
            System.Collections.Generic.List<(GameObject target, float distance, int health)> validTargets = new();

            foreach (Collider col in colliders)
            {
                // 自分自身は除外
                if (col.gameObject == gameObject || col.transform.IsChildOf(transform))
                {
                    continue;
                }

                // 馬（マウント）も除外
                if (_mountObject != null && (col.gameObject == _mountObject || col.transform.IsChildOf(_mountObject.transform)))
                {
                    continue;
                }

                // チームメイトは除外
                var otherAI = col.GetComponentInParent<AIPlayerController>();
                if (otherAI != null && otherAI.TeamIndex == _teamIndex)
                {
                    continue;
                }

                // プレイヤーまたは他のAIの騎手/馬かどうかを確認
                bool isValidTarget = false;
                int targetHealth = 100; // デフォルト体力

                // Playerタグをチェック
                if (col.CompareTag("Player"))
                {
                    isValidTarget = true;
                }

                // MRider（騎手）コンポーネントをチェック
                var riderComponents = col.GetComponentsInParent<MonoBehaviour>();
                foreach (var comp in riderComponents)
                {
                    if (comp != null && comp.GetType().Name.Contains("MRider"))
                    {
                        isValidTarget = true;
                        break;
                    }
                }

                // MAnimal（馬）コンポーネントをチェック
                var animalComponents = col.GetComponentsInParent<MonoBehaviour>();
                foreach (var comp in animalComponents)
                {
                    if (comp != null && comp.GetType().Name.Contains("MAnimal"))
                    {
                        isValidTarget = true;
                        break;
                    }
                }

                // 他のAIの場合、体力を取得
                if (otherAI != null)
                {
                    targetHealth = otherAI._currentHealth;
                }

                if (!isValidTarget)
                {
                    continue;
                }

                if (!CanSeeTarget(col.gameObject))
                {
                    continue;
                }

                float distance = Vector3.Distance(transform.position, col.transform.position);
                validTargets.Add((col.gameObject, distance, targetHealth));
            }

            if (validTargets.Count == 0)
            {
                return null;
            }

            // ターゲット優先度に基づいてソート・選択
            return priority switch
            {
                TargetPriority.Nearest => SelectNearestTarget(validTargets),
                TargetPriority.HighestScore => SelectNearestTarget(validTargets), // スコアシステムがないのでNearestにフォールバック
                TargetPriority.Weakest => SelectWeakestTarget(validTargets),
                TargetPriority.Attacker => SelectNearestTarget(validTargets), // 攻撃者がいない場合はNearestにフォールバック
                TargetPriority.Random => SelectRandomTarget(validTargets),
                _ => SelectNearestTarget(validTargets)
            };
        }

        /// <summary>
        /// 最も近いターゲットを選択します
        /// </summary>
        private GameObject? SelectNearestTarget(System.Collections.Generic.List<(GameObject target, float distance, int health)> targets)
        {
            if (targets.Count == 0) return null;

            GameObject? nearest = null;
            float nearestDistance = float.MaxValue;

            foreach (var (target, distance, _) in targets)
            {
                if (distance < nearestDistance)
                {
                    nearestDistance = distance;
                    nearest = target;
                }
            }

            return nearest;
        }

        /// <summary>
        /// 最も弱い（体力が低い）ターゲットを選択します
        /// </summary>
        private GameObject? SelectWeakestTarget(System.Collections.Generic.List<(GameObject target, float distance, int health)> targets)
        {
            if (targets.Count == 0) return null;

            GameObject? weakest = null;
            int lowestHealth = int.MaxValue;

            foreach (var (target, _, health) in targets)
            {
                if (health < lowestHealth)
                {
                    lowestHealth = health;
                    weakest = target;
                }
            }

            return weakest;
        }

        /// <summary>
        /// ランダムにターゲットを選択します
        /// </summary>
        private GameObject? SelectRandomTarget(System.Collections.Generic.List<(GameObject target, float distance, int health)> targets)
        {
            if (targets.Count == 0) return null;

            int index = UnityEngine.Random.Range(0, targets.Count);
            return targets[index].target;
        }

        #endregion

        #region State Behaviors

        private void UpdateIdle()
        {
            // 待機中は何もしない
        }

        private void StartPatrol()
        {
            // 馬のMAnimalBrainに巡回を任せる（AIはターゲットをクリア）
            SetMAnimalBrainTarget(null);
        }

        private void UpdatePatrol()
        {
            // 馬のMAnimalBrainが巡回を処理
            // ターゲット検出は別途行われる
        }

        private void StartChase()
        {
            // MAnimalAIControlにターゲットを設定（馬が追跡）
            SetMAnimalBrainTarget(_currentTarget);
        }

        private void UpdateChase()
        {
            if (_currentTarget == null)
            {
                SetState(AIState.Patrol);
                return;
            }

            float distanceToTarget = Vector3.Distance(transform.position, _currentTarget.transform.position);

            // 攻撃範囲内なら攻撃に遷移
            if (distanceToTarget <= _attackRange && CanSeeTarget(_currentTarget))
            {
                SetState(AIState.Attack);
                return;
            }

            // ターゲットに向かって移動（MAnimalAIControlが処理）
            SetMAnimalBrainTarget(_currentTarget);
        }

        private void StartAttack()
        {
            _nextAttackTime = Time.time + _difficultySettings.ReactionTime;
        }

        private void UpdateAttack()
        {
            if (_currentTarget == null)
            {
                SetState(AIState.Patrol);
                return;
            }

            float distanceToTarget = Vector3.Distance(transform.position, _currentTarget.transform.position);

            // 攻撃範囲外なら追跡に戻る
            if (distanceToTarget > _attackRange)
            {
                SetState(AIState.Chase);
                return;
            }

            // ターゲットの方を向く
            LookAtTarget();

            // 攻撃タイミング
            if (Time.time >= _nextAttackTime)
            {
                if (!_isCharging)
                {
                    StartCharge();
                }
                else
                {
                    UpdateCharge();
                }
            }

            // ストレイフ判定（攻撃性が低いほどストレイフしやすい）
            float aggressionFactor = _modeBehavior?.AggressionLevel ?? 0.5f;
            float adjustedStrafeChance = _difficultySettings.StrafeChance * (1f - aggressionFactor * 0.5f);
            if (!_isCharging && UnityEngine.Random.value < adjustedStrafeChance * Time.deltaTime)
            {
                SetState(AIState.Strafe);
            }
        }

        private void StartStrafe()
        {
            _strafeDirection = UnityEngine.Random.value > 0.5f ? 1f : -1f;
            _strafeEndTime = Time.time + UnityEngine.Random.Range(1f, 3f);

            // 馬のMAnimalBrainが移動を制御するため、横移動の指示は行わない
            // ライダーは上半身のエイムのみを行う
        }

        private void UpdateStrafe()
        {
            if (_currentTarget == null || Time.time > _strafeEndTime)
            {
                SetState(AIState.Attack);
                return;
            }

            // 馬のMAnimalBrainがターゲットへの移動を処理
            // ライダーは上半身でターゲットを狙う（LateUpdateで処理）
            LookAtTarget();
        }

        private void StartRetreat()
        {
            // MAnimalBrainにターゲットをクリア（逃走/巡回モードに入る）
            SetMAnimalBrainTarget(null);
            _strafeEndTime = Time.time + UnityEngine.Random.Range(2f, 4f);
            // 馬のMAnimalBrainが逃走/巡回を処理
        }

        private void UpdateRetreat()
        {
            // 一定時間後に攻撃状態に戻る
            if (Time.time > _strafeEndTime)
            {
                SetState(AIState.Attack);
            }
            // 馬のMAnimalBrainが移動を処理
        }

        /// <summary>
        /// MAnimalAIControlにターゲットを設定します
        /// </summary>
        /// <remarks>
        /// MAnimalAIControlを使用して馬にターゲットを設定します。
        /// これにより馬がターゲットに向かって移動/追跡します。
        /// </remarks>
        private void SetMAnimalBrainTarget(GameObject? target)
        {
            // MAnimalAIControlを使用
            if (_mAnimalAIControl != null)
            {
                try
                {
                    if (target != null)
                    {
                        // MAnimalAIControl.SetTarget(Transform) を呼び出す
                        _mAnimalAIControl.SetTarget(target.transform);
                        Debug.Log($"[AIPlayerController] AI {_aiId}: SetTarget called on MAnimalAIControl, target={target.name}");
                    }
                    else
                    {
                        // MAnimalAIControl.Stop() または ClearTarget() を呼び出す
                        _mAnimalAIControl.Stop();
                        Debug.Log($"[AIPlayerController] AI {_aiId}: Stop called on MAnimalAIControl");
                    }
                    return;
                }
                catch (System.Exception e)
                {
                    Debug.LogWarning($"[AIPlayerController] AI {_aiId}: MAnimalAIControl API error: {e.Message}. Falling back to reflection.");
                }
            }
            else
            {
                Debug.LogWarning($"[AIPlayerController] AI {_aiId}: MAnimalAIControl is NULL, cannot set target!");
            }

            // フォールバック: リフレクションでMAnimalBrainを使用
            if (_mAnimalBrain == null)
            {
                Debug.LogWarning($"[AIPlayerController] AI {_aiId}: MAnimalBrain is also NULL, horse will not move!");
                return;
            }

            var setTargetMethod = _mAnimalBrain.GetType().GetMethod("SetTarget");
            if (setTargetMethod != null)
            {
                if (target != null)
                {
                    setTargetMethod.Invoke(_mAnimalBrain, new object[] { target.transform });
                    Debug.Log($"[AIPlayerController] AI {_aiId}: SetTarget called on MAnimalBrain via reflection, target={target.name}");
                }
                else
                {
                    setTargetMethod.Invoke(_mAnimalBrain, new object?[] { null });
                    Debug.Log($"[AIPlayerController] AI {_aiId}: SetTarget(null) called on MAnimalBrain via reflection");
                }
            }
            else
            {
                Debug.LogError($"[AIPlayerController] AI {_aiId}: MAnimalBrain.SetTarget method not found!");
            }
        }

        #endregion

        #region Combat

        /// <summary>
        /// チャージを開始します
        /// </summary>
        private void StartCharge()
        {
            _isCharging = true;
            _chargeStartTime = Time.time;
            _currentCharge = 0f;

            // チャージ時間は難易度に応じて変動
            float targetChargeTime = _maxChargeTime * _difficultySettings.ChargeTimeMultiplier;
        }

        /// <summary>
        /// チャージを更新します
        /// </summary>
        private void UpdateCharge()
        {
            float chargeTime = Time.time - _chargeStartTime;
            float targetChargeTime = _maxChargeTime * _difficultySettings.ChargeTimeMultiplier;

            _currentCharge = Mathf.Clamp01(chargeTime / targetChargeTime);

            _animator?.SetFloat(ChargeParam, _currentCharge);

            // チャージ完了で発射
            if (_currentCharge >= 1f)
            {
                FireArrow();
            }
        }

        /// <summary>
        /// チャージをキャンセルします
        /// </summary>
        private void CancelCharge()
        {
            _isCharging = false;
            _currentCharge = 0f;
            _animator?.SetFloat(ChargeParam, 0f);
        }

        /// <summary>
        /// 矢を発射します
        /// </summary>
        private void FireArrow()
        {
            if (_arrowPrefab == null || _bowFirePoint == null || _currentTarget == null)
            {
                CancelCharge();
                return;
            }

            // ミス判定
            bool isMiss = UnityEngine.Random.value < _difficultySettings.MissChance;

            // 発射方向を計算
            Vector3 targetPos = _currentTarget.transform.position + Vector3.up; // 胴体を狙う

            // 精度に応じてブレを追加
            if (!isMiss)
            {
                float accuracyOffset = (1f - _difficultySettings.AimAccuracy) * 3f;
                targetPos += UnityEngine.Random.insideUnitSphere * accuracyOffset;
            }
            else
            {
                // ミスの場合は大きくブレる
                targetPos += UnityEngine.Random.insideUnitSphere * 5f;
            }

            Vector3 direction = (targetPos - _bowFirePoint.position).normalized;

            // 矢の速度
            float arrowSpeed = Mathf.Lerp(_minArrowSpeed, _maxArrowSpeed, _currentCharge);
            Vector3 velocity = direction * arrowSpeed;

            // 矢のスポーン位置を前方にオフセット（馬との衝突を防ぐ）
            Vector3 spawnPosition = _bowFirePoint.position + direction * 0.5f;

            // 矢の親オブジェクトを作成（プレイヤーと同じ方式）
            GameObject arrowParent = new GameObject("AIArrow");
            arrowParent.transform.position = spawnPosition;
            arrowParent.transform.rotation = Quaternion.LookRotation(direction);

            // VFX矢プレハブを親の子としてインスタンス化
            GameObject arrowVisual = Instantiate(_arrowPrefab, arrowParent.transform);
            arrowVisual.transform.localPosition = Vector3.zero;
            arrowVisual.transform.localRotation = Quaternion.identity;
            arrowVisual.transform.localScale = Vector3.one * 0.1f; // 矢のスケール

            // VFXプレハブにRigidbodyがある場合は無効化（親で物理制御するため）
            Rigidbody? visualRb = arrowVisual.GetComponent<Rigidbody>();
            if (visualRb != null)
            {
                visualRb.isKinematic = true;
            }

            // ArrowProjectileコンポーネントを親に追加（リフレクション使用）
            var arrowProjectileType = System.Type.GetType("CavalryFight.Gameplay.Projectiles.ArrowProjectile, Assembly-CSharp");
            Component? arrowProjectile = null;
            if (arrowProjectileType != null)
            {
                arrowProjectile = arrowParent.AddComponent(arrowProjectileType);
            }

            // 発射者と馬を無視対象に設定（リフレクション使用）
            if (arrowProjectile != null)
            {
                var addIgnoredMethod = arrowProjectileType!.GetMethod("AddIgnoredObject");
                if (addIgnoredMethod != null)
                {
                    addIgnoredMethod.Invoke(arrowProjectile, new object[] { gameObject });
                    if (_mountObject != null)
                    {
                        addIgnoredMethod.Invoke(arrowProjectile, new object[] { _mountObject });
                    }
                    addIgnoredMethod.Invoke(arrowProjectile, new object[] { transform.root.gameObject });
                }

                // 速度とチャージ量を設定
                var setVelocityMethod = arrowProjectileType.GetMethod("SetVelocity");
                if (setVelocityMethod != null)
                {
                    setVelocityMethod.Invoke(arrowProjectile, new object[] { velocity });
                }

                var setChargeMethod = arrowProjectileType.GetMethod("SetChargeAmount");
                if (setChargeMethod != null)
                {
                    setChargeMethod.Invoke(arrowProjectile, new object[] { _currentCharge });
                }
            }

            // Rigidbodyを取得して設定（ArrowProjectileのRequireComponentで自動追加済み）
            Rigidbody? arrowRb = arrowParent.GetComponent<Rigidbody>();
            if (arrowRb == null)
            {
                arrowRb = arrowParent.AddComponent<Rigidbody>();
            }
            arrowRb.useGravity = true;
            arrowRb.linearVelocity = velocity;

            // コライダーを親に追加
            var collider = arrowParent.AddComponent<SphereCollider>();
            collider.radius = 0.15f;
            collider.isTrigger = false;

            // 高速移動でも衝突を検出できるようにContinuous Dynamicに設定
            arrowRb.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;

            // 発射者と馬のコライダーとの衝突を物理的に無視
            IgnoreCollisionWithOwner(collider);

            // アニメーション
            _animator?.SetTrigger(ShootParam);

            // 音
            if (_shootSfx != null)
            {
                _audioService?.PlaySfxAtPosition(_shootSfx, transform.position);
            }

            // サービスに通知
            _combatService?.NotifyAIFiredArrow(_aiId);

            // 次の攻撃時間を設定
            float attackInterval = UnityEngine.Random.Range(
                _difficultySettings.AttackInterval.x,
                _difficultySettings.AttackInterval.y
            );
            _nextAttackTime = Time.time + attackInterval;

            // チャージをリセット
            CancelCharge();

            Debug.Log($"[AIPlayerController] AI {_aiId} fired arrow. Miss: {isMiss}");
        }

        /// <summary>
        /// 発射者と馬のコライダーとの衝突を無視します
        /// </summary>
        private void IgnoreCollisionWithOwner(Collider arrowCollider)
        {
            // 自身のすべてのコライダーを取得して無視
            Collider[] myColliders = GetComponentsInChildren<Collider>();
            foreach (var col in myColliders)
            {
                Physics.IgnoreCollision(arrowCollider, col, true);
            }

            // マウント（馬）のすべてのコライダーを取得して無視
            if (_mountObject != null)
            {
                Collider[] mountColliders = _mountObject.GetComponentsInChildren<Collider>();
                foreach (var col in mountColliders)
                {
                    Physics.IgnoreCollision(arrowCollider, col, true);
                }
            }

            // ルートオブジェクトのすべてのコライダーも無視
            if (transform.root != transform && transform.root.gameObject != _mountObject)
            {
                Collider[] rootColliders = transform.root.GetComponentsInChildren<Collider>();
                foreach (var col in rootColliders)
                {
                    Physics.IgnoreCollision(arrowCollider, col, true);
                }
            }
        }

        /// <summary>
        /// ArrowProjectileを設定します（リフレクション使用）
        /// </summary>
        private void ConfigureArrowProjectile(GameObject arrowObj, Vector3 velocity)
        {
            // ArrowProjectile型をリフレクションで検索
            var arrowProjectileType = System.Type.GetType("CavalryFight.Gameplay.Projectiles.ArrowProjectile, Assembly-CSharp");
            if (arrowProjectileType == null)
            {
                // 型が見つからない場合はRigidbodyで直接速度を設定
                var rb = arrowObj.GetComponent<Rigidbody>();
                if (rb != null)
                {
                    rb.linearVelocity = velocity;
                }
                return;
            }

            var arrowProjectile = arrowObj.GetComponent(arrowProjectileType);
            if (arrowProjectile != null)
            {
                // SetVelocityメソッドを呼び出し
                var setVelocityMethod = arrowProjectileType.GetMethod("SetVelocity");
                if (setVelocityMethod != null)
                {
                    setVelocityMethod.Invoke(arrowProjectile, new object[] { velocity });
                }

                // SetChargeAmountメソッドを呼び出し
                var setChargeMethod = arrowProjectileType.GetMethod("SetChargeAmount");
                if (setChargeMethod != null)
                {
                    setChargeMethod.Invoke(arrowProjectile, new object[] { _currentCharge });
                }
            }
            else
            {
                // ArrowProjectileがない場合はRigidbodyで直接速度を設定
                var rb = arrowObj.GetComponent<Rigidbody>();
                if (rb != null)
                {
                    rb.linearVelocity = velocity;
                }
            }
        }

        /// <summary>
        /// ターゲットの方を向きます
        /// </summary>
        /// <remarks>
        /// 馬の向きはMAnimalBrainが制御するため、ライダーのtransformは回転させません。
        /// ライダーの上半身はLateUpdate()のRotateSpineTowardTarget()で別途回転させます。
        /// このメソッドは馬のMAnimalAIControlにターゲットを設定するだけです。
        /// </remarks>
        private void LookAtTarget()
        {
            if (_currentTarget == null)
            {
                return;
            }

            // MAnimalAIControlにターゲットを設定（馬がターゲット方向に移動）
            SetMAnimalBrainTarget(_currentTarget);

            // ライダーのtransformは回転させない
            // 上半身の回転はLateUpdate()のRotateSpineTowardTarget()で行う
        }

        #endregion

        #region Bow Handling

        /// <summary>
        /// 弓を手に配置する処理を遅延実行します
        /// </summary>
        /// <remarks>
        /// カスタマイズが適用されるのを待ってから弓を検索・配置します。
        /// PlayerControllerのSetupBowToHandDelayedと同様の処理です。
        /// </remarks>
        private IEnumerator SetupBowToHandDelayed()
        {
            // カスタマイズが適用されるのを待つ（最大2秒）
            float timeout = 2f;
            float elapsed = 0f;

            while (elapsed < timeout)
            {
                // Humanoid Animatorが見つかったら弓と髪をセットアップ
                if (_humanoidAnimator == null)
                {
                    _humanoidAnimator = FindHumanoidAnimator();
                }

                if (_humanoidAnimator != null)
                {
                    SetupBowToHand();
                    ReparentHairToHead();
                    yield break;
                }

                yield return new WaitForSeconds(0.1f);
                elapsed += 0.1f;
            }

            // タイムアウトしても試行
            SetupBowToHand();
            ReparentHairToHead();
        }

        /// <summary>
        /// 弓オブジェクトを左手に配置します
        /// </summary>
        /// <remarks>
        /// PlayerControllerのSetupBowToHandと同様の処理です。
        /// P09の弓構造:
        /// - 実際の弓メッシュ（例: Bow_1, Bow_2など）はParentConstraintを持ち、2つのTargetを参照
        /// - Weapon_Target_Hand_L: 手の位置ターゲット
        /// - Bow_Target_Back: 背中の位置ターゲット
        /// "Target"を含むオブジェクトは制約ターゲットなのでスキップします。
        /// </remarks>
        private void SetupBowToHand()
        {
            if (_humanoidAnimator == null)
            {
                _humanoidAnimator = FindHumanoidAnimator();
                if (_humanoidAnimator == null)
                {
                    Debug.LogWarning($"[AIPlayerController] AI {_aiId}: SetupBowToHand - Humanoid Animator not found");
                    return;
                }
            }

            // P09モデルのルート
            Transform riderTarget = _humanoidAnimator.transform;

            // 弓オブジェクトを検索 - ParentConstraintを持つものを優先、"Target"を含むものはスキップ
            GameObject? bowWithConstraint = null;
            GameObject? bowWithoutConstraint = null;
            GameObject? inactiveBow = null; // フォールバック用
            var allChildren = riderTarget.GetComponentsInChildren<Transform>(true);

            foreach (var child in allChildren)
            {
                // "Target"を含むものは制約ターゲットなのでスキップ
                if (child.name.Contains("Target"))
                {
                    continue;
                }

                if (child.name.Contains("Bow") && !child.name.Contains("Sword"))
                {
                    if (child.gameObject.activeSelf)
                    {
                        var pc = child.GetComponent<ParentConstraint>();
                        if (pc != null)
                        {
                            bowWithConstraint = child.gameObject;
                        }
                        else if (bowWithoutConstraint == null)
                        {
                            bowWithoutConstraint = child.gameObject;
                        }
                    }
                    else if (inactiveBow == null)
                    {
                        // 非アクティブな弓もフォールバックとして保存
                        // Bow_001, Bow_002 等のパターンを優先
                        if (System.Text.RegularExpressions.Regex.IsMatch(child.name, @"^Bow_\d{3}$"))
                        {
                            inactiveBow = child.gameObject;
                        }
                    }
                }
            }

            // ParentConstraintを持つ弓を優先、次に通常の弓、最後にフォールバック
            GameObject? bowObject = bowWithConstraint ?? bowWithoutConstraint ?? inactiveBow;

            // フォールバック弓を使う場合はアクティブ化
            if (bowObject == inactiveBow && bowObject != null)
            {
                bowObject.SetActive(true);
                Debug.Log($"[AIPlayerController] AI {_aiId}: Activated inactive bow: {bowObject.name}");
            }

            if (bowObject == null)
            {
                Debug.LogWarning($"[AIPlayerController] AI {_aiId}: SetupBowToHand - Bow object not found");
                return;
            }

            // キャッシュに保存
            _p09BowObject = bowObject;
            _bowParentConstraint = bowObject.GetComponent<ParentConstraint>();

            Debug.Log($"[AIPlayerController] AI {_aiId}: SetupBowToHand - Bow found: {bowObject.name}, ParentConstraint: {_bowParentConstraint != null}");

            // BowFirePointを自動設定（未設定の場合）
            if (_bowFirePoint == null)
            {
                // 弓から発射位置を探す（子オブジェクトにFirePointがあれば使用）
                Transform? firePoint = bowObject.transform.Find("FirePoint");
                if (firePoint == null)
                {
                    // "Nock"（矢をつがえる位置）を探す
                    firePoint = bowObject.transform.Find("Nock");
                }
                if (firePoint == null)
                {
                    // 子オブジェクトから"Fire"や"Nock"を含む名前を探す
                    foreach (Transform child in bowObject.GetComponentsInChildren<Transform>(true))
                    {
                        if (child.name.Contains("Fire") || child.name.Contains("Nock") || child.name.Contains("Arrow"))
                        {
                            firePoint = child;
                            break;
                        }
                    }
                }
                if (firePoint == null)
                {
                    // なければ弓の位置を使用
                    _bowFirePoint = bowObject.transform;
                }
                else
                {
                    _bowFirePoint = firePoint;
                }
                Debug.Log($"[AIPlayerController] AI {_aiId}: BowFirePoint auto-detected: {_bowFirePoint.name}");
            }

            // 弓が左手の下にない場合は移動
            Transform? leftHand = _humanoidAnimator.GetBoneTransform(HumanBodyBones.LeftHand);
            if (leftHand != null)
            {
                // 弓が既に左手の下にあるかチェック
                bool isUnderLeftHand = false;
                Transform? current = bowObject.transform.parent;
                while (current != null)
                {
                    if (current == leftHand)
                    {
                        isUnderLeftHand = true;
                        break;
                    }
                    current = current.parent;
                }

                if (!isUnderLeftHand)
                {
                    // ParentConstraintを無効化（これが弓の位置を上書きするのを防ぐ）
                    if (_bowParentConstraint != null)
                    {
                        _bowParentConstraint.constraintActive = false;
                    }

                    // 弓を左手の子として配置
                    bowObject.transform.SetParent(leftHand);
                    bowObject.transform.localPosition = Vector3.zero;
                    // 回転はForceBowToLeftHandで毎フレーム(0, 90, -90)に設定
                }
            }
        }

        /// <summary>
        /// P09の弓を手に配置し、アニメーションによる回転変化を補正します
        /// </summary>
        /// <param name="isAiming">エイム中かどうか</param>
        /// <remarks>
        /// PlayerControllerのForceBowToLeftHandと同様の処理です。
        /// P09の弓構造:
        /// - Bow: ParentConstraintで手/背中を切り替え、回転は(0,90,-90)に設定
        /// - Skeleton_P09_Bow: アニメーションで回転する骨格
        /// - Bow_003等: 実際のメッシュ
        ///
        /// アニメーションがSkeleton_P09_Bowを回転させるため、
        /// Bowオブジェクトに(0,90,-90)を設定して打ち消します。
        /// </remarks>
        private void ForceBowToLeftHand(bool isAiming)
        {
            // 初回のみ: 弓オブジェクトを検索してキャッシュ
            if (_p09BowObject == null)
            {
                if (_humanoidAnimator == null)
                {
                    _humanoidAnimator = FindHumanoidAnimator();
                    if (_humanoidAnimator == null)
                    {
                        return;
                    }
                }

                Transform riderTarget = _humanoidAnimator.transform;

                // Bowオブジェクトを検索（ParentConstraintを持つもの）
                var allChildren = riderTarget.GetComponentsInChildren<Transform>(true);
                foreach (var child in allChildren)
                {
                    if (child.name == "Bow" && child.GetComponent<ParentConstraint>() != null)
                    {
                        _p09BowObject = child.gameObject;
                        _bowParentConstraint = child.GetComponent<ParentConstraint>();
                        break;
                    }
                }

                if (_p09BowObject == null)
                {
                    return;
                }
            }

            // ParentConstraintの重みを手側に設定（Source 0 = Hand, Source 1 = Back）
            if (_bowParentConstraint != null && _bowParentConstraint.sourceCount >= 2)
            {
                var handSource = _bowParentConstraint.GetSource(0);
                var backSource = _bowParentConstraint.GetSource(1);

                // 手の重みを1、背中の重みを0に
                if (handSource.weight < 1f || backSource.weight > 0f)
                {
                    handSource.weight = 1f;
                    backSource.weight = 0f;
                    _bowParentConstraint.SetSource(0, handSource);
                    _bowParentConstraint.SetSource(1, backSource);
                }
            }

            // Bowの位置を(0,0,0)、回転を(0, 90, -90)に設定
            // これによりアニメーションによる位置/回転の変化を打ち消す
            if (_p09BowObject != null)
            {
                _p09BowObject.transform.localPosition = Vector3.zero;
                _p09BowObject.transform.localRotation = Quaternion.Euler(0f, 90f, -90f);
            }
        }

        /// <summary>
        /// 髪オブジェクトをHeadボーンの子に再配置します
        /// </summary>
        /// <remarks>
        /// PlayerControllerのReparentHairToHeadと同じシンプルな方式です。
        /// P09モデルの髪はデフォルトではモデルルート直下にありますが、
        /// Headボーンの子にすることで頭の回転に追従させます。
        /// ワールド位置と回転を維持したまま再配置します。
        /// </remarks>
        private void ReparentHairToHead()
        {
            if (_hairReparented)
            {
                return;
            }

            if (_headTransform == null)
            {
                if (_humanoidAnimator == null)
                {
                    _humanoidAnimator = FindHumanoidAnimator();
                    if (_humanoidAnimator == null)
                    {
                        return;
                    }
                }
                _headTransform = _humanoidAnimator.GetBoneTransform(HumanBodyBones.Head);
                if (_headTransform == null)
                {
                    Debug.LogWarning($"[AIPlayerController] AI {_aiId}: ReparentHairToHead - Head bone not found");
                    return;
                }
            }

            // P09モデルのルートを取得
            Transform? riderRoot = _humanoidAnimator?.transform;
            if (riderRoot == null)
            {
                return;
            }

            // 髪オブジェクトを検索して再配置（PlayerControllerと同じシンプルな方式）
            var allChildren = riderRoot.GetComponentsInChildren<Transform>(true);

            foreach (var child in allChildren)
            {
                // Hair_ で始まるオブジェクト（P09の髪メッシュ形式: Hair_01, Hair_02等）
                if (child.name.StartsWith("Hair_") && child.parent != _headTransform)
                {
                    // 現在のワールド位置と回転を保存
                    Vector3 worldPos = child.position;
                    Quaternion worldRot = child.rotation;

                    // Headの子に移動
                    child.SetParent(_headTransform);

                    // ワールド位置と回転を維持
                    child.position = worldPos;
                    child.rotation = worldRot;
                }
            }

            _hairReparented = true;
        }

        #endregion

        #region Animation

        /// <summary>
        /// Animatorを更新します
        /// </summary>
        /// <remarks>
        /// 馬のMAnimalから速度を取得してAnimatorに設定します。
        /// P09 Riderのアニメーションパラメータに合わせて更新します。
        /// </remarks>
        private void UpdateAnimator()
        {
            if (_animator == null)
            {
                return;
            }

            // MAnimalから速度を取得
            float speed = 0f;
            if (_mAnimal != null)
            {
                // MAnimalの水平速度を使用
                speed = _mAnimal.HorizontalSpeed;
            }

            // Animatorに設定
            _animator.SetFloat(SpeedParam, speed);

            // チャージ中はChargeAmountも更新
            if (_isCharging)
            {
                _animator.SetFloat(ChargeAmountParam, _currentCharge);
                _animator.SetBool(IsAimingParam, true);
            }
            else
            {
                _animator.SetBool(IsAimingParam, false);
            }
        }

        #endregion
    }

    /// <summary>
    /// AIの状態
    /// </summary>
    public enum AIState
    {
        /// <summary>待機中</summary>
        Idle,

        /// <summary>巡回中</summary>
        Patrol,

        /// <summary>追跡中</summary>
        Chase,

        /// <summary>攻撃中</summary>
        Attack,

        /// <summary>横移動中</summary>
        Strafe,

        /// <summary>後退中</summary>
        Retreat,

        /// <summary>死亡</summary>
        Dead
    }
}
