#nullable enable

using System;
using CavalryFight.Services.Lobby;
using UnityEngine;
using UnityEngine.AI;

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
        private GameObject? _mountObject;
        private AICombatService? _combatService;

        private Animator? _animator;
        private NavMeshAgent? _navAgent;
        private AudioSource? _audioSource;

        // BlazeAI関連（グローバル名前空間、存在しない場合はnull）
        private MonoBehaviour? _blazeAI;

        // 状態
        private AIState _currentState = AIState.Idle;
        private bool _isEnabled;
        private bool _isAlive = true;
        private int _currentHealth;

        // ターゲット
        private GameObject? _currentTarget;
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

        // Animatorパラメータ
        private static readonly int SpeedParam = Animator.StringToHash("Speed");
        private static readonly int ShootParam = Animator.StringToHash("Shoot");
        private static readonly int ChargeParam = Animator.StringToHash("Charge");
        private static readonly int HitParam = Animator.StringToHash("Hit");
        private static readonly int DeathParam = Animator.StringToHash("Death");
        private static readonly int IsMountedParam = Animator.StringToHash("IsMounted");

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

        #endregion

        #region Unity Lifecycle

        private void Awake()
        {
            _animator = GetComponent<Animator>();
            _navAgent = GetComponent<NavMeshAgent>();
            _audioSource = GetComponent<AudioSource>();

            // BlazeAIコンポーネントを取得（任意、リフレクションで型を検索）
            TryGetBlazeAI();

            if (_audioSource == null)
            {
                _audioSource = gameObject.AddComponent<AudioSource>();
            }
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

        private void Update()
        {
            if (!_isEnabled || !_isAlive)
            {
                return;
            }

            UpdateStateMachine();
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

            // NavMeshAgentの設定
            if (_navAgent != null)
            {
                _navAgent.speed = difficultySettings.MoveSpeed;
                _navAgent.angularSpeed = difficultySettings.TurnSpeed * 100f;
            }

            // BlazeAIの設定（存在する場合）
            ConfigureBlazeAI();

            // 騎乗状態に設定
            _animator?.SetBool(IsMountedParam, true);

            Debug.Log($"[AIPlayerController] Initialized. ID: {aiId}, Team: {teamIndex}, Mode: {gameMode}");
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

            if (_navAgent != null)
            {
                _navAgent.enabled = true;
            }

            SetState(AIState.Patrol);
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

                SetState(AIState.Chase);
            }
            else
            {
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

            // 攻撃者をターゲットに設定
            if (attacker != null && _currentTarget == null)
            {
                SetTarget(attacker);
            }

            // 死亡判定
            if (_currentHealth <= 0)
            {
                Die(attacker);
            }

            Debug.Log($"[AIPlayerController] AI {_aiId} took {damage} damage. Health: {_currentHealth}");
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
        /// 最も近い敵を探します
        /// </summary>
        private GameObject? FindNearestEnemy()
        {
            Collider[] colliders = Physics.OverlapSphere(
                transform.position,
                _difficultySettings.VisionRange,
                _enemyLayers
            );

            GameObject? nearest = null;
            float nearestDistance = float.MaxValue;

            foreach (Collider col in colliders)
            {
                // 自分自身は除外
                if (col.gameObject == gameObject)
                {
                    continue;
                }

                // チームメイトは除外
                var otherAI = col.GetComponentInParent<AIPlayerController>();
                if (otherAI != null && otherAI.TeamIndex == _teamIndex)
                {
                    continue;
                }

                float distance = Vector3.Distance(transform.position, col.transform.position);
                if (distance < nearestDistance && CanSeeTarget(col.gameObject))
                {
                    nearestDistance = distance;
                    nearest = col.gameObject;
                }
            }

            return nearest;
        }

        #endregion

        #region State Behaviors

        private void UpdateIdle()
        {
            // 待機中は何もしない
        }

        private void StartPatrol()
        {
            // BlazeAIを使用している場合はBlazeAIに任せる
            if (_blazeAI != null)
            {
                return;
            }

            // 簡易的なランダム巡回
            if (_navAgent != null)
            {
                Vector3 randomPoint = transform.position + UnityEngine.Random.insideUnitSphere * 20f;
                if (NavMesh.SamplePosition(randomPoint, out NavMeshHit hit, 20f, NavMesh.AllAreas))
                {
                    _navAgent.SetDestination(hit.position);
                }
            }
        }

        private void UpdatePatrol()
        {
            // BlazeAIを使用している場合はBlazeAIに任せる
            if (_blazeAI != null && _blazeAI.enabled)
            {
                return;
            }

            // 目的地に到達したら新しい目的地を設定
            if (_navAgent != null && !_navAgent.pathPending && _navAgent.remainingDistance < 1f)
            {
                StartPatrol();
            }
        }

        private void StartChase()
        {
            // BlazeAIを使用している場合はBlazeAIに任せる
            if (_blazeAI != null)
            {
                return;
            }
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

            // ターゲットに向かって移動（BlazeAIがない場合のみ）
            if (_navAgent != null && _blazeAI == null)
            {
                _navAgent.SetDestination(_currentTarget.transform.position);
            }
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

            // ストレイフ判定
            if (!_isCharging && UnityEngine.Random.value < _difficultySettings.StrafeChance * Time.deltaTime)
            {
                SetState(AIState.Strafe);
            }
        }

        private void StartStrafe()
        {
            _strafeDirection = UnityEngine.Random.value > 0.5f ? 1f : -1f;
            _strafeEndTime = Time.time + UnityEngine.Random.Range(1f, 3f);
        }

        private void UpdateStrafe()
        {
            if (_currentTarget == null || Time.time > _strafeEndTime)
            {
                SetState(AIState.Attack);
                return;
            }

            // 横移動
            Vector3 strafeDir = transform.right * _strafeDirection;
            if (_navAgent != null)
            {
                Vector3 targetPos = transform.position + strafeDir * 5f;
                if (NavMesh.SamplePosition(targetPos, out NavMeshHit hit, 5f, NavMesh.AllAreas))
                {
                    _navAgent.SetDestination(hit.position);
                }
            }

            // ターゲットの方を向く
            LookAtTarget();
        }

        private void StartRetreat()
        {
            if (_currentTarget != null && _navAgent != null)
            {
                Vector3 retreatDir = (transform.position - _currentTarget.transform.position).normalized;
                Vector3 retreatPos = transform.position + retreatDir * 10f;

                if (NavMesh.SamplePosition(retreatPos, out NavMeshHit hit, 10f, NavMesh.AllAreas))
                {
                    _navAgent.SetDestination(hit.position);
                }
            }
        }

        private void UpdateRetreat()
        {
            if (_navAgent != null && !_navAgent.pathPending && _navAgent.remainingDistance < 1f)
            {
                SetState(AIState.Attack);
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

            // 矢を生成
            GameObject arrowObj = Instantiate(_arrowPrefab, _bowFirePoint.position, Quaternion.LookRotation(direction));

            // ArrowProjectileコンポーネントを設定（リフレクション使用）
            ConfigureArrowProjectile(arrowObj, direction * arrowSpeed);

            // アニメーション
            _animator?.SetTrigger(ShootParam);

            // 音
            if (_shootSfx != null && _audioSource != null)
            {
                _audioSource.PlayOneShot(_shootSfx);
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
        private void LookAtTarget()
        {
            if (_currentTarget == null)
            {
                return;
            }

            Vector3 direction = _currentTarget.transform.position - transform.position;
            direction.y = 0;

            if (direction.sqrMagnitude > 0.01f)
            {
                Quaternion targetRotation = Quaternion.LookRotation(direction);
                transform.rotation = Quaternion.Slerp(
                    transform.rotation,
                    targetRotation,
                    _difficultySettings.TurnSpeed * Time.deltaTime
                );
            }
        }

        #endregion

        #region Animation

        /// <summary>
        /// Animatorを更新します
        /// </summary>
        private void UpdateAnimator()
        {
            if (_animator == null || _navAgent == null)
            {
                return;
            }

            float speed = _navAgent.velocity.magnitude;
            _animator.SetFloat(SpeedParam, speed);
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
