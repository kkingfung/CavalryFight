#nullable enable

using System;
using System.Collections;
using System.Text.RegularExpressions;
using CavalryFight.Core.Services;
using CavalryFight.Services.Audio;
using CavalryFight.Services.Combat;
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
        private IArrowTrackerService? _arrowTrackerService;

        // BlazeAI関連（グローバル名前空間、存在しない場合はnull）
        private MonoBehaviour? _blazeAI;

        // Malbers MAnimal（馬の移動制御）
        private MalbersAnimations.Controller.MAnimal? _mAnimal;
        private MalbersAnimations.Controller.AI.MAnimalAIControl? _mAnimalAIControl;

        // MAnimalBrain（Malbers AI）が存在する場合、移動はMAnimalBrainに委譲
        private MonoBehaviour? _mAnimalBrain;
        private bool _useMAnimalBrainForMovement;

        // 後退用ウェイポイント（SetTargetで使用するため実体が必要）
        private GameObject? _retreatWaypoint;

        // 上半身のエイム回転用（RiderArcherControllerと同様）
        [Header("Spine Rotation (Optional - auto-detected if not assigned)")]
        [SerializeField] private Transform? _spineTransform;
        private Transform? _chestTransform;  // 胸ボーン（Spineだけでは回転が見えにくいため追加）
        private Transform? _headTransform;
        private Quaternion _originalSpineRotation = Quaternion.identity;  // 初期回転を保存
        private Quaternion _originalChestRotation = Quaternion.identity;  // 胸の初期回転
        private Quaternion _currentAppliedSpineRotation = Quaternion.identity;  // 現在適用中の回転（Animator上書き対策）
        private Quaternion _currentAppliedChestRotation = Quaternion.identity;  // 胸の現在適用中の回転
        private float _aimRotationSpeed = 30f; // 上半身回転速度（AIは動的なターゲットを追跡するため高速に）

        // PlayerControllerと同じ方式: 角度値でスムーズに補間
        private float _currentSpineYRotation = 0f;  // 現在のY軸回転角度（追加回転、0で元ポーズ維持）
        private float _currentSpineXRotation = 0f;  // 現在のX軸回転角度（垂直）
        private const float SpineRotationSpeed = 5f;  // 角度補間速度

        private bool _hairReparented = false;

        // RiderController（アニメーション制御用）
        private MonoBehaviour? _riderController;
        private System.Reflection.MethodInfo? _setChargeAmountMethod;
        private System.Reflection.MethodInfo? _setAnimationStateMethod;
        private System.Type? _riderAnimationStateType;

        // 上半身の回転制限（PlayerCameraControllerのAim角度に合わせる）
        // 水平は非対称: SignedAngle正=左(counterclockwise), 負=右(clockwise)
        // 左側射撃なので左方向を大きく、右方向を小さく
        private const float MinHorizontalRotation = -45f;   // 右方向の制限 (45° clockwise)
        private const float MaxHorizontalRotation = 125f;   // 左方向の制限 (125° counterclockwise)
        // 垂直は非対称: -30（下）〜 +60（上）
        private const float MinVerticalRotation = -30f;     // 下方向の制限
        private const float MaxVerticalRotation = 60f;      // 上方向の制限

        // 騎馬弓兵の理想的な射撃角度（ターゲットが馬の左側にいる状態）
        // SignedAngle正=左なので、+60°〜+90°がスイートスポット
        private const float IdealShootingAngle = 75f;       // 理想的な角度（左75°）
        private const float ShootingAngleMargin = 30f;      // 許容マージン（±30°）

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
        private float _retreatDuration; // TakeDamageで設定された撤退時間を保持
        private float _nextPatrolCheckTime; // 次のパトロールウェイポイント更新時間
        private Vector3 _lastWaypointTargetPosition; // 最後にウェイポイントを作成した時のターゲット位置
        private float _lastWaypointUpdateTime; // 最後にウェイポイントを更新した時刻
        private int _lastLookAtTargetFrame; // 最後にLookAtTarget()を呼んだフレーム（重複呼び出し防止）

        // === 拡張AI機能 ===

        // 予測射撃用（ターゲット速度追跡）
        private Vector3 _previousTargetPosition;
        private Vector3 _targetVelocity;
        private float _velocityUpdateTimer;
        private const float VelocityUpdateInterval = 0.1f;

        // フェイント行動
        private bool _isFeinting;
        private float _feintEndTime;
        private const float FeintDuration = 0.5f;

        // 回避機動
        private bool _isDodging;
        private float _dodgeEndTime;
        private Vector3 _dodgeDirection;
        private float _nextDodgeCheckTime;
        private const float DodgeCheckInterval = 0.2f;
        private const float DodgeDuration = 0.8f;

        // 脅威評価
        private float _nextThreatAssessmentTime;
        private System.Collections.Generic.List<GameObject> _trackedEnemies = new();

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
            // Servicesを取得
            _audioService = ServiceLocator.Instance.Get<IAudioService>();
            _arrowTrackerService = ServiceLocator.Instance.Get<IArrowTrackerService>();
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

            // ターゲット速度の更新（予測射撃用）
            UpdateTargetVelocity();

            // 回避機動の更新
            UpdateDodge();

            // フェイントの更新
            UpdateFeint();

            // ★馬のアニメーション状態チェック（滑り防止）
            EnsureHorseAnimation();

            UpdateStateMachine();
        }

        /// <summary>
        /// ターゲットの速度を更新します（予測射撃用）
        /// </summary>
        private void UpdateTargetVelocity()
        {
            if (_currentTarget == null)
            {
                _targetVelocity = Vector3.zero;
                return;
            }

            _velocityUpdateTimer += Time.deltaTime;
            if (_velocityUpdateTimer >= VelocityUpdateInterval)
            {
                Vector3 currentPos = _currentTarget.transform.position;
                if (_previousTargetPosition != Vector3.zero)
                {
                    _targetVelocity = (currentPos - _previousTargetPosition) / _velocityUpdateTimer;
                }
                _previousTargetPosition = currentPos;
                _velocityUpdateTimer = 0f;
            }
        }

        /// <summary>
        /// 回避機動を更新します
        /// </summary>
        private void UpdateDodge()
        {
            // 回避中の処理
            if (_isDodging)
            {
                if (Time.time > _dodgeEndTime)
                {
                    _isDodging = false;
                }
                return;
            }

            // 回避チェック間隔
            if (Time.time < _nextDodgeCheckTime)
            {
                return;
            }
            _nextDodgeCheckTime = Time.time + DodgeCheckInterval;

            // 回避効果が低い場合はスキップ
            if (_difficultySettings.DodgeEffectiveness < 0.1f)
            {
                return;
            }

            // 接近する矢を検出
            if (DetectIncomingArrow(out Vector3 arrowDirection, out float arrowDistance))
            {
                // 回避確率判定
                if (UnityEngine.Random.value < _difficultySettings.DodgeEffectiveness)
                {
                    StartDodge(arrowDirection);
                }
            }
        }

        /// <summary>
        /// 接近する矢を検出します
        /// </summary>
        /// <param name="arrowDirection">矢の方向（出力）</param>
        /// <param name="arrowDistance">矢までの距離（出力）</param>
        /// <returns>矢が検出された場合true</returns>
        private bool DetectIncomingArrow(out Vector3 arrowDirection, out float arrowDistance)
        {
            arrowDirection = Vector3.zero;
            arrowDistance = float.MaxValue;

            // ArrowTrackerServiceから矢のリストを取得
            if (_arrowTrackerService == null)
            {
                return false;
            }

            var arrows = _arrowTrackerService.ActiveArrows;
            if (arrows.Count == 0)
            {
                return false;
            }

            Vector3 myPosition = transform.position + Vector3.up; // 胸の高さ

            foreach (var arrowTransform in arrows)
            {
                if (arrowTransform == null) continue;

                Vector3 arrowPos = arrowTransform.position;
                float distance = Vector3.Distance(arrowPos, myPosition);

                // 検出距離内か
                if (distance > _difficultySettings.DodgeTriggerDistance)
                {
                    continue;
                }

                // 矢が自分に向かっているか（Rigidbodyの速度で判定）
                var rb = arrowTransform.GetComponent<Rigidbody>();
                if (rb == null) continue;

                Vector3 velocity = rb.linearVelocity;
                if (velocity.sqrMagnitude < 1f) continue;

                Vector3 toMe = (myPosition - arrowPos).normalized;
                float dot = Vector3.Dot(velocity.normalized, toMe);

                // 自分に向かっている（dot > 0.5 = 60度以内）
                if (dot > 0.5f && distance < arrowDistance)
                {
                    arrowDistance = distance;
                    arrowDirection = velocity.normalized;
                }
            }

            return arrowDistance < float.MaxValue;
        }

        /// <summary>
        /// 回避機動を開始します
        /// </summary>
        /// <param name="threatDirection">脅威の方向</param>
        private void StartDodge(Vector3 threatDirection)
        {
            _isDodging = true;
            _dodgeEndTime = Time.time + DodgeDuration;

            // 脅威方向に対して垂直に回避（左右どちらかランダム）
            Vector3 perpendicular = Vector3.Cross(threatDirection, Vector3.up).normalized;
            _dodgeDirection = (UnityEngine.Random.value > 0.5f) ? perpendicular : -perpendicular;

            Debug.Log($"[AI-DODGE] AI {_aiId}: Starting dodge! Direction={_dodgeDirection}");

            // MAnimalAIControlに回避方向への移動を指示
            if (_mAnimalAIControl != null && _mountObject != null)
            {
                Vector3 dodgeTarget = _mountObject.transform.position + _dodgeDirection * 5f;
                // 一時的な回避ターゲットを設定
                _mAnimalAIControl.SetDestination(dodgeTarget);
            }
        }

        /// <summary>
        /// フェイント行動を更新します
        /// </summary>
        private void UpdateFeint()
        {
            if (!_isFeinting)
            {
                return;
            }

            if (Time.time > _feintEndTime)
            {
                EndFeint();
            }
        }

        /// <summary>
        /// 馬のアニメーション状態を確認し、移動中なのにIdleの場合は修正します（滑り防止）
        /// </summary>
        private void EnsureHorseAnimation()
        {
            if (_mAnimalAIControl == null || _mAnimal == null || _mountObject == null)
            {
                return;
            }

            // MAnimalAIControlが移動中かチェック
            bool shouldBeMoving = _mAnimalAIControl.IsMoving || !_mAnimalAIControl.HasArrived;

            // NavMeshAgentもチェック
            var navAgent = _mountObject.GetComponentInChildren<NavMeshAgent>();
            if (navAgent != null && navAgent.enabled && navAgent.isOnNavMesh)
            {
                // NavMeshAgentに有効なパスがあるか、速度が0でない場合は移動中
                bool navAgentMoving = navAgent.hasPath || navAgent.pathPending || navAgent.velocity.sqrMagnitude > 0.01f;
                shouldBeMoving = shouldBeMoving || navAgentMoving;
            }

            // 馬が移動すべきなのにIdleの場合、MAnimalAIControlに移動を促す
            if (shouldBeMoving && _mAnimal.HorizontalSpeed < 0.1f)
            {
                // ActiveStateがIdleの場合
                if (_mAnimal.ActiveState != null && _mAnimal.ActiveState.name.Contains("Idle"))
                {
                    if (Time.frameCount % 120 == 0) // 2秒ごとにログ
                    {
                        Debug.LogWarning($"[AI-ANIM] AI {_aiId}: Horse should be moving but is Idle! Forcing Move()");
                    }

                    // MAnimalAIControlに移動を再指示
                    _mAnimalAIControl.Move();
                }
            }
        }

        /// <summary>
        /// フェイント（偽の射撃モーション）を開始します
        /// </summary>
        private void StartFeint()
        {
            if (_isFeinting || _isCharging)
            {
                return;
            }

            _isFeinting = true;
            _feintEndTime = Time.time + FeintDuration;

            Debug.Log($"[AI-FEINT] AI {_aiId}: Starting feint!");

            // エイムアニメーションを開始（フェイク）
            Animator? animToUse = _humanoidAnimator ?? _animator;
            if (animToUse != null && animToUse.runtimeAnimatorController != null)
            {
                animToUse.SetBool(IsAimingParam, true);
            }
        }

        /// <summary>
        /// フェイントを終了します
        /// </summary>
        private void EndFeint()
        {
            _isFeinting = false;

            // アニメーションをリセット
            Animator? animToUse = _humanoidAnimator ?? _animator;
            if (animToUse != null && animToUse.runtimeAnimatorController != null)
            {
                animToUse.SetBool(IsAimingParam, false);
            }

            Debug.Log($"[AI-FEINT] AI {_aiId}: Feint ended");
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

            // 馬の位置を監視（地面に埋まる問題の診断）
            if (_mountObject != null)
            {
                var mountY = _mountObject.transform.position.y;
                if (mountY < -1f)
                {
                    Debug.LogError($"[AI-PERIODIC] AI {_aiId}: ★★★ MOUNT IS BELOW GROUND! Y={mountY:F2} ★★★");
                }
            }

            // MAnimalの状態
            if (_mAnimal != null)
            {
                Debug.Log($"[AI-PERIODIC] AI {_aiId}: MAnimal - Grounded={_mAnimal.Grounded}, HSpeed={_mAnimal.HorizontalSpeed:F2}, ActiveState={_mAnimal.ActiveState?.name ?? "NULL"}");
            }

            // MAnimalAIControlの状態
            if (_mAnimalAIControl != null)
            {
                string aiTarget = _mAnimalAIControl.Target != null ? _mAnimalAIControl.Target.name : "NULL";
                Debug.Log($"[AI-PERIODIC] AI {_aiId}: MAnimalAIControl - enabled={_mAnimalAIControl.enabled}, Target={aiTarget}, HasArrived={_mAnimalAIControl.HasArrived}, IsMoving={_mAnimalAIControl.IsMoving}, StoppingDist={_mAnimalAIControl.StoppingDistance:F1}m");
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

            // ★重要: Animatorが設定した回転を保存（累積を防ぐ）
            // Animatorは自身のLateUpdateで_spineTransformを更新済み
            // この時点での回転をベースとして使用し、前フレームの修正は含めない
            if (_spineTransform != null)
            {
                _currentAppliedSpineRotation = _spineTransform.rotation;
            }

            // 戦闘中は上半身をターゲット方向に回転
            // チャージ中、Attack状態、またはStrafe状態でターゲットがいる場合にエイム
            // ★重要: _isChargingフラグを追加 - チャージ中は常にターゲットを狙う
            bool shouldAim = _currentTarget != null &&
                (_isCharging || _currentState == AIState.Attack || _currentState == AIState.Strafe);

            // デバッグログ（2秒ごと）
            _lateUpdateLogTimer += Time.deltaTime;
            if (_lateUpdateLogTimer >= 2f)
            {
                _lateUpdateLogTimer = 0f;
                Debug.Log($"[AI-SPINE] AI {_aiId}: LateUpdate - shouldAim={shouldAim}, target={(_currentTarget != null ? _currentTarget.name : "NULL")}, state={_currentState}, isCharging={_isCharging}, spine={(_spineTransform != null)}");
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
        // 一度だけログ出力するためのフラグ
        private bool _spineRotationLoggedOnce = false;

        private void RotateSpineTowardTarget()
        {
            // 初回呼び出し時に状態をログ（一度だけ）
            if (!_spineRotationLoggedOnce)
            {
                _spineRotationLoggedOnce = true;
                Debug.Log($"[AI-SPINE] AI {_aiId}: ★★★ RotateSpineTowardTarget FIRST CALL - spine={(_spineTransform != null)}, target={(_currentTarget != null ? _currentTarget.name : "NULL")}, mount={(_mountObject != null)}, humanoidAnim={(_humanoidAnimator != null)}");
            }

            // ★スパイン遅延初期化: humanoidAnimatorがあるがspineがない場合は再取得を試みる
            if (_spineTransform == null && _humanoidAnimator != null)
            {
                Debug.Log($"[AI-SPINE] AI {_aiId}: Attempting LAZY init of spine transform...");
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
                    else
                    {
                        Debug.LogError($"[AI-SPINE] AI {_aiId}: ★★★ LAZY INIT FAILED - spine still NULL!");
                    }
                }
                catch (System.Exception e)
                {
                    Debug.LogError($"[AI-SPINE] AI {_aiId}: LAZY INIT exception: {e.Message}");
                }
            }

            if (_spineTransform == null || _currentTarget == null || _mountObject == null)
            {
                // デバッグ: なぜ回転しないかを確認（毎秒ログ）
                if (Time.frameCount % 60 == 0) // 1秒ごと
                {
                    Debug.LogError($"[AI-SPINE] AI {_aiId}: ★★★ RotateSpine SKIPPED! spine={(_spineTransform != null)}, target={(_currentTarget != null)}, mount={(_mountObject != null)}, humanoidAnim={(_humanoidAnimator != null)}");
                }
                return;
            }

            // ========== localRotationベースの回転 ==========
            // _originalSpineRotationを基準に追加回転を適用（累積を防ぐ）

            // ★水平方向: 馬の位置を基準にして角度計算（LookAtTargetと一致させる）
            Vector3 mountPos = _mountObject.transform.position;
            Vector3 targetRootPos = _currentTarget.transform.position;
            Vector3 horizontalDir = targetRootPos - mountPos;
            horizontalDir.y = 0;

            // ★垂直方向: 実際のspineボーンの位置からターゲットの胸を狙う
            Vector3 spinePos = _spineTransform.position;  // spineボーンのワールド位置
            // ターゲットも騎乗している可能性があるので、適度なオフセット
            Vector3 targetChestPos = _currentTarget.transform.position + Vector3.up * 0.5f;
            Vector3 direction = targetChestPos - spinePos;

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
            float rawAngleY = Vector3.SignedAngle(mountForward, horizontalDir, Vector3.up);

            // 制限を適用（非対称: 右-45° 〜 左+125°）
            float clampedAngleY = Mathf.Clamp(rawAngleY, MinHorizontalRotation, MaxHorizontalRotation);

            // ターゲット角度（オフセットなし - 直接の角度差を使用）
            float targetAngleY = clampedAngleY;

            // 垂直角度を計算（spineからターゲットへの高低差）
            // PlayerControllerと同じ規則: 負=下向き、正=上向き
            float distanceXZ = new Vector3(direction.x, 0, direction.z).magnitude;
            float heightDiff = targetChestPos.y - spinePos.y;
            // atan2: target上→正, target下→負 → そのまま使用（PlayerControllerと一致）
            float targetAngleX = Mathf.Atan2(heightDiff, distanceXZ) * Mathf.Rad2Deg;
            // 垂直は非対称: -30°（下）〜 +60°（上）
            targetAngleX = Mathf.Clamp(targetAngleX, MinVerticalRotation, MaxVerticalRotation);

            // スムーズに目標角度に近づける
            _currentSpineYRotation = Mathf.LerpAngle(_currentSpineYRotation, targetAngleY, SpineRotationSpeed * Time.deltaTime);
            _currentSpineXRotation = Mathf.LerpAngle(_currentSpineXRotation, targetAngleX, SpineRotationSpeed * Time.deltaTime);

            // ★ワールドY軸で水平回転、spineのright軸で垂直回転
            // これにより、キャラクターの向きに関係なく正しくエイムできる
            // ★修正: _currentAppliedSpineRotationを使用（LateUpdate開始時に保存したAnimatorの回転）
            // これにより前フレームの修正が累積することを防ぐ
            Quaternion baseRotation = _currentAppliedSpineRotation;

            // 水平回転: ワールドY軸（垂直軸）周りに回転
            Quaternion yawRotation = Quaternion.AngleAxis(_currentSpineYRotation, Vector3.up);

            // 垂直回転: spineのright軸周りに回転（上下を向く）
            Vector3 spineRight = baseRotation * Vector3.right;
            Quaternion pitchRotation = Quaternion.AngleAxis(_currentSpineXRotation, spineRight);

            // 回転を適用: pitch * yaw * base
            _spineTransform.rotation = pitchRotation * yawRotation * baseRotation;

            // 2秒ごとに回転状態をログ出力
            if (Time.frameCount % 120 == 0)
            {
                var worldRot = _spineTransform.rotation.eulerAngles;
                Debug.Log($"[AI-SPINE] AI {_aiId}: rawY={rawAngleY:F1}°, targetY={targetAngleY:F1}°, currentY={_currentSpineYRotation:F1}°");
                Debug.Log($"[AI-SPINE] AI {_aiId}: targetX={targetAngleX:F1}°, currentX={_currentSpineXRotation:F1}°, heightDiff={heightDiff:F2}m");
                Debug.Log($"[AI-SPINE] AI {_aiId}: worldRot=({worldRot.x:F0},{worldRot.y:F0},{worldRot.z:F0}), additionalRot=({_currentSpineXRotation:F1},{_currentSpineYRotation:F1})");
            }
        }

        /// <summary>
        /// 上半身の回転を徐々にリセットします
        /// </summary>
        /// <remarks>
        /// PlayerControllerと同じ: 角度を0に戻す（オフセットなし）
        /// </remarks>
        private void ResetSpineRotation()
        {
            if (_spineTransform == null)
            {
                return;
            }

            // 0に戻す（徐々に減衰）
            _currentSpineYRotation = Mathf.LerpAngle(_currentSpineYRotation, 0f, SpineRotationSpeed * 2f * Time.deltaTime);
            _currentSpineXRotation = Mathf.LerpAngle(_currentSpineXRotation, 0f, SpineRotationSpeed * 2f * Time.deltaTime);

            // ★RotateSpineTowardTargetと同じ方式でリセット
            // 角度が0に近づくと追加回転も0に近づき、自然にAnimatorの回転のみになる
            if (Mathf.Abs(_currentSpineYRotation) > 0.1f || Mathf.Abs(_currentSpineXRotation) > 0.1f)
            {
                // ★修正: _currentAppliedSpineRotationを使用（前フレームの修正累積を防ぐ）
                Quaternion baseRotation = _currentAppliedSpineRotation;

                // 水平回転: ワールドY軸周りに回転
                Quaternion yawRotation = Quaternion.AngleAxis(_currentSpineYRotation, Vector3.up);

                // 垂直回転: spineのright軸周りに回転
                Vector3 spineRight = baseRotation * Vector3.right;
                Quaternion pitchRotation = Quaternion.AngleAxis(_currentSpineXRotation, spineRight);

                _spineTransform.rotation = pitchRotation * yawRotation * baseRotation;
            }
            // 角度が十分小さければ何もしない（Animatorの回転をそのまま使用）
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
            Debug.Log($"[AI-SPINE] AI {_aiId}: InitializeSpineTransform() called");

            // 既にInspectorで設定されている場合はスキップ
            if (_spineTransform != null)
            {
                _originalSpineRotation = _spineTransform.localRotation;
                _currentAppliedSpineRotation = _originalSpineRotation;
                Debug.Log($"[AI-SPINE] AI {_aiId}: ★★★ SPINE PRE-ASSIGNED: {_spineTransform.name}, rot=({_originalSpineRotation.eulerAngles.x:F0},{_originalSpineRotation.eulerAngles.y:F0},{_originalSpineRotation.eulerAngles.z:F0})");

                // Humanoid Animatorも取得（他の用途のため）
                _humanoidAnimator = FindHumanoidAnimator();
                return;
            }

            // P09モデルのAnimatorを探す（Humanoid Avatarを持つもの）
            _humanoidAnimator = FindHumanoidAnimator();
            if (_humanoidAnimator == null)
            {
                Debug.LogWarning($"[AI-SPINE] AI {_aiId}: ★★★ Humanoid Animator NOT FOUND - spine rotation will NOT work!");
                return;
            }

            Debug.Log($"[AI-SPINE] AI {_aiId}: Humanoid animator = {_humanoidAnimator.gameObject.name}");

            // Avatarの状態を確認
            bool isHuman = _humanoidAnimator.isHuman;
            bool hasAvatar = _humanoidAnimator.avatar != null;
            Debug.Log($"[AI-SPINE] AI {_aiId}: isHuman={isHuman}, hasAvatar={hasAvatar}");

            if (!isHuman || !hasAvatar)
            {
                Debug.LogError($"[AI-SPINE] AI {_aiId}: ★★★ NOT Humanoid or no Avatar! Spine rotation will NOT work!");
                return;
            }

            // Humanoid AnimatorからSpineボーンとHeadボーンを取得
            try
            {
                _spineTransform = _humanoidAnimator.GetBoneTransform(HumanBodyBones.Spine);
                Debug.Log($"[AI-SPINE] AI {_aiId}: GetBoneTransform(Spine) = {(_spineTransform != null ? _spineTransform.name : "NULL")}");

                if (_spineTransform == null)
                {
                    _spineTransform = _humanoidAnimator.GetBoneTransform(HumanBodyBones.Chest);
                    Debug.Log($"[AI-SPINE] AI {_aiId}: GetBoneTransform(Chest) = {(_spineTransform != null ? _spineTransform.name : "NULL")}");
                }

                _headTransform = _humanoidAnimator.GetBoneTransform(HumanBodyBones.Head);

                if (_spineTransform != null)
                {
                    // 初期回転を保存（RiderArcherControllerと同様）
                    _originalSpineRotation = _spineTransform.localRotation;
                    // 現在適用中の回転を初期化（Animator上書き対策）
                    _currentAppliedSpineRotation = _originalSpineRotation;
                    Debug.Log($"[AI-SPINE] AI {_aiId}: ★★★ SPINE FOUND: {_spineTransform.name}, rot=({_originalSpineRotation.eulerAngles.x:F0},{_originalSpineRotation.eulerAngles.y:F0},{_originalSpineRotation.eulerAngles.z:F0})");
                }
                else
                {
                    Debug.LogError($"[AI-SPINE] AI {_aiId}: ★★★ SPINE IS NULL! Spine rotation will NOT work!");
                }

                if (_headTransform != null)
                {
                    Debug.Log($"[AI-SPINE] AI {_aiId}: Head = {_headTransform.name}");
                }
            }
            catch (System.InvalidOperationException e)
            {
                Debug.LogError($"[AI-SPINE] AI {_aiId}: ★★★ Avatar error: {e.Message}");
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

        // 現在の矢タイプ（カスタマイズから設定）
        private CavalryFight.Services.Customization.ArrowType _currentArrowType = CavalryFight.Services.Customization.ArrowType.Arrow;
        private CavalryFight.Services.Customization.ArrowTypeConfig? _arrowTypeConfig;

        /// <summary>
        /// 矢タイプを設定します（カスタマイズから呼ばれる）
        /// </summary>
        /// <param name="arrowType">設定する矢タイプ</param>
        public void SetArrowType(CavalryFight.Services.Customization.ArrowType arrowType)
        {
            _currentArrowType = arrowType;
            Debug.Log($"[AI-COMBAT] AI {_aiId}: SetArrowType({arrowType})");

            // ArrowTypeConfigを取得（キャッシュ）
            if (_arrowTypeConfig == null)
            {
                _arrowTypeConfig = Resources.Load<CavalryFight.Services.Customization.ArrowTypeConfig>("ArrowTypeConfig");
                if (_arrowTypeConfig == null)
                {
                    _arrowTypeConfig = Resources.Load<CavalryFight.Services.Customization.ArrowTypeConfig>("Settings/ArrowTypeConfig");
                }
            }

            if (_arrowTypeConfig != null)
            {
                _arrowPrefab = _arrowTypeConfig.GetArrowPrefab(arrowType);
                if (_arrowPrefab != null)
                {
                    Debug.Log($"[AI-COMBAT] AI {_aiId}: Arrow prefab set to: {_arrowPrefab.name} for type {arrowType}");
                }
                else
                {
                    Debug.LogWarning($"[AI-COMBAT] AI {_aiId}: No arrow prefab for type {arrowType}, using default");
                    _arrowPrefab = _arrowTypeConfig.GetArrowPrefab(CavalryFight.Services.Customization.ArrowType.Arrow);
                }
            }
        }

        /// <summary>
        /// 矢プレハブを自動設定します（未設定の場合）
        /// </summary>
        /// <remarks>
        /// ArrowTypeConfigから矢プレハブを取得します（PlayerControllerと同じ方式）。
        /// カスタマイズでSetArrowTypeが呼ばれた場合はそちらが優先されます。
        /// </remarks>
        private void InitializeArrowPrefab()
        {
            Debug.Log($"[AI-COMBAT] AI {_aiId}: InitializeArrowPrefab() called");

            // 既に設定されている場合はスキップ（SetArrowTypeで設定済みの可能性）
            if (_arrowPrefab != null)
            {
                Debug.Log($"[AI-COMBAT] AI {_aiId}: Arrow prefab already set: {_arrowPrefab.name}");
                return;
            }

            // ArrowTypeConfigから取得（PlayerControllerと同じ方式）
            if (_arrowTypeConfig == null)
            {
                _arrowTypeConfig = Resources.Load<CavalryFight.Services.Customization.ArrowTypeConfig>("ArrowTypeConfig");
                if (_arrowTypeConfig == null)
                {
                    _arrowTypeConfig = Resources.Load<CavalryFight.Services.Customization.ArrowTypeConfig>("Settings/ArrowTypeConfig");
                }
            }

            if (_arrowTypeConfig != null)
            {
                // 現在の矢タイプのプレハブを取得
                _arrowPrefab = _arrowTypeConfig.GetArrowPrefab(_currentArrowType);
                if (_arrowPrefab != null)
                {
                    Debug.Log($"[AI-COMBAT] AI {_aiId}: Arrow prefab loaded: {_arrowPrefab.name} for type {_currentArrowType}");
                }
                else
                {
                    Debug.LogError($"[AI-COMBAT] AI {_aiId}: Arrow prefab not set in ArrowTypeConfig for type {_currentArrowType}!");
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

            // ★常にRiderArcherControllerのスパイン回転を無効化（両方存在する場合があるため）
            var archerType = System.Type.GetType("CavalryFight.Gameplay.Player.RiderArcherController, Assembly-CSharp");
            if (archerType != null)
            {
                var archerController = GetComponent(archerType) as MonoBehaviour;
                if (archerController == null)
                {
                    archerController = GetComponentInChildren(archerType) as MonoBehaviour;
                }

                if (archerController != null)
                {
                    // 一時的に_riderControllerを保存
                    var savedController = _riderController;
                    _riderController = archerController;
                    DisableRiderArcherControllerAiming(archerType);
                    _riderController = savedController;
                }
            }

            // RiderControllerがない場合、RiderArcherControllerを_riderControllerとして使用
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

                        // ★重要: RiderArcherControllerのスパイン回転を無効化（AI用）
                        // RiderArcherControllerはカメラ方向で照準するが、AIにはカメラがないため
                        // AIPlayerControllerがターゲット方向で直接スパインを制御する
                        DisableRiderArcherControllerAiming(archerControllerType);

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
        /// RiderArcherControllerのスパイン回転（照準）を無効化します
        /// </summary>
        /// <remarks>
        /// RiderArcherControllerはカメラ方向でスパインを回転させますが、
        /// AIにはカメラがないため、この機能を無効化してAIPlayerControllerが
        /// ターゲット方向でスパインを制御できるようにします。
        /// </remarks>
        private void DisableRiderArcherControllerAiming(System.Type archerControllerType)
        {
            if (_riderController == null) return;

            try
            {
                // _isAimingフィールドを取得してfalseに設定
                var isAimingField = archerControllerType.GetField("_isAiming",
                    System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

                if (isAimingField != null)
                {
                    isAimingField.SetValue(_riderController, false);
                    Debug.Log($"[AI-COMBAT] AI {_aiId}: ★ RiderArcherController._isAiming set to FALSE (AI spine control enabled)");
                }
                else
                {
                    Debug.LogWarning($"[AI-COMBAT] AI {_aiId}: Could not find _isAiming field on RiderArcherController");
                }

                // _spineTransformもnullに設定してスパイン回転を完全に無効化
                var spineField = archerControllerType.GetField("_spineTransform",
                    System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

                if (spineField != null)
                {
                    spineField.SetValue(_riderController, null);
                    Debug.Log($"[AI-COMBAT] AI {_aiId}: ★ RiderArcherController._spineTransform set to NULL (spine rotation disabled)");
                }
            }
            catch (System.Exception e)
            {
                Debug.LogWarning($"[AI-COMBAT] AI {_aiId}: Failed to disable RiderArcherController aiming: {e.Message}");
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
            // 矢やプロジェクタイルはターゲットにしない
            if (target != null && IsProjectile(target))
            {
                Debug.LogWarning($"[AI-TARGET] AI {_aiId}: Rejecting projectile as target");
                return;
            }

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
        /// オブジェクトがプロジェクタイル（矢など）かどうかを判定します
        /// </summary>
        private bool IsProjectile(GameObject obj)
        {
            if (obj == null) return false;

            // ArrowTrackerServiceに登録されているかチェック
            if (_arrowTrackerService == null) return false;

            var arrows = _arrowTrackerService.ActiveArrows;
            for (int i = 0; i < arrows.Count; i++)
            {
                if (arrows[i] != null && arrows[i].gameObject == obj)
                {
                    return true;
                }
            }
            return false;
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
            // 矢やプロジェクタイルは攻撃者として記録しない
            if (attacker != null && !IsProjectile(attacker))
            {
                _lastAttacker = attacker;
            }

            // 攻撃者をターゲットに設定（攻撃を受けたら常に反撃する）
            if (attacker != null && !IsProjectile(attacker))
            {
                Debug.Log($"[AI-DAMAGE] AI {_aiId}: Hit by {attacker.name}, switching target to attacker");
                SetTarget(attacker);

                // 被弾時は攻撃状態に遷移（Idle/Patrolから即座に反応）
                if (_currentState == AIState.Idle || _currentState == AIState.Patrol)
                {
                    Debug.Log($"[AI-DAMAGE] AI {_aiId}: Transitioning from {_currentState} to Chase");
                    SetState(AIState.Chase);
                }
            }

            // 死亡判定
            if (_currentHealth <= 0)
            {
                // ゲームモードで死亡が許可されているかチェック
                // MatchManagerは異なるアセンブリにあるため、動的にアクセス
                bool canDie = true;
                GameMode? currentGameMode = null;

                // リフレクションでMatchManagerにアクセス
                var matchManagerType = System.Type.GetType("CavalryFight.Gameplay.Match.MatchManager, Assembly-CSharp");
                if (matchManagerType != null)
                {
                    var instanceProperty = matchManagerType.GetProperty("Instance", System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static);
                    var matchManager = instanceProperty?.GetValue(null);

                    if (matchManager != null)
                    {
                        var activeHandlerProperty = matchManagerType.GetProperty("ActiveHandler");
                        var activeHandler = activeHandlerProperty?.GetValue(matchManager);

                        if (activeHandler != null)
                        {
                            var canPlayersDieProperty = activeHandler.GetType().GetProperty("CanPlayersDie");
                            var canPlayersDieValue = canPlayersDieProperty?.GetValue(activeHandler);
                            if (canPlayersDieValue is bool canDieValue)
                            {
                                canDie = canDieValue;
                            }
                        }

                        var currentGameModeProperty = matchManagerType.GetProperty("CurrentGameMode");
                        var gameModeValue = currentGameModeProperty?.GetValue(matchManager);
                        if (gameModeValue is GameMode mode)
                        {
                            currentGameMode = mode;
                        }
                    }
                }

                if (canDie)
                {
                    // 死亡が許可されているゲームモード（Deathmatch）
                    Die(attacker);
                    return;
                }
                else
                {
                    // 死亡が許可されていないゲームモード（Arena, ScoreMatch, TeamFight, Hunting）
                    // 体力を回復してペナルティを付与
                    _currentHealth = _maxHealth;

                    // 一時的な撤退を強制（リスポーン的な挙動）
                    _retreatDuration = 3f;
                    SetState(AIState.Retreat);

                    Debug.Log($"[AIPlayerController] AI {_aiId} took lethal damage but cannot die in {currentGameMode} mode. Respawning with full health.");
                    return;
                }
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
                    _retreatDuration = _modeBehavior?.CanRespawn == true ? 1.5f : 3f; // リスポーンモードでは撤退時間も短く
                    Debug.Log($"[AIPlayerController] AI {_aiId} health low ({healthPercent:P0}), retreating for {_retreatDuration}s (Aggression: {aggressionFactor:F2})");
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
                case AIState.Search:
                    StartSearch();
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
            switch (state)
            {
                case AIState.Attack:
                    // Attack状態を離れる時はチャージをキャンセル（元の動作）
                    CancelCharge();
                    break;
                case AIState.Retreat:
                    // Retreat状態を離れる時はウェイポイントをクリーンアップ
                    if (_retreatWaypoint != null)
                    {
                        UnityEngine.Object.Destroy(_retreatWaypoint);
                        _retreatWaypoint = null;
                        Debug.Log($"[AI-RETREAT] AI {_aiId}: Retreat waypoint destroyed");
                    }
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
                case AIState.Search:
                    UpdateSearch();
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
                // ターゲットが無効（破壊されたなど）になったかチェック
                if (_currentTarget == null || !_currentTarget.activeInHierarchy)
                {
                    Debug.Log($"[AI-TARGET] AI {_aiId}: Target destroyed or inactive, clearing target");
                    _currentTarget = null;
                    SetState(AIState.Patrol);
                    return;
                }

                // ターゲットが視界内か確認
                bool canSeeTarget = CanSeeTarget(_currentTarget);
                float distToTarget = Vector3.Distance(transform.position, _currentTarget.transform.position);

                // ★改善: Chase状態では視界外でも追跡を続ける（遠距離の敵に向かう）
                if (canSeeTarget || _currentState == AIState.Chase || distToTarget <= _attackRange)
                {
                    _lastKnownTargetPosition = _currentTarget.transform.position;
                    _targetLostTime = 0f;

                    if (!canSeeTarget && Time.frameCount % 120 == 0)
                    {
                        Debug.Log($"[AI-TARGET] AI {_aiId}: Tracking target outside vision range ({distToTarget:F1}m), state={_currentState}");
                    }
                }
                else
                {
                    _targetLostTime += Time.deltaTime;

                    // ★改善: 距離が十分近ければ視線チェックを緩和
                    // （乱戦時に一時的に視線が遮られても追跡を続ける）
                    if (distToTarget < _attackRange * 0.5f)
                    {
                        // 近距離ではタイムアウトを延長
                        _targetLostTime = Mathf.Min(_targetLostTime, 3f);
                    }

                    // 一定時間ターゲットを見失ったら探索状態に移行
                    if (_targetLostTime > 10f)
                    {
                        Debug.Log($"[AI-TARGET] AI {_aiId}: Lost sight of target for too long, transitioning to Search");
                        // ターゲットをクリアする前に探索状態に移行
                        // Search状態で最後の目撃位置を調査する
                        SetState(AIState.Search);
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
                else
                {
                    // ★フォールバック: MAnimalAIControlからターゲットを取得
                    // FindNearestEnemyが見つからない場合でも、MAnimalAIControlがターゲットを持っていれば使用
                    if (_mAnimalAIControl != null && _mAnimalAIControl.Target != null)
                    {
                        Transform aiTarget = _mAnimalAIControl.Target;
                        // ターゲットがライダーかどうか確認（馬ではなくライダーをターゲットにする）
                        GameObject? riderTarget = GetRiderFromTarget(aiTarget.gameObject);
                        if (riderTarget != null)
                        {
                            Debug.Log($"[AI-TARGET] AI {_aiId}: Fallback - using MAnimalAIControl target: {riderTarget.name}");
                            SetTarget(riderTarget);
                        }
                        else if (!IsOwnMount(aiTarget.gameObject))
                        {
                            Debug.Log($"[AI-TARGET] AI {_aiId}: Fallback - using MAnimalAIControl target directly: {aiTarget.name}");
                            SetTarget(aiTarget.gameObject);
                        }
                    }
                    else
                    {
                        // ターゲットが見つからない場合、Idle状態ならPatrol状態に移行
                        if (_currentState == AIState.Idle)
                        {
                            Debug.Log($"[AI-TARGET] AI {_aiId}: No target found in Idle state, transitioning to Patrol");
                            SetState(AIState.Patrol);
                        }
                        // Patrol状態でもターゲットを探し続けるが、積極的に移動する（UpdatePatrolで処理）
                    }
                }
            }
        }

        /// <summary>
        /// ターゲットからライダーを取得します
        /// </summary>
        private GameObject? GetRiderFromTarget(GameObject target)
        {
            if (target == null) return null;

            // 子オブジェクトからライダーコンポーネントを探す
            var childComponents = target.GetComponentsInChildren<MonoBehaviour>();
            foreach (var comp in childComponents)
            {
                if (comp == null) continue;
                string typeName = comp.GetType().Name;

                // MRiderコンポーネントを持っているか確認
                if (typeName.Contains("MRider"))
                {
                    return comp.gameObject;
                }

                // PlayerControllerコンポーネントを持っているか確認
                if (typeName == "PlayerController")
                {
                    return comp.gameObject;
                }
            }

            return null;
        }

        /// <summary>
        /// 自分のマウント（馬）かどうか確認
        /// </summary>
        private bool IsOwnMount(GameObject obj)
        {
            if (_mountObject == null) return false;
            return obj == _mountObject || obj.transform.IsChildOf(_mountObject.transform);
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
        /// <remarks>
        /// 騎馬弓兵は上半身を自由に回転できるため、角度チェックは行わない。
        /// 距離と障害物のみでターゲットの可視性を判定する。
        /// </remarks>
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
                // デバッグ: 距離超過でfalse
                Debug.Log($"[AI-VISION] AI {_aiId}: CanSeeTarget=False - out of vision range ({distanceToTarget:F1}m > {_difficultySettings.VisionRange:F1}m)");
                return false;
            }

            // 注意: 角度チェックは削除
            // 騎馬弓兵は上半身を自由に回転させてターゲットを狙えるため、
            // transform.forward（馬の進行方向）に依存した角度チェックは適切ではない

            // 視線チェック（障害物）
            Ray ray = new Ray(transform.position + Vector3.up, directionToTarget.normalized);
            if (Physics.Raycast(ray, distanceToTarget, _obstacleLayers))
            {
                Debug.Log($"[AI-VISION] AI {_aiId}: CanSeeTarget=False - obstacle blocking view");
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

            // ★デバッグ: 毎秒ログ出力
            if (Time.frameCount % 60 == 0)
            {
                Debug.Log($"[AI-FIND] AI {_aiId}: FindNearestEnemy - searchLayers={searchLayers}, range={_difficultySettings.VisionRange:F1}m, found {colliders.Length} colliders");
            }

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

                float distance = Vector3.Distance(transform.position, targetRoot.transform.position);

                // ★改善: 近距離の敵は視線チェックなしで発見可能
                // （乱戦中に一時的に視線が遮られても敵を見つけられる）
                bool canSee = CanSeeTarget(targetRoot);
                bool isCloseEnough = distance < _attackRange * 0.75f;

                if (!canSee && !isCloseEnough)
                {
                    continue;
                }

                validTargets.Add((targetRoot, distance, targetHealth));
            }

            // ★デバッグ: 有効なターゲット数をログ出力
            if (Time.frameCount % 60 == 0)
            {
                Debug.Log($"[AI-FIND] AI {_aiId}: Found {validTargets.Count} valid targets");
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
            // アイドル状態では、ターゲットが見つからない場合はパトロールに移行
            if (_currentTarget == null)
            {
                SetState(AIState.Patrol);
            }
        }

        private void StartPatrol()
        {
            // ランダムなパトロールポイントを設定
            _nextPatrolCheckTime = Time.time;
            Debug.Log($"[AI-PATROL] AI {_aiId}: StartPatrol() called");
        }

        private void UpdatePatrol()
        {
            // 定期的に新しいパトロールポイントに向かう
            if (Time.time >= _nextPatrolCheckTime)
            {
                _nextPatrolCheckTime = Time.time + UnityEngine.Random.Range(5f, 10f);

                // ランダムなパトロールポイントを生成
                if (_mountObject != null)
                {
                    Vector3 randomDir = UnityEngine.Random.insideUnitSphere;
                    randomDir.y = 0; // 水平方向のみ
                    randomDir = randomDir.normalized;
                    Vector3 patrolPos = _mountObject.transform.position + randomDir * UnityEngine.Random.Range(15f, 30f);

                    // NavMesh上の有効な位置を探す
                    if (UnityEngine.AI.NavMesh.SamplePosition(patrolPos, out var hit, 30f, UnityEngine.AI.NavMesh.AllAreas))
                    {
                        patrolPos = hit.position;

                        // ウェイポイント方式で移動
                        if (_retreatWaypoint != null)
                        {
                            UnityEngine.Object.Destroy(_retreatWaypoint);
                        }

                        _retreatWaypoint = new GameObject($"PatrolWaypoint_AI{_aiId}");
                        _retreatWaypoint.transform.position = patrolPos;

                        // MAnimalAIControlにウェイポイントをターゲットとして設定
                        if (_mAnimalAIControl != null)
                        {
                            if (!_mAnimalAIControl.AIReady)
                            {
                                _mAnimalAIControl.StartAI();
                            }

                            _mAnimalAIControl.SetTarget(_retreatWaypoint.transform);
                            Debug.Log($"[AI-PATROL] AI {_aiId}: Moving to patrol waypoint at {patrolPos}, distance={Vector3.Distance(_mountObject.transform.position, patrolPos):F1}m");
                        }
                    }
                    else
                    {
                        Debug.LogWarning($"[AI-PATROL] AI {_aiId}: NavMesh.SamplePosition failed for patrol point");
                    }
                }
            }
        }

        private void StartChase()
        {
            // ★重要: Chase開始時は必ずターゲットを保持（視界外でも）
            if (_currentTarget != null)
            {
                _lastKnownTargetPosition = _currentTarget.transform.position;
                _targetLostTime = 0f;

                Debug.Log($"[AI-CHASE] AI {_aiId}: StartChase - target={_currentTarget.name}, dist={Vector3.Distance(transform.position, _currentTarget.transform.position):F1}m");
            }

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
            bool canSeeTarget = CanSeeTarget(_currentTarget);

            // 攻撃範囲内なら攻撃に遷移
            if (distanceToTarget <= _attackRange)
            {
                SetState(AIState.Attack);
                return;
            }

            // ★視界内の場合：直接ターゲットを追跡
            if (canSeeTarget)
            {
                _lastKnownTargetPosition = _currentTarget.transform.position;

                if (Time.frameCount % 120 == 0)
                {
                    Debug.Log($"[AI-CHASE] AI {_aiId}: Chasing visible target at {distanceToTarget:F1}m");
                }
            }
            // ★視界外の場合：最後に見た位置を記録（ターゲット追跡は継続）
            else
            {
                // ★FIX: ターゲット位置を記録するが、SetMAnimalBrainTargetは呼ばない
                // StartChase()で既に設定されているため、ここで毎フレーム呼ぶとパスがリセットされる
                _lastKnownTargetPosition = _currentTarget.transform.position;

                if (Time.frameCount % 120 == 0)
                {
                    Debug.Log($"[AI-CHASE] AI {_aiId}: Target out of sight ({distanceToTarget:F1}m), continuing chase");
                }
            }
        }

        private void StartAttack()
        {
            _nextAttackTime = Time.time + _difficultySettings.ReactionTime;

            // ★FIX: Attack状態に入る際、SearchWaypointやRetreatWaypointを破棄
            // これにより、馬がターゲットに向かって正しく方向転換できる
            if (_retreatWaypoint != null)
            {
                UnityEngine.Object.Destroy(_retreatWaypoint);
                _retreatWaypoint = null;
                Debug.Log($"[AI-COMBAT] AI {_aiId}: StartAttack - Destroyed search/retreat waypoint");
            }

            // ★FIX: ウェイポイント破棄後、即座にプレイヤーをターゲットに設定
            // これにより、破棄されたウェイポイントを参照し続けることを防ぐ
            if (_currentTarget != null)
            {
                SetMAnimalBrainTarget(_currentTarget);
                Debug.Log($"[AI-COMBAT] AI {_aiId}: StartAttack - Set target to player: {_currentTarget.name}");
            }

            Debug.Log($"[AI-COMBAT] AI {_aiId} StartAttack: ReactionTime={_difficultySettings.ReactionTime:F2}s");
        }

        private void UpdateAttack()
        {
            if (_currentTarget == null)
            {
                Debug.Log($"[AI-COMBAT] AI {_aiId}: Target is NULL, switching to Patrol");
                SetState(AIState.Patrol);
                return;
            }

            // 回避中はチャージを中断
            if (_isDodging)
            {
                if (_isCharging)
                {
                    CancelCharge();
                }
                return;
            }

            // フェイント中は射撃しない
            if (_isFeinting)
            {
                return;
            }

            float distanceToTarget = Vector3.Distance(transform.position, _currentTarget.transform.position);

            // 毎秒距離をログ出力
            if (Time.frameCount % 60 == 0)
            {
                Debug.Log($"[AI-ATTACK] AI {_aiId}: dist={distanceToTarget:F1}m, charging={_isCharging}, range={_attackRange:F1}m");
            }

            // 攻撃範囲外 または 視界範囲外なら追跡に戻る
            if (distanceToTarget > _attackRange)
            {
                Debug.Log($"[AI-COMBAT] AI {_aiId}: Out of range ({distanceToTarget:F1}m > {_attackRange:F1}m), switching to Chase");
                SetState(AIState.Chase);
                return;
            }

            // ★FIX: 視界範囲外なら追跡に戻る
            if (!CanSeeTarget(_currentTarget))
            {
                Debug.Log($"[AI-COMBAT] AI {_aiId}: Target out of vision range, switching to Chase");
                SetState(AIState.Chase);
                return;
            }

            // ★距離維持ロジック: 最適距離を維持
            // 近距離でも攻撃可能にする（弓は接近戦でも使える）
            float optimalMinDistance = _attackRange * 0.2f;  // 5m (25 * 0.2) - 非常に近い場合のみ後退
            float optimalMaxDistance = _attackRange * 0.7f;  // 17.5m (25 * 0.7)

            if (distanceToTarget < optimalMinDistance && !_isCharging)
            {
                // 非常に近すぎる＆チャージ中でない → 後退（Retreat状態へ）
                Debug.Log($"[AI-COMBAT] AI {_aiId}: Too close ({distanceToTarget:F1}m < {optimalMinDistance:F1}m), retreating");
                _retreatDuration = 2.0f;
                SetState(AIState.Retreat);
                return;
            }
            else if (distanceToTarget > optimalMaxDistance)
            {
                // ★FIX: 最適距離より遠い場合でも、射撃角度が悪ければ位置調整が必要
                // "Waiting for better angle" で止まらないように、常に位置調整を行う
                bool canShootAngle = IsTargetInShootingArc();

                if (!canShootAngle)
                {
                    // 射撃角度が悪い → 位置調整しながら近づく
                    LookAtTarget();

                    if (Time.frameCount % 60 == 0)
                    {
                        Debug.Log($"[AI-COMBAT] AI {_aiId}: Too far ({distanceToTarget:F1}m > {optimalMaxDistance:F1}m), positioning while approaching");
                    }
                }
                else
                {
                    // 射撃角度は良い → 直接近づく
                    SetMAnimalBrainTarget(_currentTarget);

                    if (Time.frameCount % 60 == 0)
                    {
                        Debug.Log($"[AI-COMBAT] AI {_aiId}: Too far ({distanceToTarget:F1}m > {optimalMaxDistance:F1}m), approaching directly (angle good)");
                    }
                }
            }
            else
            {
                // 最適距離内（5m～17.5m）→ 位置調整を行う
                // ターゲットの方を向く（位置調整も行う）
                // ★射撃可能角度外の場合はwaypoint設定で馬を移動させる
                // 射撃可能角度内の場合はSetMAnimalBrainTarget(null)で停止させる
                LookAtTarget();
            }

            // ★重要: チャージ中でも位置調整を継続（パルティアンショット）
            // Aimingアニメーションが馬の移動を止めることがあるため、強制的に継続
            if (_isCharging && distanceToTarget >= optimalMinDistance && distanceToTarget <= optimalMaxDistance)
            {
                LookAtTarget();
            }

            // ターゲットが射撃可能な角度にあるかチェック
            bool canShoot = IsTargetInShootingArc();

            // 攻撃タイミング
            if (Time.time >= _nextAttackTime)
            {
                // フェイント判定（チャージ開始前に確率でフェイント）
                if (!_isCharging && !_isFeinting && _difficultySettings.FeintChance > 0f)
                {
                    if (UnityEngine.Random.value < _difficultySettings.FeintChance * Time.deltaTime * 2f)
                    {
                        StartFeint();
                        // フェイント後に次の攻撃時間を少し遅らせる
                        _nextAttackTime = Time.time + FeintDuration + 0.5f;
                        return;
                    }
                }

                if (!_isCharging)
                {
                    // 射撃可能な角度の場合のみチャージ開始
                    if (canShoot)
                    {
                        Debug.Log($"[AI-COMBAT] AI {_aiId}: >>> STARTING CHARGE <<< (target={_currentTarget.name}, dist={distanceToTarget:F1}m)");
                        StartCharge();
                    }
                    else if (Time.frameCount % 60 == 0)
                    {
                        Debug.Log($"[AI-COMBAT] AI {_aiId}: Waiting for better angle to shoot...");
                    }
                }
                else
                {
                    UpdateCharge();
                }
            }

            // ストレイフ判定（チャージ中はしない）
            if (!_isCharging && !_isFeinting && UnityEngine.Random.value < _difficultySettings.StrafeChance * Time.deltaTime)
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

            // ★FIX: プレイヤーが視界範囲外に出た場合、Chase状態に遷移
            if (!CanSeeTarget(_currentTarget))
            {
                Debug.Log($"[AI-STRAFE] AI {_aiId}: Target out of vision range, switching to Chase");
                SetState(AIState.Chase);
                return;
            }

            // 馬のMAnimalBrainがターゲットへの移動を処理
            // ライダーは上半身でターゲットを狙う（LateUpdateで処理）
            LookAtTarget();
        }

        private void StartRetreat()
        {
            // TakeDamageで設定された撤退時間を使用、未設定の場合はランダム値
            float duration = _retreatDuration > 0f ? _retreatDuration : UnityEngine.Random.Range(2f, 4f);
            _strafeEndTime = Time.time + duration;

            // 使用後にリセット（次回のために）
            _retreatDuration = 0f;

            Debug.Log($"[AI-RETREAT] AI {_aiId}: StartRetreat() called, duration={duration:F1}s, target={(_currentTarget != null ? _currentTarget.name : "NULL")}, mount={(_mountObject != null ? _mountObject.name : "NULL")}");

            // ★ターゲットから離れる方向に移動
            if (_currentTarget != null && _mountObject != null)
            {
                Vector3 awayDir = (_mountObject.transform.position - _currentTarget.transform.position).normalized;
                // 横にも少しずれる（真後ろだけでなく）
                awayDir = Quaternion.Euler(0, UnityEngine.Random.Range(-45f, 45f), 0) * awayDir;
                Vector3 retreatPos = _mountObject.transform.position + awayDir * 15f;

                Debug.Log($"[AI-RETREAT] AI {_aiId}: Initial retreatPos={retreatPos}");

                // NavMesh上の有効な位置を探す
                if (UnityEngine.AI.NavMesh.SamplePosition(retreatPos, out var hit, 20f, UnityEngine.AI.NavMesh.AllAreas))
                {
                    retreatPos = hit.position;
                    Debug.Log($"[AI-RETREAT] AI {_aiId}: NavMesh adjusted retreatPos={retreatPos}");
                }
                else
                {
                    Debug.LogWarning($"[AI-RETREAT] AI {_aiId}: NavMesh.SamplePosition failed!");
                }

                // ★ウェイポイント方式で後退（Chaseと同じSetTargetを使う）
                // 1. 既存のウェイポイントを破棄
                if (_retreatWaypoint != null)
                {
                    UnityEngine.Object.Destroy(_retreatWaypoint);
                }

                // 2. 新しいウェイポイントを作成
                _retreatWaypoint = new GameObject($"RetreatWaypoint_AI{_aiId}");
                _retreatWaypoint.transform.position = retreatPos;

                // 3. MAnimalAIControlにウェイポイントをターゲットとして設定（Chaseと同じ方式）
                if (_mAnimalAIControl != null)
                {
                    // AIが停止している場合は再開
                    if (!_mAnimalAIControl.AIReady)
                    {
                        _mAnimalAIControl.StartAI();
                        Debug.Log($"[AI-RETREAT] AI {_aiId}: StartAI() called");
                    }

                    // ウェイポイントをターゲットとして設定
                    _mAnimalAIControl.SetTarget(_retreatWaypoint.transform);

                    Debug.Log($"[AI-RETREAT] AI {_aiId}: SetTarget(waypoint at {retreatPos}) called, AIReady={_mAnimalAIControl.AIReady}, IsMoving={_mAnimalAIControl.IsMoving}");
                }
                else
                {
                    Debug.LogError($"[AI-RETREAT] AI {_aiId}: MAnimalAIControl is NULL!");
                }
            }
            else
            {
                Debug.LogWarning($"[AI-RETREAT] AI {_aiId}: Cannot retreat - target or mount is null");
                // ターゲットがない場合はクリア
                SetMAnimalBrainTarget(null);
            }
        }

        private void UpdateRetreat()
        {
            // 一定時間後に攻撃状態に戻る
            if (Time.time > _strafeEndTime)
            {
                Debug.Log($"[AI-RETREAT] AI {_aiId}: Retreat timeout, returning to Attack");

                // ★FIX: Retreat終了時にRetreatWaypointを破棄
                if (_retreatWaypoint != null)
                {
                    UnityEngine.Object.Destroy(_retreatWaypoint);
                    _retreatWaypoint = null;
                    Debug.Log($"[AI-RETREAT] AI {_aiId}: RetreatWaypoint destroyed");
                }

                SetState(AIState.Attack);
                return;
            }

            // 後退中も十分離れたら早めに攻撃に戻る
            if (_currentTarget != null)
            {
                float dist = Vector3.Distance(transform.position, _currentTarget.transform.position);
                // 毎秒ログ出力
                if (Time.frameCount % 60 == 0)
                {
                    Debug.Log($"[AI-RETREAT] AI {_aiId}: Retreating... dist={dist:F1}m, threshold={_attackRange * 0.5f:F1}m");
                }
                if (dist > _attackRange * 0.5f) // 12.5m以上離れたら
                {
                    Debug.Log($"[AI-RETREAT] AI {_aiId}: Retreat complete, distance={dist:F1}m");

                    // ★FIX: Retreat終了時にRetreatWaypointを破棄
                    if (_retreatWaypoint != null)
                    {
                        UnityEngine.Object.Destroy(_retreatWaypoint);
                        _retreatWaypoint = null;
                        Debug.Log($"[AI-RETREAT] AI {_aiId}: RetreatWaypoint destroyed");
                    }

                    SetState(AIState.Attack);
                }
            }
        }

        /// <summary>
        /// 探索状態を開始します（ランダムに移動してターゲットを探す）
        /// </summary>
        private void StartSearch()
        {
            // 次の探索ポイント更新時間を設定
            _nextPatrolCheckTime = Time.time;
            Debug.Log($"[AI-SEARCH] AI {_aiId}: StartSearch() called - will search randomly");
        }

        /// <summary>
        /// 探索状態を更新します（ランダムに移動しながらターゲットを探す）
        /// </summary>
        private void UpdateSearch()
        {
            // ターゲットを探す
            if (_currentTarget == null)
            {
                GameObject? newTarget = FindNearestEnemy();
                if (newTarget != null)
                {
                    Debug.Log($"[AI-SEARCH] AI {_aiId}: Target found during search: {newTarget.name}");
                    SetTarget(newTarget);
                }
            }

            // ターゲットが見つかった場合、Chase状態に移行
            if (_currentTarget != null)
            {
                Debug.Log($"[AI-SEARCH] AI {_aiId}: Target found! Transitioning to Chase");
                SetState(AIState.Chase);
                return;
            }

            // 定期的に新しい探索ポイントに向かう（Patrol状態と同じロジック）
            if (Time.time >= _nextPatrolCheckTime)
            {
                _nextPatrolCheckTime = Time.time + UnityEngine.Random.Range(5f, 10f);

                // ランダムな探索ポイントを生成
                if (_mountObject != null)
                {
                    Vector3 randomDir = UnityEngine.Random.insideUnitSphere;
                    randomDir.y = 0; // 水平方向のみ
                    randomDir = randomDir.normalized;
                    Vector3 searchPos = _mountObject.transform.position + randomDir * UnityEngine.Random.Range(15f, 30f);

                    // NavMesh上の有効な位置を探す
                    if (UnityEngine.AI.NavMesh.SamplePosition(searchPos, out var hit, 30f, UnityEngine.AI.NavMesh.AllAreas))
                    {
                        searchPos = hit.position;

                        // ウェイポイント方式で移動
                        if (_retreatWaypoint != null)
                        {
                            UnityEngine.Object.Destroy(_retreatWaypoint);
                        }

                        _retreatWaypoint = new GameObject($"SearchWaypoint_AI{_aiId}");
                        _retreatWaypoint.transform.position = searchPos;

                        // MAnimalAIControlにウェイポイントをターゲットとして設定
                        if (_mAnimalAIControl != null)
                        {
                            if (!_mAnimalAIControl.AIReady)
                            {
                                _mAnimalAIControl.StartAI();
                            }

                            _mAnimalAIControl.SetTarget(_retreatWaypoint.transform);
                            Debug.Log($"[AI-SEARCH] AI {_aiId}: Moving to random search waypoint at {searchPos}, distance={Vector3.Distance(_mountObject.transform.position, searchPos):F1}m");
                        }
                    }
                    else
                    {
                        Debug.LogWarning($"[AI-SEARCH] AI {_aiId}: NavMesh.SamplePosition failed for search point");
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

                        // デバッグ: StoppingDistanceと実際の距離を確認
                        float actualDist = Vector3.Distance(_mountObject!.transform.position, target.transform.position);

                        // ★詳細デバッグ: NavMeshAgentの状態を常に確認
                        var navAgent = _mountObject.GetComponentInChildren<NavMeshAgent>();

                        // ★重要: NavMeshAgentを確認して強制的に移動を継続
                        // チャージ中（Aimingアニメーション）でも馬が移動できるようにする
                        if (navAgent != null && navAgent.isOnNavMesh)
                        {
                            if (navAgent.isStopped)
                            {
                                navAgent.isStopped = false;
                                if (Time.frameCount % 60 == 0)
                                {
                                    Debug.Log($"[AI-MOVE] AI {_aiId}: ★ NavMeshAgent was stopped, re-enabling movement");
                                }
                            }
                        }

                        // ★重要: MAnimalに直接移動を指示（AIControlが機能しない場合のフォールバック）
                        if (_mAnimal != null && actualDist > _mAnimalAIControl.StoppingDistance)
                        {
                            // MAnimalが Idle 状態で HSpeed=0 の場合、強制的に移動させる
                            if (_mAnimal.HorizontalSpeed < 0.1f && _mAnimal.ActiveState != null &&
                                _mAnimal.ActiveState.name.Contains("Idle"))
                            {
                                // Locomotion状態に遷移させる（歩行を開始）
                                if (Time.frameCount % 60 == 0)
                                {
                                    Debug.LogWarning($"[AI-MOVE] AI {_aiId}: ★ MAnimal stuck in Idle! Forcing movement...");
                                }

                                // MAnimal.Move(Vector3) を呼んで移動を強制
                                Vector3 direction = (target.transform.position - _mountObject.transform.position).normalized;
                                _mAnimal.Move(direction);
                            }
                        }
                        if (navAgent == null)
                        {
                            Debug.LogError($"[AI-MOVE] AI {_aiId}: ★★★ NavMeshAgent NOT FOUND on mount! Cannot move!");
                        }
                        else if (!navAgent.enabled)
                        {
                            Debug.LogWarning($"[AI-MOVE] AI {_aiId}: ★★★ NavMeshAgent DISABLED! Enabling it...");
                            navAgent.enabled = true;
                        }
                        else if (!navAgent.isOnNavMesh)
                        {
                            Debug.LogError($"[AI-MOVE] AI {_aiId}: ★★★ NavMeshAgent NOT ON NAVMESH! Position={_mountObject.transform.position}");
                        }

                        // ★詳細デバッグ: MAnimalAIControlとNavMeshAgentの状態を確認（1秒ごと）
                        if (Time.frameCount % 60 == 0)
                        {
                            string navInfo = "NavAgent=NULL";
                            if (navAgent != null)
                            {
                                navInfo = $"enabled={navAgent.enabled}, onMesh={navAgent.isOnNavMesh}, hasPath={navAgent.hasPath}, pathPending={navAgent.pathPending}, isStopped={navAgent.isStopped}, vel={navAgent.velocity.magnitude:F2}";
                            }

                            Debug.Log($"[AI-MOVE-DEBUG] AI {_aiId}: MAnimalAIControl - AIReady={_mAnimalAIControl.AIReady}, IsMoving={_mAnimalAIControl.IsMoving}, enabled={_mAnimalAIControl.enabled}");
                            Debug.Log($"[AI-MOVE-DEBUG] AI {_aiId}: {navInfo}");

                            if (_mAnimal != null)
                            {
                                Debug.Log($"[AI-MOVE-DEBUG] AI {_aiId}: MAnimal - HSpeed={_mAnimal.HorizontalSpeed:F2}, State={_mAnimal.ActiveState?.name ?? "NULL"}");
                            }
                        }

                        Debug.Log($"[AI-MOVE] AI {_aiId}: SetMAnimalBrainTarget - Target={target.name}, ActualDist={actualDist:F1}m, StoppingDist={_mAnimalAIControl.StoppingDistance:F1}m, HasArrived={_mAnimalAIControl.HasArrived}");

                        // HasArrivedがtrueでも実際には遠い場合、MAnimalAIControlをリセットして再設定
                        if (_mAnimalAIControl.HasArrived && actualDist > _mAnimalAIControl.StoppingDistance + 1f)
                        {
                            Debug.LogWarning($"[AI-MOVE] AI {_aiId}: HasArrived=true but far from target! Resetting MAnimalAIControl");

                            // MAnimalAIControlをリセット（targetをnullにしてから再設定）
                            _mAnimalAIControl.Stop();
                            _mAnimalAIControl.ClearTarget();
                            _mAnimalAIControl.SetTarget(target.transform);
                            _mAnimalAIControl.Move();

                            Debug.Log($"[AI-MOVE] AI {_aiId}: MAnimalAIControl reset complete, target={target.name}");
                        }

                        // ★追加チェック: NavMeshAgentが実際にパスを持っているか確認
                        if (navAgent != null && navAgent.isOnNavMesh)
                        {
                            // NavMeshAgentにパスがない場合、直接SetDestinationを呼ぶ
                            if (!navAgent.hasPath && !navAgent.pathPending)
                            {
                                Debug.LogWarning($"[AI-MOVE] AI {_aiId}: NavMeshAgent has no path! Calling SetDestination directly");

                                // ★FIX: ターゲット位置がNavMesh上にない可能性があるため、
                                // NavMesh.SamplePositionで有効な位置を取得してから設定
                                Vector3 targetPos = target.transform.position;
                                if (UnityEngine.AI.NavMesh.SamplePosition(targetPos, out var hit, 10f, UnityEngine.AI.NavMesh.AllAreas))
                                {
                                    targetPos = hit.position;
                                    navAgent.SetDestination(targetPos);

                                    // MAnimalAIControlも更新
                                    _mAnimalAIControl.Stop();
                                    _mAnimalAIControl.ClearTarget();
                                    _mAnimalAIControl.SetTarget(target.transform);
                                    _mAnimalAIControl.Move();

                                    Debug.Log($"[AI-MOVE] AI {_aiId}: NavMeshAgent.SetDestination({targetPos}) called (sampled from {target.transform.position})");
                                }
                                else
                                {
                                    Debug.LogError($"[AI-MOVE] AI {_aiId}: Failed to find valid NavMesh position near target {target.name} at {target.transform.position}");
                                }
                            }
                        }
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

            // 最小チャージ量を満たしているか確認
            float minCharge = _difficultySettings.MinFireCharge;

            // チャージ完了で発射（または最小チャージを満たして確率で早撃ち）
            if (_currentCharge >= 1f)
            {
                Debug.Log($"[AI-COMBAT] AI {_aiId}: Charge complete! Firing arrow...");
                FireArrow();
            }
            else if (_currentCharge >= minCharge)
            {
                // 最小チャージを満たした後、確率で早撃ち（低難易度AIの特徴）
                // 高難易度AIはフルチャージまで待つ傾向
                float earlyFireChance = (1f - _difficultySettings.ChargeTimeMultiplier) * 0.02f;
                if (UnityEngine.Random.value < earlyFireChance)
                {
                    Debug.Log($"[AI-COMBAT] AI {_aiId}: Early fire at {_currentCharge:P0} charge (min={minCharge:P0})");
                    FireArrow();
                }
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
            // 2点法（root→fire）で矢の物理的な向きを使用
            Vector3 direction;
            if (_arrowRootPoint != null)
            {
                direction = (_bowFirePoint.position - _arrowRootPoint.position).normalized;
            }
            else
            {
                // フォールバック: ターゲット方向を使用
                Vector3 targetAimPos = _currentTarget.transform.position + Vector3.up * 0.5f;
                direction = (targetAimPos - _bowFirePoint.position).normalized;
            }

            Debug.Log($"[AI-ARROW-DIR] AI {_aiId}: firePoint={_bowFirePoint.position}, rootPoint={(_arrowRootPoint != null ? _arrowRootPoint.position.ToString() : "NULL")}, dir={direction}");

            // === 予測射撃（LeadTargetFactor）===
            float leadFactor = _difficultySettings.LeadTargetFactor;
            if (leadFactor > 0f && _targetVelocity.sqrMagnitude > 0.1f)
            {
                // 矢の速度から到達時間を推定
                float estimatedArrowSpeed = Mathf.Lerp(_minArrowSpeed, _maxArrowSpeed, _currentCharge);
                float distanceToTarget = Vector3.Distance(_bowFirePoint.position, _currentTarget.transform.position);
                float timeToHit = distanceToTarget / estimatedArrowSpeed;

                // ターゲットの予測位置を計算
                Vector3 predictedPosition = _currentTarget.transform.position + (_targetVelocity * timeToHit * leadFactor);

                // 予測位置への方向に調整
                Vector3 predictedDirection = (predictedPosition + Vector3.up * 1f - _bowFirePoint.position).normalized;

                // 元の方向と予測方向をブレンド
                direction = Vector3.Slerp(direction, predictedDirection, leadFactor);

                Debug.Log($"[AI-PREDICT] AI {_aiId}: LeadFactor={leadFactor:F2}, TargetVel={_targetVelocity.magnitude:F1}m/s, TimeToHit={timeToHit:F2}s, PredictedPos={predictedPosition}");
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

            Debug.Log($"[AI-ARROW-DIR] AI {_aiId}: FINAL dir={direction}, miss={isMiss}");

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
            Debug.Log($"[AI-ARROW-SPAWN] AI {_aiId}: About to instantiate arrow prefab: {_arrowPrefab.name}");
            GameObject arrowVisual = Instantiate(_arrowPrefab, arrowParent.transform);
            arrowVisual.transform.localPosition = Vector3.zero;
            arrowVisual.transform.localRotation = Quaternion.identity;
            arrowVisual.transform.localScale = Vector3.one * 0.1f; // 矢のスケール

            Debug.Log($"[AI-ARROW-SPAWN] AI {_aiId}: Arrow created - parent={arrowParent.name}, visual={arrowVisual.name}, childCount={arrowVisual.transform.childCount}, scale={arrowVisual.transform.localScale}");

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
        /// ターゲットに対して最適な射撃位置に馬を移動させます
        /// </summary>
        /// <remarks>
        /// 騎馬弓兵は左側から射撃するため、ターゲットが馬の左側（+45°〜+105°）に
        /// 位置するように馬を移動させます。直接ターゲットに向かうのではなく、
        /// ターゲットを周回するような動きをします。
        /// </remarks>
        private void LookAtTarget()
        {
            if (_currentTarget == null || _mountObject == null)
            {
                return;
            }

            // ★FIX: 1フレームに複数回呼ばれるのを防ぐ
            // UpdateAttack, UpdateStrafe, チャージ中など複数箇所から呼ばれるため、
            // 同じフレーム内での重複処理を防ぐ
            if (_lastLookAtTargetFrame == Time.frameCount)
            {
                return;
            }
            _lastLookAtTargetFrame = Time.frameCount;

            // 現在のターゲット角度を計算
            Vector3 toTarget = _currentTarget.transform.position - _mountObject.transform.position;
            toTarget.y = 0;
            if (toTarget.sqrMagnitude < 0.01f)
            {
                return;
            }

            Vector3 mountForward = _mountObject.transform.forward;
            mountForward.y = 0;
            mountForward.Normalize();
            toTarget.Normalize();

            float currentAngle = Vector3.SignedAngle(mountForward, toTarget, Vector3.up);

            // 理想角度範囲: IdealShootingAngle ± ShootingAngleMargin (45°〜105°)
            float minIdealAngle = IdealShootingAngle - ShootingAngleMargin; // 45°
            float maxIdealAngle = IdealShootingAngle + ShootingAngleMargin; // 105°

            // ターゲットが理想角度範囲内にあるかチェック
            bool inIdealArc = currentAngle >= minIdealAngle && currentAngle <= maxIdealAngle;

            if (inIdealArc)
            {
                // 理想角度内 → 停止してその場で攻撃
                // ★FIX: null設定は1回だけで十分（既にnullなら何もしない）
                if (_mAnimalAIControl != null && _mAnimalAIControl.Target != null)
                {
                    SetMAnimalBrainTarget(null);

                    if (Time.frameCount % 60 == 0)
                    {
                        Debug.Log($"[AI-POS] AI {_aiId}: Target in ideal arc ({currentAngle:F1}°), STOPPING movement");
                    }
                }
                else if (Time.frameCount % 120 == 0)
                {
                    Debug.Log($"[AI-POS] AI {_aiId}: Target in ideal arc ({currentAngle:F1}°), already stopped");
                }
            }
            else
            {
                // 理想角度外 → 馬を回り込ませてターゲットを左側に配置
                // ターゲットの周りを時計回り（右回り）に移動することで、ターゲットが左側に来る
                PositionForLeftSideShooting(currentAngle, toTarget);
            }
        }

        /// <summary>
        /// ターゲットが射撃可能な角度範囲内にあるかチェックします
        /// </summary>
        /// <returns>射撃可能な場合はtrue</returns>
        /// <remarks>
        /// 理想角度範囲（+45°〜+105°）内にある場合のみtrueを返します。
        /// 物理的な回転限界（-45°〜+125°）内であれば射撃可能とします。
        /// 理想範囲（45°〜105°）外でも射撃は可能ですが、位置調整は続けます。
        /// </remarks>
        private bool IsTargetInShootingArc()
        {
            if (_currentTarget == null || _mountObject == null)
            {
                return false;
            }

            Vector3 toTarget = _currentTarget.transform.position - _mountObject.transform.position;
            toTarget.y = 0;
            if (toTarget.sqrMagnitude < 0.01f)
            {
                return false;
            }

            Vector3 mountForward = _mountObject.transform.forward;
            mountForward.y = 0;
            mountForward.Normalize();
            toTarget.Normalize();

            float currentAngle = Vector3.SignedAngle(mountForward, toTarget, Vector3.up);

            // 射撃可能範囲: 物理的な回転限界内（-45°〜+125°）
            // この範囲内であれば上半身がターゲットを向けるので射撃可能
            bool inArc = currentAngle >= MinHorizontalRotation && currentAngle <= MaxHorizontalRotation;

            if (Time.frameCount % 120 == 0)
            {
                Debug.Log($"[AI-ARC] AI {_aiId}: angle={currentAngle:F1}°, inArc={inArc} (physical: {MinHorizontalRotation}°〜{MaxHorizontalRotation}°)");
            }

            return inArc;
        }

        /// <summary>
        /// ターゲットが左側に来るように馬を位置取りします
        /// </summary>
        /// <param name="currentAngle">現在のターゲット角度（SignedAngle: 正=左, 負=右）</param>
        /// <param name="toTargetNormalized">ターゲット方向（正規化済み）</param>
        /// <remarks>
        /// 騎馬弓兵は左側から射撃するため、ターゲットが馬の左側（+45°〜+105°）に
        /// 位置するように馬を移動させます。
        ///
        /// 戦略:
        /// - ターゲットを「周回」するために、ターゲットを中心とした円上の理想位置を計算
        /// - 理想位置 = ターゲットから見て、馬が-75°（右後方）にいる位置
        /// - これにより、馬から見るとターゲットが+75°（左前方）に見える
        /// </remarks>
        private void PositionForLeftSideShooting(float currentAngle, Vector3 toTargetNormalized)
        {
            if (_currentTarget == null || _mountObject == null)
            {
                return;
            }

            float distanceToTarget = Vector3.Distance(_mountObject.transform.position, _currentTarget.transform.position);
            Vector3 targetPos = _currentTarget.transform.position;

            // ターゲットを中心とした円周上の理想位置を計算
            // 馬から見てターゲットが+75°（左）に見える位置 = ターゲットから見て馬が-75°（右）にいる
            //
            // 計算方法:
            // 1. ターゲットから馬への方向ベクトルを取得
            // 2. それを理想角度になるまで回転
            // 3. その方向に適切な距離を取った位置がウェイポイント

            // ★FIX: プレイヤーが右側や後ろにいる場合、後退しないように修正
            // currentAngle < 0 (右側) の場合、前進して左側に回り込む
            Vector3 waypointPos;
            float angleDiff = IdealShootingAngle - currentAngle;
            float actualAngleStep = 0f; // スコープ外で使用するため、ここで宣言

            // 角度差が180°を超える場合は逆回りの方が速い
            if (angleDiff > 180f) angleDiff -= 360f;
            if (angleDiff < -180f) angleDiff += 360f;

            // ★プレイヤーが右側にいる場合（負の角度）、プレイヤーに向かって移動
            if (currentAngle < -30f)
            {
                // ★FIX: 馬の現在向きではなく、プレイヤーへの方向を基準に移動
                // プレイヤーに近づきつつ、プレイヤーが左側に来る位置を目指す

                // プレイヤーから見て、馬が右後方にいる理想位置を計算
                // これにより、馬から見るとプレイヤーが左前方(+75°)に見える
                Vector3 fromPlayerToMount = -toTargetNormalized;

                // プレイヤーから見て馬が-75°(右後方)の位置 = 馬から見てプレイヤーが+75°(左前方)
                float idealAngleFromPlayer = -IdealShootingAngle; // -75°
                Vector3 idealDirection = Quaternion.Euler(0, idealAngleFromPlayer, 0) * fromPlayerToMount;

                // 理想距離 = 現在距離の70%（近づく）
                float idealDistance = Mathf.Max(distanceToTarget * 0.7f, _attackRange * 0.5f);
                waypointPos = targetPos + idealDirection * idealDistance;
                actualAngleStep = angleDiff; // ログ用に記録

                if (Time.frameCount % 60 == 0)
                {
                    Debug.Log($"[AI-POS] AI {_aiId}: Player on RIGHT side ({currentAngle:F1}°), moving TOWARD player to get on left (dist={distanceToTarget:F1}m → {idealDistance:F1}m)");
                }
            }
            else
            {
                // プレイヤーが左側にいる場合（正の角度）、円周移動で微調整
                Vector3 fromTargetToMount = -toTargetNormalized;

                // 一度に大きく動きすぎないように制限（徐々に近づく）
                float maxAngleStep = 60f;
                actualAngleStep = Mathf.Clamp(angleDiff, -maxAngleStep, maxAngleStep);

                // ターゲットから見て、馬が移動すべき方向を計算
                Vector3 idealDirection = Quaternion.Euler(0, -actualAngleStep, 0) * fromTargetToMount;

                // 理想位置 = ターゲット位置 + 方向 × 距離（少し近めに設定）
                float idealDistance = Mathf.Max(distanceToTarget * 0.9f, _attackRange * 0.4f);
                waypointPos = targetPos + idealDirection * idealDistance;
            }

            // NavMesh上の有効な位置を探す
            if (UnityEngine.AI.NavMesh.SamplePosition(waypointPos, out var hit, 20f, UnityEngine.AI.NavMesh.AllAreas))
            {
                waypointPos = hit.position;
            }

            // ウェイポイントを作成・更新
            bool isNewWaypoint = false;
            if (_retreatWaypoint == null)
            {
                _retreatWaypoint = new GameObject($"PositionWaypoint_AI{_aiId}");
                _lastWaypointTargetPosition = targetPos;
                _lastWaypointUpdateTime = Time.time;
                isNewWaypoint = true;
            }

            // ★FIX: ウェイポイントを更新すべきかチェック
            // 条件:
            // 1. ターゲットが大きく移動した (> 5m)
            // 2. AIがウェイポイントに到着した
            // 3. 最後の更新から十分時間が経過した (> 2秒) AND 角度が大きくずれている

            bool needsUpdate = isNewWaypoint;

            if (!needsUpdate)
            {
                // ターゲットが移動したかチェック
                float targetMovedDistance = Vector3.Distance(_lastWaypointTargetPosition, targetPos);
                if (targetMovedDistance > 5f)
                {
                    needsUpdate = true;
                    if (Time.frameCount % 60 == 0)
                    {
                        Debug.Log($"[AI-POS] AI {_aiId}: Target moved {targetMovedDistance:F1}m, updating waypoint");
                    }
                }
            }

            if (!needsUpdate && _mAnimalAIControl != null)
            {
                // AIがウェイポイントに到着したかチェック
                float distToWaypoint = Vector3.Distance(_mountObject.transform.position, _retreatWaypoint.transform.position);
                if (distToWaypoint < 2.5f)
                {
                    // 到着したが、まだ理想角度に達していない場合のみ更新
                    if (Mathf.Abs(angleDiff) > 15f)
                    {
                        needsUpdate = true;
                        if (Time.frameCount % 60 == 0)
                        {
                            Debug.Log($"[AI-POS] AI {_aiId}: Arrived at waypoint but angle still off ({currentAngle:F1}° vs {IdealShootingAngle:F0}°), creating next waypoint");
                        }
                    }
                }
            }

            // 最後の手段: 長時間更新されておらず、角度が大きくずれている場合
            if (!needsUpdate && Time.time - _lastWaypointUpdateTime > 3f && Mathf.Abs(angleDiff) > 40f)
            {
                needsUpdate = true;
                if (Time.frameCount % 60 == 0)
                {
                    Debug.Log($"[AI-POS] AI {_aiId}: Waypoint too old and angle far from ideal, forcing update");
                }
            }

            if (needsUpdate)
            {
                _retreatWaypoint.transform.position = waypointPos;
                _lastWaypointTargetPosition = targetPos;
                _lastWaypointUpdateTime = Time.time;

                // MAnimalAIControlにウェイポイントを設定
                SetMAnimalBrainTarget(_retreatWaypoint);

                if (Time.frameCount % 60 == 0)
                {
                    float wpDist = Vector3.Distance(_mountObject.transform.position, waypointPos);
                    Debug.Log($"[AI-POS] AI {_aiId}: NEW waypoint created - current={currentAngle:F1}°, ideal={IdealShootingAngle:F0}°, step={actualAngleStep:F1}°, wpDist={wpDist:F1}m");
                }
            }
            else
            {
                // ウェイポイントは更新しないが、移動は継続
                // (SetMAnimalBrainTarget()を呼ばないことで、既存の経路を維持)
                if (Time.frameCount % 120 == 0)
                {
                    float distToWaypoint = Vector3.Distance(_mountObject.transform.position, _retreatWaypoint.transform.position);
                    Debug.Log($"[AI-POS] AI {_aiId}: Keeping existing waypoint - dist={distToWaypoint:F1}m, angle={currentAngle:F1}°");
                }
            }
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

        #region Cleanup

        /// <summary>
        /// オブジェクト破棄時のクリーンアップ
        /// </summary>
        private void OnDestroy()
        {
            // 後退用ウェイポイントをクリーンアップ
            if (_retreatWaypoint != null)
            {
                UnityEngine.Object.Destroy(_retreatWaypoint);
                _retreatWaypoint = null;
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

        /// <summary>ターゲット捜索中</summary>
        Search,

        /// <summary>死亡</summary>
        Dead
    }
}
