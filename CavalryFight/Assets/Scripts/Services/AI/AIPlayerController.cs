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

        [Header("Aim Reference")]
        [Tooltip("矢の根元位置（弓のグリップ部分）。_bowFirePointと組み合わせて発射方向を計算します。")]
        [SerializeField] private Transform? _arrowRootPoint;

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

        // RiderController（アニメーション制御用）
        private MonoBehaviour? _riderController;
        private System.Reflection.MethodInfo? _setChargeAmountMethod;
        private System.Reflection.MethodInfo? _setAnimationStateMethod;
        private System.Type? _riderAnimationStateType;

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

        // チャージエフェクト（プレイヤーと同じ視覚効果）
        private GameObject? _chargingEffectPrefab;
        private GameObject? _chargingEffectInstance;
        private float _chargingEffectMinScale = 0.1f;
        private float _chargingEffectMaxScale = 1.0f;

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

        // 定期ログ用のタイマー
        private float _periodicLogTimer;
        private const float PeriodicLogInterval = 5f; // 5秒ごとにログ出力

        private void Update()
        {
            if (!_isEnabled || !_isAlive)
            {
                return;
            }

            // 定期的なステータスログ（5秒ごと）
            _periodicLogTimer += Time.deltaTime;
            if (_periodicLogTimer >= PeriodicLogInterval)
            {
                _periodicLogTimer = 0f;
                LogPeriodicStatus();
            }

            UpdateStateMachine();
        }

        /// <summary>
        /// 定期的なステータスをログ出力します（デバッグ用）
        /// </summary>
        private void LogPeriodicStatus()
        {
            string targetName = _currentTarget != null ? _currentTarget.name : "NULL";
            string mountPos = _mountObject != null ? _mountObject.transform.position.ToString() : "NULL";
            float distToTarget = _currentTarget != null ? Vector3.Distance(transform.position, _currentTarget.transform.position) : -1f;
            bool canSee = _currentTarget != null && CanSeeTarget(_currentTarget);
            bool canAttack = Time.time >= _nextAttackTime;
            float timeToAttack = _nextAttackTime - Time.time;

            Debug.Log($"[AI-PERIODIC] AI {_aiId}: State={_currentState}, Target={targetName}, Dist={distToTarget:F1}m, Range={_attackRange:F1}m");
            Debug.Log($"[AI-PERIODIC] AI {_aiId}: CanSee={canSee}, CanAttack={canAttack}, TimeToAttack={timeToAttack:F1}s, Charging={_isCharging}, Charge={_currentCharge:P0}");
            Debug.Log($"[AI-PERIODIC] AI {_aiId}: ArrowPrefab={(_arrowPrefab != null)}, FirePoint={(_bowFirePoint != null ? _bowFirePoint.name : "NULL")}, Spine={(_spineTransform != null)}");

            // MAnimalの状態
            if (_mAnimal != null)
            {
                Debug.Log($"[AI-PERIODIC] AI {_aiId}: MAnimal - Grounded={_mAnimal.Grounded}, HSpeed={_mAnimal.HorizontalSpeed:F2}, ActiveState={_mAnimal.ActiveState?.name ?? "NULL"}");
            }

            // MAnimalAIControlの状態
            if (_mAnimalAIControl != null)
            {
                string aiTarget = _mAnimalAIControl.Target != null ? _mAnimalAIControl.Target.name : "NULL";
                Debug.Log($"[AI-PERIODIC] AI {_aiId}: MAnimalAIControl - enabled={_mAnimalAIControl.enabled}, Target={aiTarget}, HasArrived={_mAnimalAIControl.HasArrived}, IsMoving={_mAnimalAIControl.IsMoving}");
            }

            // NavMeshAgentの状態（馬のもの）
            if (_mountObject != null)
            {
                var navAgent = _mountObject.GetComponentInChildren<NavMeshAgent>();
                if (navAgent != null)
                {
                    Debug.Log($"[AI-PERIODIC] AI {_aiId}: NavMeshAgent - enabled={navAgent.enabled}, isOnNavMesh={navAgent.isOnNavMesh}, hasPath={navAgent.hasPath}, pathPending={navAgent.pathPending}, velocity={navAgent.velocity}, remainingDistance={navAgent.remainingDistance:F2}");
                }
            }
        }

        // デバッグログ用タイマー
        private float _lateUpdateLogTimer;

        private void LateUpdate()
        {
            // 弓を常に左手に固定（isEnabledに関わらず実行）
            ForceBowToLeftHand(_isCharging);

            if (!_isEnabled || !_isAlive)
            {
                return;
            }

            // 戦闘中は上半身をターゲット方向に回転
            // ★修正: Attack状態または_isChargingの場合にエイム（Chase/Strafe中の機動射撃も含む）
            bool shouldAim = _currentTarget != null &&
                (_currentState == AIState.Attack || _currentState == AIState.Strafe || _isCharging);

            // デバッグログ（2秒ごと）
            _lateUpdateLogTimer += Time.deltaTime;
            if (_lateUpdateLogTimer >= 2f)
            {
                _lateUpdateLogTimer = 0f;
                Debug.Log($"[AI-AIM-DEBUG] AI {_aiId}: shouldAim={shouldAim}, target={(_currentTarget != null ? _currentTarget.name : "NULL")}, state={_currentState}, charging={_isCharging}, spine={(_spineTransform != null)}");
            }

            if (shouldAim)
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
            // ★スパイン遅延初期化: humanoidAnimatorがあるがspineがない場合は再取得を試みる
            if (_spineTransform == null && _humanoidAnimator != null)
            {
                try
                {
                    _spineTransform = _humanoidAnimator.GetBoneTransform(HumanBodyBones.Spine);
                    if (_spineTransform == null)
                    {
                        _spineTransform = _humanoidAnimator.GetBoneTransform(HumanBodyBones.Chest);
                    }
                    if (_spineTransform != null)
                    {
                        Debug.Log($"[AI-SPINE] AI {_aiId}: ★ Spine transform LAZY initialized: {_spineTransform.name}");
                    }
                }
                catch (System.Exception) { }
            }

            if (_spineTransform == null || _currentTarget == null || _mountObject == null)
            {
                // デバッグ: なぜ回転しないかを確認
                if (Time.frameCount % 120 == 0) // 2秒ごと
                {
                    Debug.LogWarning($"[AI-SPINE] AI {_aiId}: RotateSpine SKIPPED - spine={(_spineTransform != null)}, target={(_currentTarget != null)}, mount={(_mountObject != null)}, humanoidAnim={(_humanoidAnimator != null)}");
                }
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
            Debug.Log($"[AI-INIT] AIPlayerController.Initialize() on {gameObject.name}, aiId={aiId}");
            Debug.Log($"[AI-INIT] Serialized fields: _bowFirePoint={(_bowFirePoint != null ? _bowFirePoint.name : "NULL")}, _arrowRootPoint={(_arrowRootPoint != null ? _arrowRootPoint.name : "NULL")}, _arrowPrefab={(_arrowPrefab != null ? _arrowPrefab.name : "NULL")}");

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

            // チャージエフェクトを初期化
            InitializeChargingEffect();

            // RiderControllerを検索（アニメーション制御用）
            InitializeRiderController();

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
            _humanoidAnimator = FindHumanoidAnimator();
            if (_humanoidAnimator == null)
            {
                Debug.LogWarning($"[AIPlayerController] AI {_aiId}: Humanoid Animator not found");
                return;
            }

            Debug.Log($"[AIPlayerController] AI {_aiId}: ★ Humanoid animator set to: {_humanoidAnimator.gameObject.name}");

            // Humanoid AnimatorからSpineボーンとHeadボーンを取得
            try
            {
                _spineTransform = _humanoidAnimator.GetBoneTransform(HumanBodyBones.Spine);
                if (_spineTransform == null)
                {
                    _spineTransform = _humanoidAnimator.GetBoneTransform(HumanBodyBones.Chest);
                }

                _headTransform = _humanoidAnimator.GetBoneTransform(HumanBodyBones.Head);

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
            Debug.Log($"[AIPlayerController] AI {_aiId}: FindHumanoidAnimator() searching...");

            // 自身のAnimatorをチェック（馬ではない）
            if (_animator != null)
            {
                bool hasAvatar = _animator.avatar != null;
                bool isHuman = hasAvatar && _animator.avatar.isHuman;
                bool isHorse = IsHorseAnimator(_animator);
                Debug.Log($"[AIPlayerController] AI {_aiId}: Self animator: {_animator.gameObject.name}, hasAvatar={hasAvatar}, isHuman={isHuman}, isHorse={isHorse}");

                if (isHuman && !isHorse)
                {
                    return _animator;
                }
            }

            // 子オブジェクトからHumanoid Animatorを検索（馬は除外）
            var animators = GetComponentsInChildren<Animator>(true);
            Debug.Log($"[AIPlayerController] AI {_aiId}: Found {animators.Length} animators in children");

            foreach (var anim in animators)
            {
                bool hasAvatar = anim.avatar != null;
                bool isHuman = hasAvatar && anim.avatar.isHuman;
                bool isHorse = IsHorseAnimator(anim);
                Debug.Log($"[AIPlayerController] AI {_aiId}: Child animator: {anim.gameObject.name}, hasAvatar={hasAvatar}, isHuman={isHuman}, isHorse={isHorse}");

                if (isHuman && !isHorse)
                {
                    Debug.Log($"[AIPlayerController] AI {_aiId}: ★ Selected humanoid animator: {anim.gameObject.name}");
                    return anim;
                }
            }

            Debug.LogWarning($"[AIPlayerController] AI {_aiId}: No humanoid animator found!");
            return null;
        }

        /// <summary>
        /// 指定されたAnimatorが馬のものかどうかを判定します
        /// </summary>
        private bool IsHorseAnimator(Animator animator)
        {
            // 馬のコンポーネントが同じGameObjectにあるか確認
            var mAnimal = animator.GetComponent<MalbersAnimations.Controller.MAnimal>();
            if (mAnimal != null) return true;

            // 名前で判定（馬っぽい名前かつP09でない場合のみ）
            string name = animator.gameObject.name.ToLower();

            // P09は明らかにライダー（人間）
            if (name.Contains("p09") || name.Contains("human") || name.Contains("rider"))
            {
                return false; // これはライダー
            }

            // 馬っぽい名前
            if (name.Contains("horse") || name.Contains("mount"))
            {
                return true;
            }

            // ★注意: 親に馬がいてもライダーのAnimatorを除外しない
            // P09はAIMount（馬）の子として配置されるため、親チェックは行わない

            return false;
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
            Debug.Log($"[AI-COMBAT] AI {_aiId}: InitializeArrowPrefab() called");

            // 既に設定されている場合はスキップ
            if (_arrowPrefab != null)
            {
                Debug.Log($"[AI-COMBAT] AI {_aiId}: Arrow prefab already set: {_arrowPrefab.name}");
                return;
            }

            // ArrowTypeConfigから取得（PlayerControllerと同じ方式）
            // パスを複数試す（Resources/ArrowTypeConfig または Resources/Settings/ArrowTypeConfig）
            Debug.Log($"[AI-COMBAT] AI {_aiId}: Trying to load ArrowTypeConfig...");
            var arrowTypeConfig = Resources.Load<CavalryFight.Services.Customization.ArrowTypeConfig>("ArrowTypeConfig");
            if (arrowTypeConfig == null)
            {
                Debug.Log($"[AI-COMBAT] AI {_aiId}: ArrowTypeConfig not found at 'ArrowTypeConfig', trying 'Settings/ArrowTypeConfig'...");
                arrowTypeConfig = Resources.Load<CavalryFight.Services.Customization.ArrowTypeConfig>("Settings/ArrowTypeConfig");
            }

            if (arrowTypeConfig != null)
            {
                // デフォルトの矢タイプ（Arrow = 0）のプレハブを取得
                _arrowPrefab = arrowTypeConfig.GetArrowPrefab(CavalryFight.Services.Customization.ArrowType.Arrow);
                if (_arrowPrefab != null)
                {
                    Debug.Log($"[AI-COMBAT] AI {_aiId}: Arrow prefab loaded: {_arrowPrefab.name}");
                }
                else
                {
                    Debug.LogError($"[AI-COMBAT] AI {_aiId}: Arrow prefab not set in ArrowTypeConfig!");
                }
            }
            else
            {
                Debug.LogError($"[AI-COMBAT] AI {_aiId}: ArrowTypeConfig not found! AI cannot shoot.");
            }
        }

        /// <summary>
        /// チャージエフェクトを初期化します
        /// </summary>
        /// <remarks>
        /// AIServiceConfigからチャージエフェクトプレハブを取得します。
        /// プレイヤーと同じチャージエフェクトを使用します。
        /// </remarks>
        private void InitializeChargingEffect()
        {
            // 既に設定されている場合はスキップ
            if (_chargingEffectPrefab != null)
            {
                return;
            }

            // AIServiceConfigから取得
            var aiServiceConfig = Resources.Load<AIServiceConfig>("Settings/AIServiceConfig");
            if (aiServiceConfig != null)
            {
                _chargingEffectPrefab = aiServiceConfig.ChargingEffectPrefab;
                _chargingEffectMinScale = aiServiceConfig.ChargingEffectMinScale;
                _chargingEffectMaxScale = aiServiceConfig.ChargingEffectMaxScale;

                if (_chargingEffectPrefab != null)
                {
                    Debug.Log($"[AI-COMBAT] AI {_aiId}: Charging effect prefab loaded: {_chargingEffectPrefab.name}");
                }
                else
                {
                    Debug.Log($"[AI-COMBAT] AI {_aiId}: No charging effect prefab set in AIServiceConfig");
                }
            }
        }

        /// <summary>
        /// チャージエフェクトを生成します
        /// </summary>
        private void SpawnChargingEffect()
        {
            if (_chargingEffectPrefab == null || _bowFirePoint == null)
            {
                Debug.LogWarning($"[AI-COMBAT] AI {_aiId}: Cannot spawn charging effect - prefab={(_chargingEffectPrefab != null)}, firePoint={(_bowFirePoint != null)}");
                return;
            }

            // 既存のエフェクトがあれば破棄
            DestroyChargingEffect();

            // チャージエフェクトを弓の発射位置に生成し、子として設定
            _chargingEffectInstance = Instantiate(_chargingEffectPrefab, _bowFirePoint.position, _bowFirePoint.rotation, _bowFirePoint);
            _chargingEffectInstance.transform.localPosition = Vector3.zero;

            // 初期スケールを最小に設定
            _chargingEffectInstance.transform.localScale = Vector3.one * _chargingEffectMinScale;

            Debug.Log($"[AI-COMBAT] AI {_aiId}: ★ Charging effect spawned at {_bowFirePoint.name}");
        }

        /// <summary>
        /// チャージエフェクトのスケールを更新します
        /// </summary>
        /// <param name="chargeAmount">チャージ量（0.0～1.0）</param>
        private void UpdateChargingEffectScale(float chargeAmount)
        {
            if (_chargingEffectInstance == null)
            {
                return;
            }

            // チャージ量に応じてスケールを補間
            float scale = Mathf.Lerp(_chargingEffectMinScale, _chargingEffectMaxScale, chargeAmount);
            _chargingEffectInstance.transform.localScale = Vector3.one * scale;
        }

        /// <summary>
        /// チャージエフェクトを破棄します
        /// </summary>
        private void DestroyChargingEffect()
        {
            if (_chargingEffectInstance != null)
            {
                Destroy(_chargingEffectInstance);
                _chargingEffectInstance = null;
            }
        }

        /// <summary>
        /// RiderControllerを検索して初期化します
        /// </summary>
        /// <remarks>
        /// RiderControllerがある場合、SetChargeAmount()を使ってアニメーションを制御します。
        /// これによりプレイヤーと同じ弓引きアニメーションが再生されます。
        /// </remarks>
        private void InitializeRiderController()
        {
            Debug.Log($"[AI-COMBAT] AI {_aiId}: InitializeRiderController() searching for RiderController...");

            // RiderControllerを検索
            var riderControllerType = System.Type.GetType("CavalryFight.Gameplay.Player.RiderController, Assembly-CSharp");
            if (riderControllerType != null)
            {
                _riderController = GetComponent(riderControllerType) as MonoBehaviour;
                if (_riderController == null)
                {
                    _riderController = GetComponentInChildren(riderControllerType) as MonoBehaviour;
                }

                if (_riderController != null)
                {
                    _setChargeAmountMethod = riderControllerType.GetMethod("SetChargeAmount");
                    _setAnimationStateMethod = riderControllerType.GetMethod("SetAnimationState");

                    // RiderAnimationState enumを取得
                    _riderAnimationStateType = System.Type.GetType("CavalryFight.Gameplay.Player.RiderAnimationState, Assembly-CSharp");

                    Debug.Log($"[AI-COMBAT] AI {_aiId}: RiderController FOUND! " +
                        $"SetChargeAmount={_setChargeAmountMethod != null}, " +
                        $"SetAnimationState={_setAnimationStateMethod != null}, " +
                        $"AnimStateType={_riderAnimationStateType != null}");
                }
                else
                {
                    Debug.Log($"[AI-COMBAT] AI {_aiId}: RiderController type exists but component not found on this object");
                }
            }
            else
            {
                Debug.LogWarning($"[AI-COMBAT] AI {_aiId}: RiderController TYPE not found!");
            }

            // RiderControllerがない場合、RiderArcherControllerを試す
            if (_riderController == null)
            {
                var archerControllerType = System.Type.GetType("CavalryFight.Gameplay.Player.RiderArcherController, Assembly-CSharp");
                if (archerControllerType != null)
                {
                    _riderController = GetComponent(archerControllerType) as MonoBehaviour;
                    if (_riderController == null)
                    {
                        _riderController = GetComponentInChildren(archerControllerType) as MonoBehaviour;
                    }

                    if (_riderController != null)
                    {
                        _setChargeAmountMethod = archerControllerType.GetMethod("SetChargeAmount");
                        _setAnimationStateMethod = archerControllerType.GetMethod("SetAnimationState");
                        _riderAnimationStateType = System.Type.GetType("CavalryFight.Gameplay.Player.RiderAnimationState, Assembly-CSharp");

                        Debug.Log($"[AI-COMBAT] AI {_aiId}: RiderArcherController FOUND! " +
                            $"SetChargeAmount={_setChargeAmountMethod != null}, " +
                            $"SetAnimationState={_setAnimationStateMethod != null}");
                    }
                }
            }

            if (_riderController == null)
            {
                Debug.LogWarning($"[AI-COMBAT] AI {_aiId}: ★ NO RiderController found! Using direct animator control. Animator={_animator != null}");

                // Animatorの状態を確認
                if (_animator != null)
                {
                    Debug.Log($"[AI-COMBAT] AI {_aiId}: Animator parameters: " +
                        $"HasIsAiming={HasAnimatorParameter(_animator, "IsAiming")}, " +
                        $"HasChargeAmount={HasAnimatorParameter(_animator, "ChargeAmount")}, " +
                        $"HasShoot={HasAnimatorParameter(_animator, "Shoot")}");
                }
            }
        }

        /// <summary>
        /// Animatorにパラメータが存在するか確認します
        /// </summary>
        private bool HasAnimatorParameter(Animator animator, string paramName)
        {
            foreach (var param in animator.parameters)
            {
                if (param.name == paramName) return true;
            }
            return false;
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
            Debug.Log($"[AI-CTRL-DEBUG] ========== AIPlayerController.Enable() START for AI {_aiId} ==========");

            _isEnabled = true;

            if (_blazeAI != null)
            {
                _blazeAI.enabled = true;
                Debug.Log($"[AI-CTRL-DEBUG] AI {_aiId}: BlazeAI enabled");
            }

            // NavMeshAgentは使用しない（馬のMAnimalBrainが移動を制御）
            // _navAgentは無効のまま

            // 現在の状態をログ出力
            Debug.Log($"[AI-CTRL-DEBUG] AI {_aiId}: _mountObject={((_mountObject != null) ? _mountObject.name : "NULL")}");
            Debug.Log($"[AI-CTRL-DEBUG] AI {_aiId}: _mAnimal={((_mAnimal != null) ? _mAnimal.gameObject.name : "NULL")}");
            Debug.Log($"[AI-CTRL-DEBUG] AI {_aiId}: _mAnimalAIControl={((_mAnimalAIControl != null) ? _mAnimalAIControl.gameObject.name : "NULL")}");
            Debug.Log($"[AI-CTRL-DEBUG] AI {_aiId}: _useMAnimalBrainForMovement={_useMAnimalBrainForMovement}");

            // プレイヤーを自動的にターゲットとして探す
            // ターゲットが見つかった場合はChaseまたはAttack状態になる
            // 見つからなかった場合のみPatrol状態にする
            Debug.Log($"[AI-CTRL-DEBUG] AI {_aiId}: Searching for player target...");
            if (!FindAndSetPlayerTarget())
            {
                Debug.LogWarning($"[AI-CTRL-DEBUG] AI {_aiId}: No player target found! Setting state to Patrol");
                SetState(AIState.Patrol);
            }
            else
            {
                Debug.Log($"[AI-CTRL-DEBUG] AI {_aiId}: Player target found: {(_currentTarget != null ? _currentTarget.name : "NULL")}");
            }

            // MAnimalBrainにもターゲットを設定
            if (_useMAnimalBrainForMovement && _currentTarget != null)
            {
                Debug.Log($"[AI-CTRL-DEBUG] AI {_aiId}: Setting MAnimalBrain target to {_currentTarget.name}");
                SetMAnimalBrainTarget(_currentTarget);
            }

            Debug.Log($"[AI-CTRL-DEBUG] AI {_aiId}: Final state={_currentState}");
            Debug.Log($"[AI-CTRL-DEBUG] ========== AIPlayerController.Enable() END for AI {_aiId} ==========");
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

            // ヒットアニメーション（_humanoidAnimatorを優先）
            Animator? animForHit = _humanoidAnimator ?? _animator;
            if (animForHit != null && animForHit.runtimeAnimatorController != null)
            {
                animForHit.SetTrigger(HitParam);
            }

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

                // リスポーン可能なモード（Arena, ScoreMatch等）では撤退を大幅に抑制
                // 死んでも問題ないので攻撃を続ける
                if (_modeBehavior?.CanRespawn == true)
                {
                    retreatChance *= 0.2f; // 撤退確率を80%削減
                }

                // 高攻撃性モード（Arena）ではほぼ撤退しない
                if (aggressionFactor >= 0.7f)
                {
                    retreatChance *= 0.1f; // さらに90%削減
                }

                if (UnityEngine.Random.value < retreatChance)
                {
                    float retreatDuration = _modeBehavior?.CanRespawn == true ? 1.5f : 3f; // リスポーンモードでは撤退時間も短く
                    Debug.Log($"[AIPlayerController] AI {_aiId} health low ({healthPercent:P0}), retreating for {retreatDuration}s (Aggression: {aggressionFactor:F2})");
                    _strafeEndTime = Time.time + retreatDuration;
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

            // 死亡アニメーション（_humanoidAnimatorを優先）
            Animator? animForDeath = _humanoidAnimator ?? _animator;
            if (animForDeath != null && animForDeath.runtimeAnimatorController != null)
            {
                animForDeath.SetTrigger(DeathParam);
            }

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

            // チャージ状態も含めてログ出力
            Debug.Log($"[AI-COMBAT] AI {_aiId} STATE: {previousState} -> {newState} (charging={_isCharging}, charge={_currentCharge:P0})");
        }

        /// <summary>
        /// 状態に入った時の処理
        /// </summary>
        private void OnStateEnter(AIState state)
        {
            switch (state)
            {
                case AIState.Idle:
                    // 射撃不可の状態に入るのでチャージをキャンセル
                    CancelCharge();
                    break;
                case AIState.Patrol:
                    // 射撃不可の状態に入るのでチャージをキャンセル
                    CancelCharge();
                    StartPatrol();
                    break;
                case AIState.Chase:
                    // Chase中も射撃可能なのでチャージは継続
                    StartChase();
                    break;
                case AIState.Attack:
                    StartAttack();
                    break;
                case AIState.Strafe:
                    // Strafe中も射撃可能なのでチャージは継続
                    StartStrafe();
                    break;
                case AIState.Retreat:
                    // 射撃不可の状態に入るのでチャージをキャンセル
                    CancelCharge();
                    StartRetreat();
                    break;
                case AIState.Dead:
                    // 死亡時はチャージをキャンセル
                    CancelCharge();
                    break;
            }
        }

        /// <summary>
        /// 状態から出た時の処理
        /// </summary>
        private void OnStateExit(AIState state)
        {
            // ★修正: Attack→Strafe/Chaseへの遷移時はチャージを継続（機動射撃のため）
            // チャージをキャンセルするのはOnStateEnter側で非射撃状態に入るときのみ
            switch (state)
            {
                case AIState.Attack:
                    // 以前はここでCancelCharge()を呼んでいたが、
                    // Strafe/Chase状態でも射撃を続行するため削除
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
                // ★重要: コライダーではなく、ルートオブジェクトをターゲットにする
                GameObject? targetRoot = null;
                bool isValidTarget = false;
                int targetHealth = 100; // デフォルト体力

                // Playerタグをチェック（ルートを取得）
                if (col.CompareTag("Player"))
                {
                    targetRoot = col.gameObject;
                    isValidTarget = true;
                }

                // MRider（騎手）コンポーネントをチェック - ルートオブジェクトを取得
                var riderComponents = col.GetComponentsInParent<MonoBehaviour>();
                foreach (var comp in riderComponents)
                {
                    if (comp != null && comp.GetType().Name.Contains("MRider"))
                    {
                        targetRoot = comp.gameObject; // MRiderがあるGameObjectをターゲットに
                        isValidTarget = true;
                        break;
                    }
                }

                // MAnimal（馬）コンポーネントをチェック - ライダーがいればライダーを、いなければ馬をターゲットに
                if (targetRoot == null)
                {
                    var animalComponents = col.GetComponentsInParent<MonoBehaviour>();
                    foreach (var comp in animalComponents)
                    {
                        if (comp != null && comp.GetType().Name.Contains("MAnimal"))
                        {
                            // まず馬にライダーがいるか確認
                            var rider = comp.GetComponentInChildren<MonoBehaviour>();
                            var riderInChildren = comp.gameObject.GetComponentsInChildren<MonoBehaviour>();
                            foreach (var r in riderInChildren)
                            {
                                if (r != null && r.GetType().Name.Contains("MRider"))
                                {
                                    targetRoot = r.gameObject;
                                    break;
                                }
                            }
                            // ライダーがいなければ馬自体をターゲットに
                            if (targetRoot == null)
                            {
                                targetRoot = comp.gameObject;
                            }
                            isValidTarget = true;
                            break;
                        }
                    }
                }

                // 他のAIの場合、AIのルートを使用
                if (otherAI != null)
                {
                    targetRoot = otherAI.gameObject;
                    targetHealth = otherAI._currentHealth;
                    isValidTarget = true;
                }

                if (!isValidTarget || targetRoot == null)
                {
                    continue;
                }

                // 既に追加済みのターゲットはスキップ（同じターゲットの複数コライダーを避ける）
                bool alreadyAdded = false;
                foreach (var (existing, _, _) in validTargets)
                {
                    if (existing == targetRoot)
                    {
                        alreadyAdded = true;
                        break;
                    }
                }
                if (alreadyAdded)
                {
                    continue;
                }

                if (!CanSeeTarget(targetRoot))
                {
                    continue;
                }

                float distance = Vector3.Distance(transform.position, targetRoot.transform.position);
                validTargets.Add((targetRoot, distance, targetHealth));
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
                Debug.Log($"[AI-COMBAT] AI {_aiId}: UpdateChase - target is null, switching to Patrol");
                SetState(AIState.Patrol);
                return;
            }

            float distanceToTarget = Vector3.Distance(transform.position, _currentTarget.transform.position);

            // ゲームモード別の攻撃範囲調整
            // 攻撃性が高いほど遠距離から攻撃を試みる
            float aggressionFactor = _modeBehavior?.AggressionLevel ?? 0.5f;
            float effectiveAttackRange = _attackRange * (0.8f + aggressionFactor * 0.4f); // 攻撃性0.8で1.12倍

            // ★既にチャージ中の場合は継続（Attack状態から遷移してきた場合など）
            if (_isCharging && CanSeeTarget(_currentTarget))
            {
                UpdateCharge();
            }

            // 毎秒ログ出力（デバッグ用）
            if (Time.frameCount % 60 == 0)
            {
                bool canSee = CanSeeTarget(_currentTarget);
                bool isMoving = _mAnimalAIControl?.IsMoving ?? false;
                Debug.Log($"[AI-COMBAT] AI {_aiId}: Chase dist={distanceToTarget:F1}m, range={effectiveAttackRange:F1}m, canSee={canSee}, isMoving={isMoving}, charging={_isCharging}");
            }

            // 攻撃範囲内なら攻撃に遷移
            if (distanceToTarget <= effectiveAttackRange && CanSeeTarget(_currentTarget))
            {
                SetState(AIState.Attack);
                return;
            }

            // 攻撃性が高いモードでは遠距離からでも射撃を試みる（騎馬弓兵の機動射撃）
            // ただし距離が遠いほど命中精度は下がる
            if (aggressionFactor >= 0.6f && distanceToTarget <= _attackRange * 1.5f && CanSeeTarget(_currentTarget))
            {
                // 追跡しながらも射撃を試みる
                if (Time.time >= _nextAttackTime && !_isCharging)
                {
                    StartCharge();
                }
            }

            // ターゲットに向かって移動（MAnimalAIControlが処理）
            SetMAnimalBrainTarget(_currentTarget);
        }

        private void StartAttack()
        {
            // ゲームモードの攻撃性に応じて反応時間を調整
            // 攻撃性が高いほど素早く攻撃を開始
            float aggressionFactor = _modeBehavior?.AggressionLevel ?? 0.5f;
            float reactionTimeModifier = 1f - (aggressionFactor * 0.5f); // 攻撃性0.8で0.6倍に短縮
            float adjustedReactionTime = _difficultySettings.ReactionTime * reactionTimeModifier;

            _nextAttackTime = Time.time + adjustedReactionTime;

            Debug.Log($"[AI-COMBAT] AI {_aiId} StartAttack: ReactionTime={adjustedReactionTime:F2}s (aggression={aggressionFactor:F2}), NextAttackTime={_nextAttackTime:F2}");
        }

        private void UpdateAttack()
        {
            float distanceToTarget = _currentTarget != null
                ? Vector3.Distance(transform.position, _currentTarget.transform.position)
                : -1f;
            bool canAttack = Time.time >= _nextAttackTime;

            if (_currentTarget == null)
            {
                Debug.Log($"[AI-COMBAT] AI {_aiId}: Target is NULL, switching to Patrol");
                SetState(AIState.Patrol);
                return;
            }

            // 攻撃範囲外なら追跡に戻る
            if (distanceToTarget > _attackRange)
            {
                Debug.Log($"[AI-COMBAT] AI {_aiId}: Out of range ({distanceToTarget:F1}m > {_attackRange:F1}m), switching to Chase");
                SetState(AIState.Chase);
                return;
            }

            // ★騎馬弓兵の機動戦: 常に移動し続ける
            // ターゲットに近すぎる場合は回り込む、遠い場合は近づく
            if (distanceToTarget < _attackRange * 0.3f)
            {
                // 近すぎる - 回避しながら射撃（サイドに移動）
                PerformCirclingMovement();
            }
            else
            {
                // ターゲットに向かいながら射撃
                LookAtTarget();

                // 馬を移動させ続ける（停止を防ぐ）
                if (_mAnimalAIControl != null)
                {
                    _mAnimalAIControl.Move();
                }
            }

            // ゲームモード別の攻撃行動
            float aggressionFactor = _modeBehavior?.AggressionLevel ?? 0.5f;
            float scoringPriority = _modeBehavior?.ScoringPriority ?? 0.5f;

            // 攻撃タイミング
            if (canAttack)
            {
                if (!_isCharging)
                {
                    Debug.Log($"[AI-COMBAT] AI {_aiId}: >>> STARTING CHARGE <<< (target={_currentTarget.name}, dist={distanceToTarget:F1}m)");
                    StartCharge();
                }
                else
                {
                    UpdateCharge();
                }
            }

            // ストレイフ判定
            // - 攻撃性が高いほどストレイフしにくい（攻撃に集中）
            // - スコア重視モードではストレイフよりも攻撃優先
            // - チャージ中はストレイフしない
            // - ★重要: クールダウン中はストレイフしない（攻撃準備中）
            if (canAttack && !_isCharging)
            {
                float strafeModifier = (1f - aggressionFactor) * (1f - scoringPriority * 0.5f);
                float adjustedStrafeChance = _difficultySettings.StrafeChance * strafeModifier;

                // 高攻撃性モード（Arena等）ではストレイフを大幅に抑制
                if (aggressionFactor >= 0.7f)
                {
                    adjustedStrafeChance *= 0.3f; // 70%削減
                }

                if (UnityEngine.Random.value < adjustedStrafeChance * Time.deltaTime)
                {
                    SetState(AIState.Strafe);
                }
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

            // ★既にチャージ中の場合は継続（Attack状態から遷移してきた場合など）
            if (_isCharging && CanSeeTarget(_currentTarget))
            {
                UpdateCharge();
                return; // チャージ中は新規チャージ判定をスキップ
            }

            // ★騎馬弓兵はストレイフ中も射撃可能
            // 攻撃性が高いほどストレイフ中の攻撃頻度が上がる
            float aggressionFactor = _modeBehavior?.AggressionLevel ?? 0.5f;
            if (aggressionFactor > 0.3f && Time.time >= _nextAttackTime)
            {
                float distanceToTarget = Vector3.Distance(transform.position, _currentTarget.transform.position);
                if (distanceToTarget <= _attackRange && CanSeeTarget(_currentTarget))
                {
                    StartCharge();
                }
            }
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
            // 攻撃性が高いモードでは早く攻撃に戻る
            float aggressionFactor = _modeBehavior?.AggressionLevel ?? 0.5f;
            float retreatReduction = aggressionFactor * 0.5f; // 攻撃性0.8で40%短縮

            if (Time.time > _strafeEndTime * (1f - retreatReduction))
            {
                // リスポーン可能なモード（Arena等）では積極的に攻撃再開
                if (_modeBehavior?.CanRespawn == true && aggressionFactor >= 0.6f)
                {
                    Debug.Log($"[AI-ATTACK] AI {_aiId} ending retreat early (respawn mode + high aggression)");
                }
                SetState(AIState.Attack);
                return;
            }

            // 高攻撃性モードでは撤退中もターゲットを追跡し続ける
            if (aggressionFactor >= 0.7f && _currentTarget != null)
            {
                SetMAnimalBrainTarget(_currentTarget);

                // 撤退中でも射撃のチャンスがあれば撃つ
                float distanceToTarget = Vector3.Distance(transform.position, _currentTarget.transform.position);
                if (distanceToTarget <= _attackRange && CanSeeTarget(_currentTarget) && Time.time >= _nextAttackTime)
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
            }
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
                        // ★重要: AIReadyがfalseの場合、StartAI()を呼んで初期化する必要がある
                        if (!_mAnimalAIControl.AIReady)
                        {
                            _mAnimalAIControl.SetTarget(target.transform);
                            _mAnimalAIControl.StartAI();
                            Debug.Log($"[AI-COMBAT] AI {_aiId}: StartAI() called, AIReady={_mAnimalAIControl.AIReady}");
                        }
                        else
                        {
                            _mAnimalAIControl.SetTarget(target.transform);
                        }

                        // 移動開始を強制
                        _mAnimalAIControl.Move();
                    }
                    else
                    {
                        _mAnimalAIControl.Stop();
                    }
                    return;
                }
                catch (System.Exception e)
                {
                    Debug.LogError($"[AI-COMBAT] AI {_aiId}: MAnimalAIControl error: {e.Message}");
                }
            }
            else
            {
                Debug.LogWarning($"[AI-COMBAT] AI {_aiId}: MAnimalAIControl is NULL!");
            }

            // フォールバック: リフレクションでMAnimalBrainを使用
            if (_mAnimalBrain == null)
            {
                Debug.LogError($"[AI-COMBAT] AI {_aiId}: MAnimalBrain is also NULL!");
                return;
            }

            var setTargetMethod = _mAnimalBrain.GetType().GetMethod("SetTarget");
            if (setTargetMethod != null)
            {
                if (target != null)
                {
                    setTargetMethod.Invoke(_mAnimalBrain, new object[] { target.transform });
                }
                else
                {
                    setTargetMethod.Invoke(_mAnimalBrain, new object?[] { null });
                }
            }
            else
            {
                Debug.LogError($"[AI-COMBAT] AI {_aiId}: MAnimalBrain.SetTarget not found!");
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

            // エイムアニメーションを開始（RiderController.SetAnimationState(Aiming)）
            if (_setAnimationStateMethod != null && _riderController != null && _riderAnimationStateType != null)
            {
                try
                {
                    // RiderAnimationState.Aiming = 2
                    var aimingState = System.Enum.ToObject(_riderAnimationStateType, 2);
                    _setAnimationStateMethod.Invoke(_riderController, new object[] { aimingState });
                    Debug.Log($"[AI-COMBAT] AI {_aiId}: ★ SetAnimationState(Aiming) called via RiderController");
                }
                catch (System.Exception ex)
                {
                    Debug.LogWarning($"[AI-COMBAT] AI {_aiId}: SetAnimationState failed: {ex.Message}");
                }
            }
            else
            {
                // フォールバック: 直接アニメーターを制御
                // P09のAnimatorはブレンドツリーを使用するため、パラメータのみ設定
                // _humanoidAnimatorを優先使用（P09のAnimator）
                Animator? animToUse = _humanoidAnimator ?? _animator;

                Debug.Log($"[AI-COMBAT] AI {_aiId}: Using DIRECT animator control. " +
                    $"humanoidAnimator={(_humanoidAnimator != null ? _humanoidAnimator.gameObject.name : "NULL")}, " +
                    $"_animator={(_animator != null ? _animator.gameObject.name : "NULL")}, " +
                    $"using={( animToUse != null ? animToUse.gameObject.name : "NULL")}");

                if (animToUse != null)
                {
                    // AnimatorControllerが設定されているか確認
                    bool hasController = animToUse.runtimeAnimatorController != null;
                    Debug.Log($"[AI-COMBAT] AI {_aiId}: Animator has controller: {hasController}, " +
                        $"HasIsAiming={HasAnimatorParameter(animToUse, "IsAiming")}, " +
                        $"HasChargeAmount={HasAnimatorParameter(animToUse, "ChargeAmount")}");

                    if (hasController)
                    {
                        animToUse.SetBool(IsAimingParam, true);
                        animToUse.SetBool("IsMounted", true);  // 騎乗状態
                        // ブレンドツリーの開始のため初期チャージ量を設定
                        animToUse.SetFloat(ChargeAmountParam, 0f);
                        Debug.Log($"[AI-COMBAT] AI {_aiId}: ★ Set IsAiming=true, IsMounted=true on {animToUse.gameObject.name}");
                    }
                    else
                    {
                        Debug.LogWarning($"[AI-COMBAT] AI {_aiId}: ★ AnimatorController missing on {animToUse.gameObject.name}! Animation won't play.");
                    }
                }
            }

            // チャージエフェクトを生成（プレイヤーと同じ視覚効果）
            SpawnChargingEffect();

            // チャージ時間は難易度に応じて変動
            float targetChargeTime = _maxChargeTime * _difficultySettings.ChargeTimeMultiplier;

            Debug.Log($"[AI-COMBAT] AI {_aiId}: StartCharge! maxChargeTime={_maxChargeTime:F2}, targetTime={targetChargeTime:F2}s");
        }

        /// <summary>
        /// チャージを更新します
        /// </summary>
        private void UpdateCharge()
        {
            float chargeTime = Time.time - _chargeStartTime;
            float targetChargeTime = _maxChargeTime * _difficultySettings.ChargeTimeMultiplier;

            _currentCharge = Mathf.Clamp01(chargeTime / targetChargeTime);

            // RiderControllerがある場合はそれを使う（プレイヤーと同じアニメーション）
            if (_setChargeAmountMethod != null && _riderController != null)
            {
                try
                {
                    _setChargeAmountMethod.Invoke(_riderController, new object[] { _currentCharge });
                }
                catch (System.Exception) { }
            }
            else
            {
                // フォールバック: 直接アニメーターを制御（_humanoidAnimatorを優先）
                Animator? animToUse = _humanoidAnimator ?? _animator;
                if (animToUse != null && animToUse.runtimeAnimatorController != null)
                {
                    animToUse.SetFloat(ChargeParam, _currentCharge);
                    animToUse.SetFloat(ChargeAmountParam, _currentCharge);
                    animToUse.SetBool(IsAimingParam, true);
                }
            }

            // チャージエフェクトのスケールを更新（プレイヤーと同じ）
            UpdateChargingEffectScale(_currentCharge);

            // チャージ進行を50%ごとにログ
            if (_currentCharge >= 0.5f && _currentCharge < 0.52f)
            {
                Debug.Log($"[AI-COMBAT] AI {_aiId}: Charging 50%...");
            }

            // チャージ完了で発射
            if (_currentCharge >= 1f)
            {
                Debug.Log($"[AI-COMBAT] AI {_aiId}: Charge complete! Firing arrow...");
                FireArrow();
            }
        }

        /// <summary>
        /// チャージをキャンセルします
        /// </summary>
        private void CancelCharge()
        {
            if (_isCharging)
            {
                // スタックトレースを取得して呼び出し元を特定
                var stackTrace = new System.Diagnostics.StackTrace(true);
                var callerFrame = stackTrace.GetFrame(1);
                string callerMethod = callerFrame?.GetMethod()?.Name ?? "Unknown";
                Debug.Log($"[AI-COMBAT] AI {_aiId}: Charge CANCELLED at {_currentCharge * 100:F0}% (called from {callerMethod})");
            }
            _isCharging = false;
            _currentCharge = 0f;

            // アニメーション状態をMountedIdleに戻す
            if (_setAnimationStateMethod != null && _riderController != null && _riderAnimationStateType != null)
            {
                try
                {
                    // RiderAnimationState.MountedIdle = 1
                    var mountedIdleState = System.Enum.ToObject(_riderAnimationStateType, 1);
                    _setAnimationStateMethod.Invoke(_riderController, new object[] { mountedIdleState });
                }
                catch (System.Exception) { }
            }

            // チャージ量をリセット
            if (_setChargeAmountMethod != null && _riderController != null)
            {
                try
                {
                    _setChargeAmountMethod.Invoke(_riderController, new object[] { 0f });
                }
                catch (System.Exception) { }
            }
            else
            {
                // フォールバック: 直接アニメーターを制御（_humanoidAnimatorを優先）
                Animator? animToUse = _humanoidAnimator ?? _animator;
                if (animToUse != null && animToUse.runtimeAnimatorController != null)
                {
                    animToUse.SetFloat(ChargeParam, 0f);
                    animToUse.SetFloat(ChargeAmountParam, 0f);
                    animToUse.SetBool(IsAimingParam, false);
                }
            }

            // チャージエフェクトを破棄
            DestroyChargingEffect();
        }

        /// <summary>
        /// 矢を発射します
        /// </summary>
        private void FireArrow()
        {
            if (_arrowPrefab == null || _bowFirePoint == null || _currentTarget == null)
            {
                Debug.LogWarning($"[AI-COMBAT] AI {_aiId}: FireArrow ABORTED - arrowPrefab={(_arrowPrefab != null)}, firePoint={(_bowFirePoint != null)}, target={(_currentTarget != null ? _currentTarget.name : "NULL")}");
                CancelCharge();
                return;
            }

            // ミス判定
            bool isMiss = UnityEngine.Random.value < _difficultySettings.MissChance;

            // 発射方向を計算
            Vector3 direction;
            string directionSource;

            if (_arrowRootPoint != null && _bowFirePoint != null)
            {
                // 2点から方向を計算（根元→発射点）
                Vector3 rootPos = _arrowRootPoint.position;
                Vector3 firePos = _bowFirePoint.position;
                direction = (firePos - rootPos).normalized;
                directionSource = "twoPoint";

                // デバッグログ
                Debug.Log($"[AI-ARROW-DIR] AI {_aiId}: rootPos={rootPos}, firePos={firePos}, dir={direction}, dist={(firePos - rootPos).magnitude:F3}");
            }
            else
            {
                // フォールバック: ターゲットへの直接方向
                Vector3 targetAimPos = _currentTarget.transform.position + Vector3.up * 1f;
                direction = (targetAimPos - _bowFirePoint.position).normalized;
                directionSource = "target";

                Debug.Log($"[AI-ARROW-DIR] AI {_aiId}: FALLBACK - rootPoint={(_arrowRootPoint != null)}, firePoint={(_bowFirePoint != null)}, dir={direction}");
            }

            // 精度に応じてブレを追加
            if (!isMiss)
            {
                float maxAngleOffset = (1f - _difficultySettings.AimAccuracy) * 10f;
                float angleX = UnityEngine.Random.Range(-maxAngleOffset, maxAngleOffset);
                float angleY = UnityEngine.Random.Range(-maxAngleOffset, maxAngleOffset);
                Quaternion rotation = Quaternion.Euler(angleX, angleY, 0);
                direction = rotation * direction;
            }
            else
            {
                float angleX = UnityEngine.Random.Range(-30f, 30f);
                float angleY = UnityEngine.Random.Range(-30f, 30f);
                Quaternion rotation = Quaternion.Euler(angleX, angleY, 0);
                direction = rotation * direction;
            }

            Debug.Log($"[AI-ARROW-DIR] AI {_aiId}: FINAL dir={direction}, source={directionSource}, miss={isMiss}");

            // 矢の速度
            float arrowSpeed = Mathf.Lerp(_minArrowSpeed, _maxArrowSpeed, _currentCharge);
            Vector3 velocity = direction * arrowSpeed;

            // 矢のスポーン位置を前方にオフセット（馬との衝突を防ぐ）
            Vector3 spawnPosition = _bowFirePoint.position + direction * 0.5f;

            Debug.Log($"[AI-ARROW-SPAWN] AI {_aiId}: SpawnPos={spawnPosition}, FirePoint={_bowFirePoint.position}, Dir={direction}, Speed={arrowSpeed}");

            // 矢の親オブジェクトを作成（プレイヤーと同じ方式）
            GameObject arrowParent = new GameObject("AIArrow");
            arrowParent.transform.position = spawnPosition;
            arrowParent.transform.rotation = Quaternion.LookRotation(direction);

            // VFX矢プレハブを親の子としてインスタンス化
            GameObject arrowVisual = Instantiate(_arrowPrefab, arrowParent.transform);
            arrowVisual.transform.localPosition = Vector3.zero;
            arrowVisual.transform.localRotation = Quaternion.identity;
            arrowVisual.transform.localScale = Vector3.one * 0.1f; // 矢のスケール

            Debug.Log($"[AI-ARROW-SPAWN] AI {_aiId}: Arrow created - parent={arrowParent.name}, visual={arrowVisual.name}, scale={arrowVisual.transform.localScale}");

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
            // AddIgnoredObject(GameObject? obj, bool isOwner = false) - 2つのパラメータが必要
            if (arrowProjectile != null)
            {
                var addIgnoredMethod = arrowProjectileType!.GetMethod("AddIgnoredObject");
                if (addIgnoredMethod != null)
                {
                    // isOwner=true で自分自身を設定
                    addIgnoredMethod.Invoke(arrowProjectile, new object[] { gameObject, true });
                    if (_mountObject != null)
                    {
                        addIgnoredMethod.Invoke(arrowProjectile, new object[] { _mountObject, false });
                    }
                    addIgnoredMethod.Invoke(arrowProjectile, new object[] { transform.root.gameObject, false });
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

            // アニメーション - SetAnimationState(Shooting)を使用
            if (_setAnimationStateMethod != null && _riderController != null && _riderAnimationStateType != null)
            {
                try
                {
                    // RiderAnimationState.Shooting = 3
                    var shootingState = System.Enum.ToObject(_riderAnimationStateType, 3);
                    _setAnimationStateMethod.Invoke(_riderController, new object[] { shootingState });
                }
                catch (System.Exception) { }
            }
            else
            {
                // フォールバック: 直接アニメーターを制御（_humanoidAnimatorを優先）
                Animator? animToUse = _humanoidAnimator ?? _animator;
                if (animToUse != null && animToUse.runtimeAnimatorController != null)
                {
                    animToUse.SetTrigger(ShootParam);
                    animToUse.SetBool(IsAimingParam, false);
                }
            }

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

            Debug.Log($"[AI-COMBAT] AI {_aiId}: ★★★ ARROW FIRED ★★★ Speed={arrowSpeed:F1}, Miss={isMiss}");
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
        /// ターゲットの周囲を円を描くように移動します（騎馬弓兵の機動戦）
        /// </summary>
        /// <remarks>
        /// ターゲットに近すぎる場合、直接向かうのではなくサイドに移動して
        /// 距離を保ちながら射撃できるようにします。
        /// </remarks>
        private void PerformCirclingMovement()
        {
            if (_currentTarget == null || _mAnimalAIControl == null) return;

            // ターゲットを中心に円を描くように移動
            Vector3 toTarget = _currentTarget.transform.position - transform.position;
            toTarget.y = 0; // Y軸は無視

            // サイド方向を計算（現在のstrafeDirectionを使用）
            Vector3 sideDirection = Vector3.Cross(Vector3.up, toTarget.normalized) * _strafeDirection;

            // サイド方向の目標位置（距離を保ちながら横に移動）
            float idealDistance = _attackRange * 0.5f;
            Vector3 targetPosition = _currentTarget.transform.position - toTarget.normalized * idealDistance + sideDirection * 5f;

            // 仮のゲームオブジェクトを使わずにDestinationを直接設定
            // MAnimalAIControlのSetDestinationを使用
            try
            {
                var setDestMethod = _mAnimalAIControl.GetType().GetMethod("SetDestination",
                    new System.Type[] { typeof(Vector3) });
                if (setDestMethod != null)
                {
                    setDestMethod.Invoke(_mAnimalAIControl, new object[] { targetPosition });
                    _mAnimalAIControl.Move();
                }
                else
                {
                    // フォールバック: ターゲットに向かう
                    SetMAnimalBrainTarget(_currentTarget);
                }
            }
            catch
            {
                SetMAnimalBrainTarget(_currentTarget);
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
            Debug.Log($"[AIPlayerController] AI {_aiId}: SetupBowToHandDelayed() coroutine STARTED");

            // キャッシュをリセットして新たに検索
            _humanoidAnimator = null;
            _headTransform = null;
            _hairReparented = false;

            // カスタマイズが適用されるのを待つ（最大2秒）
            float timeout = 2f;
            float elapsed = 0f;

            while (elapsed < timeout)
            {
                // Humanoid Animatorが見つかったら弓と髪をセットアップ
                if (_humanoidAnimator == null)
                {
                    _humanoidAnimator = FindHumanoidAnimator();
                    if (_humanoidAnimator != null)
                    {
                        Debug.Log($"[AIPlayerController] AI {_aiId}: SetupBowToHandDelayed - Found humanoid animator: {_humanoidAnimator.gameObject.name}");
                    }
                    else if (elapsed < 0.2f) // 最初の数回だけログ
                    {
                        Debug.Log($"[AIPlayerController] AI {_aiId}: SetupBowToHandDelayed - Searching for humanoid animator... (elapsed={elapsed:F1}s)");
                    }
                }

                if (_humanoidAnimator != null)
                {
                    Debug.Log($"[AIPlayerController] AI {_aiId}: SetupBowToHandDelayed - Calling SetupBowToHand and ReparentHairToHead");
                    SetupBowToHand();
                    ReparentHairToHead();
                    Debug.Log($"[AIPlayerController] AI {_aiId}: SetupBowToHandDelayed - COMPLETED. BowFirePoint={(_bowFirePoint != null ? _bowFirePoint.name : "NULL")}");
                    yield break;
                }

                yield return new WaitForSeconds(0.1f);
                elapsed += 0.1f;
            }

            Debug.LogWarning($"[AIPlayerController] AI {_aiId}: SetupBowToHandDelayed - Timeout, humanoid animator not found");

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
            Debug.Log($"[AIPlayerController] AI {_aiId}: SetupBowToHand() called");

            if (_humanoidAnimator == null)
            {
                _humanoidAnimator = FindHumanoidAnimator();
                if (_humanoidAnimator == null)
                {
                    Debug.LogWarning($"[AIPlayerController] AI {_aiId}: SetupBowToHand - Humanoid Animator not found");
                    return;
                }
            }

            Debug.Log($"[AIPlayerController] AI {_aiId}: SetupBowToHand - Using humanoid animator: {_humanoidAnimator.gameObject.name}");

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
                        Debug.LogWarning($"[AIPlayerController] AI {_aiId}: ReparentHairToHead - Humanoid animator not found");
                        return;
                    }
                }
                _headTransform = _humanoidAnimator.GetBoneTransform(HumanBodyBones.Head);
                if (_headTransform == null)
                {
                    Debug.LogWarning($"[AIPlayerController] AI {_aiId}: ReparentHairToHead - Head bone not found on {_humanoidAnimator.gameObject.name}");
                    return;
                }
                Debug.Log($"[AIPlayerController] AI {_aiId}: ReparentHairToHead - Head bone found: {_headTransform.name} on {_humanoidAnimator.gameObject.name}");
            }

            // P09モデルのルートを取得
            Transform? riderRoot = _humanoidAnimator?.transform;
            if (riderRoot == null)
            {
                return;
            }

            // 髪オブジェクトを検索して再配置（PlayerControllerと同じシンプルな方式）
            var allChildren = riderRoot.GetComponentsInChildren<Transform>(true);
            int hairCount = 0;

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

                    hairCount++;
                }
            }

            if (hairCount > 0)
            {
                Debug.Log($"[AIPlayerController] AI {_aiId}: ReparentHairToHead - Reparented {hairCount} hair object(s) to {_headTransform.name}");
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
            // P09の人間型Animatorを優先使用
            Animator? animToUse = _humanoidAnimator ?? _animator;
            if (animToUse == null || animToUse.runtimeAnimatorController == null)
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
            animToUse.SetFloat(SpeedParam, speed);

            // チャージ中はChargeAmountも更新
            if (_isCharging)
            {
                animToUse.SetFloat(ChargeAmountParam, _currentCharge);
                animToUse.SetBool(IsAimingParam, true);
            }
            else
            {
                animToUse.SetBool(IsAimingParam, false);
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
