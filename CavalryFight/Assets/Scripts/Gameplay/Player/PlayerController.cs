#nullable enable

using System.Collections;
using UnityEngine;
using UnityEngine.Animations;
using CavalryFight.Core.Services;
using CavalryFight.Services.Input;
using CavalryFight.Services.Audio;
using CavalryFight.Services.Training;
using CavalryFight.Services.Customization;
using CavalryFight.Gameplay.Projectiles;
using CavalryFight.Gameplay.Match;

namespace CavalryFight.Gameplay.Player
{
    /// <summary>
    /// プレイヤーの移動、射撃、乗馬を管理するコントローラー
    /// </summary>
    /// <remarks>
    /// トレーニングシーンで使用されます。
    /// CharacterControllerを使用して移動し、IInputServiceから入力を取得します。
    /// チャージ攻撃システムを実装しています。
    /// </remarks>
    [RequireComponent(typeof(CharacterController))]
    public class PlayerController : MonoBehaviour
    {
        #region Serialized Fields

        [Header("Movement Settings")]
        [SerializeField] private float _walkSpeed = 5f;
        [SerializeField] private float _sprintSpeed = 8f;
        [SerializeField] private float _rotationSpeed = 10f;
        [SerializeField] private float _gravity = -9.81f;
        [SerializeField] private float _jumpHeight = 1.5f;

        [Header("Bow Settings")]
        [SerializeField] private Transform? _bowFirePoint;
        [Tooltip("デフォルトの矢プレハブ（ArrowType配列が設定されていない場合に使用）")]
        [SerializeField] private GameObject? _arrowPrefab;
        [SerializeField] private float _minArrowSpeed = 15f;
        [SerializeField] private float _maxArrowSpeed = 50f;
        [SerializeField] private float _maxChargeTime = 2f;
        [Tooltip("矢のスケール（デフォルト: 0.1）")]
        [SerializeField] private float _arrowScale = 0.1f;

        [Header("Arrow Types (MasterStylizedProjectiles)")]
        [Tooltip("矢タイプ設定（ScriptableObject）- Assets/Settings/ArrowTypeConfig.asset")]
        [SerializeField] private ArrowTypeConfig? _arrowTypeConfig;

        [Header("Visual Effects (MasterStylizedProjectiles)")]
        [Tooltip("発射時のマズルエフェクト（デフォルト、配列が設定されていない場合に使用）")]
        [SerializeField] private GameObject? _muzzleEffectPrefab;

        [Header("Charging Effect")]
        [Tooltip("チャージ中のエフェクトプレハブ")]
        [SerializeField] private GameObject? _chargingEffectPrefab;

        [Tooltip("チャージエフェクトの最小スケール")]
        [SerializeField] private float _chargingEffectMinScale = 0.1f;

        [Tooltip("チャージエフェクトの最大スケール")]
        [SerializeField] private float _chargingEffectMaxScale = 1.0f;

        [Header("Audio")]
        [SerializeField] private AudioClip? _shootSfx;

        [Header("Mount Settings")]
        [SerializeField] private float _mountDistance = 2f;

        [Header("References")]
        [Tooltip("騎手コントローラー（P09モデルのラッパー）")]
        [SerializeField] private RiderController? _riderController;
        [SerializeField] private Transform? _cameraTransform;

        [Header("Bow")]
        [Tooltip("弓オブジェクト（自動検出）")]
        [SerializeField] private Transform? _bowTransform;

        #endregion

        #region Private Fields

        private CharacterController? _characterController;
        private IInputService? _inputService;
        private IAudioService? _audioService;
        private Vector3 _velocity;
        private bool _isGrounded;
        private bool _isMounted;

        // チャージ攻撃関連
        private bool _isCharging;
        private float _chargeStartTime;
        private float _currentCharge;

        // カスタマイズから適用された現在の矢タイプ
        private ArrowType _currentArrowType = ArrowType.Arrow;
        private GameObject? _currentArrowPrefab;
        private GameObject? _currentMuzzleEffectPrefab;
        private GameObject? _currentHitEffectPrefab;

        // チャージエフェクト
        private GameObject? _chargingEffectInstance;

        // アニメーション制御はRiderControllerに委譲

        #endregion

        #region Properties

        /// <summary>
        /// 現在チャージ中かどうかを取得します
        /// </summary>
        public bool IsCharging => _isCharging;

        /// <summary>
        /// 現在のチャージ量を取得します（0.0～1.0）
        /// </summary>
        public float ChargeAmount => _currentCharge;

        /// <summary>
        /// 弓の発射位置（FirstPersonカメラ用）
        /// </summary>
        public Transform? BowFirePoint => _bowFirePoint;

        #endregion

        #region Unity Lifecycle

        private void Awake()
        {
            _characterController = GetComponent<CharacterController>();
            _inputService = ServiceLocator.Instance.Get<IInputService>();
            _audioService = ServiceLocator.Instance.Get<IAudioService>();

            if (_inputService == null)
            {
                Debug.LogError("[PlayerController] IInputService が取得できませんでした！");
            }

            if (_cameraTransform == null)
            {
                UnityEngine.Camera mainCamera = UnityEngine.Camera.main;
                if (mainCamera != null)
                {
                    _cameraTransform = mainCamera.transform;
                }
            }

            // カスタマイズサービスから矢タイプを適用
            ApplyArrowTypeFromCustomization();

            // 弓の自動検出
            InitializeBowReferences();
        }

        private void Start()
        {
            // RiderControllerが設定されていない場合は自動検出
            if (_riderController == null)
            {
                _riderController = GetComponent<RiderController>();
                if (_riderController == null)
                {
                    _riderController = GetComponentInChildren<RiderController>();
                }
                if (_riderController == null)
                {
                    _riderController = FindFirstObjectByType<RiderController>();
                }
            }

            // 騎乗はPlayerSpawnerが処理します

            // 弓を手に配置（遅延実行でカスタマイズが適用されるのを待つ）
            StartCoroutine(SetupBowToHandDelayed());
        }

        private IEnumerator SetupBowToHandDelayed()
        {
            // カスタマイズが適用されるのを待つ（最大2秒）
            float timeout = 2f;
            float elapsed = 0f;

            while (elapsed < timeout)
            {
                if (_riderController != null)
                {
                    var target = _riderController.GetCustomizationTarget();
                    if (target != null)
                    {
                        SetupBowToHand();
                        yield break;
                    }
                }

                yield return new WaitForSeconds(0.1f);
                elapsed += 0.1f;
            }

            SetupBowToHand();
        }

        /// <summary>
        /// 弓オブジェクトを左手に配置します
        /// </summary>
        /// <remarks>
        /// P09の弓構造:
        /// - 実際の弓メッシュ（例: Bow_1, Bow_2など）はParentConstraintを持ち、2つのTargetを参照
        /// - Weapon_Target_Hand_L: 手の位置ターゲット
        /// - Bow_Target_Back: 背中の位置ターゲット
        /// "Target"を含むオブジェクトは制約ターゲットなのでスキップします。
        /// </remarks>
        private void SetupBowToHand()
        {
            if (_riderController == null)
            {
                return;
            }

            // RiderArcherControllerを取得
            var archerController = _riderController.ArcherController;

            // P09モデルから弓を探す
            GameObject? riderTarget = _riderController.GetCustomizationTarget();
            if (riderTarget == null)
            {
                return;
            }

            // 弓オブジェクトを検索 - ParentConstraintを持つものを優先、"Target"を含むものはスキップ
            GameObject? bowWithConstraint = null;
            GameObject? bowWithoutConstraint = null;
            var allChildren = riderTarget.GetComponentsInChildren<Transform>(true);
            foreach (var child in allChildren)
            {
                // "Target"を含むものは制約ターゲットなのでスキップ
                if (child.name.Contains("Target"))
                {
                    continue;
                }

                if (child.name.Contains("Bow") && !child.name.Contains("Sword") && child.gameObject.activeSelf)
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
            }

            // ParentConstraintを持つ弓を優先
            GameObject? bowObject = bowWithConstraint ?? bowWithoutConstraint;

            if (bowObject == null)
            {
                return;
            }

            // ForceBowToLeftHand用のキャッシュを設定
            _p09BowObject = bowObject;

            // 弓をRiderArcherControllerに設定
            if (archerController != null)
            {
                archerController.SetBowObject(bowObject, immediatelyInHand: true);
            }

            // 弓が左手の下にない場合は移動
            var animator = riderTarget.GetComponentInChildren<Animator>();
            if (animator != null)
            {
                Transform? leftHand = animator.GetBoneTransform(HumanBodyBones.LeftHand);
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
                        var bowPC = bowObject.GetComponent<ParentConstraint>();
                        if (bowPC != null)
                        {
                            bowPC.constraintActive = false;
                        }

                        // 弓を左手の子として配置
                        bowObject.transform.SetParent(leftHand);
                        bowObject.transform.localPosition = Vector3.zero;
                        // 回転はForceBowToLeftHandで毎フレーム(0, 90, -90)に設定
                    }

                    // 弓が直接アタッチされていることをマーク（Sheathe/Unsheatheで位置変更しない）
                    if (archerController != null)
                    {
                        archerController.MarkBowAsDirectlyAttached();
                    }
                }
            }
        }

        /// <summary>
        /// 弓の参照を初期化します
        /// </summary>
        private void InitializeBowReferences()
        {
            if (_bowTransform == null)
            {
                var shootable = GetComponentInChildren<MalbersAnimations.Weapons.MShootable>(true);
                if (shootable != null)
                {
                    _bowTransform = shootable.transform;
                }
            }
        }

        /// <summary>
        /// シーン開始時に自動的に馬に騎乗します
        /// </summary>
        /// <remarks>
        /// トレーニング/マッチ開始時に最も近い馬を見つけて自動騎乗します。
        /// 距離チェックは行わず、必ず騎乗します。
        /// </remarks>
        private void AutoMountAtStart()
        {
            // 最も近い馬を見つける
            GameObject? nearestHorse = FindNearestHorse();

            if (nearestHorse == null)
            {
                Debug.LogWarning("[PlayerController] No horse found for auto-mount at start!");
                return;
            }

            // 騎乗状態に設定（距離チェックなし）
            _isMounted = true;
            _riderController?.SetAnimationState(RiderAnimationState.MountedIdle);

            // プレイヤーを馬のMountPointに移動
            Transform? horseMountPoint = nearestHorse.transform.Find("MountPoint");
            if (horseMountPoint != null)
            {
                transform.position = horseMountPoint.position;
                transform.rotation = horseMountPoint.rotation;
                transform.SetParent(nearestHorse.transform);
            }
            else
            {
                // MountPointがない場合は馬の位置に配置
                transform.position = nearestHorse.transform.position + Vector3.up * 1.5f;
                transform.rotation = nearestHorse.transform.rotation;
                transform.SetParent(nearestHorse.transform);
            }

        }

        private void Update()
        {
            if (_inputService == null || !_inputService.InputEnabled)
            {
                return;
            }

            // RiderControllerから騎乗状態を同期
            if (_riderController != null)
            {
                _isMounted = _riderController.IsMounted;
            }

            // 騎乗中は移動をMountControllerに任せ、戦闘のみ処理
            if (_isMounted)
            {
                HandleMountedState();
            }
            else
            {
                HandleGroundedState();
            }

            HandleChargeAttack();

            // 弓は手のボーンの子になっているので、手の動きに自動的に追従する
            // UpdateBowAimingPosition()は不要（手動で位置を上書きすると親子関係が無効になる）
        }

        /// <summary>
        /// LateUpdateで弓の位置を強制的に維持（他のスクリプトが上書きするのを防ぐ）
        /// </summary>
        private void LateUpdate()
        {
            // P09の弓を常に左手に固定し、回転を(0, 90, -90)に維持
            ForceBowToLeftHand(_isCharging);

            // エイム中は上半身をカメラの向きに回転（アニメーション後に適用）
            if (_isCharging)
            {
                _isResettingSpineRotation = false;
                RotateRiderTowardCamera();
            }
            // リセット中は徐々に前方を向く
            else if (_isResettingSpineRotation)
            {
                SmoothResetSpineRotation();
            }
            // 通常時（騎乗中、非エイム）はアニメーションに任せる（補正なし）
            // アニメーションのMountedIdleが正しく前方を向いていることを前提とする
        }

        // P09弓オブジェクトのキャッシュ
        private GameObject? _p09BowObject;              // Bowオブジェクト（ParentConstraintを持つ）
        private ParentConstraint? _bowParentConstraint; // BowのParentConstraint

        /// <summary>
        /// P09の弓を手に配置し、アニメーションによる回転変化を補正します
        /// </summary>
        /// <param name="isAiming">エイム中かどうか</param>
        /// <remarks>
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
                if (_riderController == null)
                {
                    _riderController = GetComponentInChildren<RiderController>();
                    if (_riderController == null)
                    {
                        _riderController = FindFirstObjectByType<RiderController>();
                    }
                    if (_riderController == null)
                    {
                        return;
                    }
                }

                var riderTarget = _riderController.GetCustomizationTarget();
                if (riderTarget == null)
                {
                    return;
                }

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

        #endregion

        #region Movement

        /// <summary>
        /// 地上での移動処理
        /// </summary>
        private void HandleGroundedState()
        {
            if (_characterController == null || _cameraTransform == null)
            {
                return;
            }

            // 接地判定
            _isGrounded = _characterController.isGrounded;

            if (_isGrounded && _velocity.y < 0)
            {
                _velocity.y = -2f;
            }

            // 入力取得
            Vector2 moveInput = _inputService!.GetMovementInput();

            // カメラ基準の移動方向を計算
            Vector3 forward = _cameraTransform.forward;
            Vector3 right = _cameraTransform.right;
            forward.y = 0f;
            right.y = 0f;
            forward.Normalize();
            right.Normalize();

            Vector3 moveDirection = (forward * moveInput.y + right * moveInput.x).normalized;

            // 移動速度（IInputServiceからスプリント状態を取得）
            float speed = _inputService!.GetSprintButton() ? _sprintSpeed : _walkSpeed;

            // 移動適用
            _characterController.Move(moveDirection * speed * Time.deltaTime);

            // キャラクターを移動方向に回転（チャージ中は回転しない）
            if (moveDirection.magnitude > 0.1f && !_isCharging)
            {
                Quaternion targetRotation = Quaternion.LookRotation(moveDirection);
                transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, _rotationSpeed * Time.deltaTime);
            }

            // ジャンプ
            if (_inputService.GetJumpButtonDown() && _isGrounded)
            {
                _velocity.y = Mathf.Sqrt(_jumpHeight * -2f * _gravity);
                // ジャンプアニメーションはRiderControllerに追加が必要な場合に実装
            }

            // 重力適用
            _velocity.y += _gravity * Time.deltaTime;
            _characterController.Move(_velocity * Time.deltaTime);

            // 地上移動時のアニメーション更新（将来的にRiderControllerに移行）
        }

        /// <summary>
        /// 騎乗中の状態処理
        /// </summary>
        private void HandleMountedState()
        {
            // 騎乗中はプレイヤーの移動は無効
            // Malbers Horse Controllerが馬の移動を制御します
            // ここでは降馬の処理のみ行います
        }

        #endregion

        #region Combat - Charge Attack System

        /// <summary>
        /// チャージ攻撃処理
        /// </summary>
        /// <remarks>
        /// - 左クリック押下: チャージ開始
        /// - 左クリック押し続け: チャージ蓄積
        /// - 左クリック離す: チャージ量に応じて矢を発射
        /// - 右クリック（チャージ中）: チャージキャンセル
        /// </remarks>
        private void HandleChargeAttack()
        {
            if (_inputService == null)
            {
                return;
            }

            // チャージ開始
            if (_inputService.GetAttackButtonDown())
            {
                StartCharge();
            }

            // チャージ中
            if (_isCharging)
            {
                // チャージキャンセル（右クリック）
                if (_inputService.GetCancelAttackButtonDown())
                {
                    CancelCharge();
                    return;
                }

                // チャージ蓄積
                float chargeTime = Time.time - _chargeStartTime;
                _currentCharge = Mathf.Clamp01(chargeTime / _maxChargeTime);

                // RiderControllerでチャージ量を更新
                _riderController?.SetChargeAmount(_currentCharge);

                // チャージエフェクトのスケールを更新
                UpdateChargingEffectScale(_currentCharge);

                // 矢を発射（ボタン離した時）
                if (_inputService.GetAttackButtonUp())
                {
                    // チャージエフェクトを破棄
                    DestroyChargingEffect();

                    FireArrow(_currentCharge);
                    _isCharging = false;
                    _currentCharge = 0f;
                    _riderController?.SetChargeAmount(0f);

                    // 射撃アニメーションを再生
                    _riderController?.SetAnimationState(RiderAnimationState.Shooting);

                    // 上半身の回転をリセット
                    ResetSpineRotation();

                    // TrainingManagerにチャージ終了を通知
                    TrainingManager.Instance?.NotifyChargingEnded();
                }
            }
        }

        /// <summary>
        /// チャージを開始します
        /// </summary>
        private void StartCharge()
        {
            _isCharging = true;
            _chargeStartTime = Time.time;
            _currentCharge = 0f;

            // エイムアニメーション開始（StateMachineBehavioursが弓の処理を担当）
            _riderController?.SetAnimationState(RiderAnimationState.Aiming);

            // チャージエフェクトを生成
            SpawnChargingEffect();

            // TrainingManagerにチャージ開始を通知
            TrainingManager.Instance?.NotifyChargingStarted();
        }

        /// <summary>
        /// チャージをキャンセルします
        /// </summary>
        private void CancelCharge()
        {
            _isCharging = false;
            _currentCharge = 0f;
            _riderController?.SetChargeAmount(0f);
            _riderController?.SetAnimationState(RiderAnimationState.MountedIdle);

            // RiderArcherControllerが弓の状態をリセット
            _riderController?.ArcherController?.ResetBowState();

            // チャージエフェクトを破棄
            DestroyChargingEffect();

            // 上半身の回転をリセット
            ResetSpineRotation();

            // TrainingManagerにチャージ終了を通知
            TrainingManager.Instance?.NotifyChargingEnded();
        }

        /// <summary>
        /// 矢を発射します
        /// </summary>
        /// <param name="chargeAmount">チャージ量（0.0～1.0）</param>
        private void FireArrow(float chargeAmount)
        {
            // マッチモードで矢が残っているかチェック
            if (!CanFireArrow())
            {
                Debug.Log("[PlayerController] Cannot fire - no arrows remaining!");
                return;
            }

            // カスタマイズで設定された矢プレハブを優先、なければデフォルトを使用
            GameObject? arrowPrefabToUse = _currentArrowPrefab ?? _arrowPrefab;

            if (arrowPrefabToUse == null || _bowFirePoint == null)
            {
                Debug.LogWarning("[PlayerController] Arrow prefab or bow fire point not assigned!");
                return;
            }

            // マズルエフェクトを生成（MasterStylizedProjectiles）
            SpawnMuzzleEffect();

            // カメラの向きを取得（矢の発射方向）
            Vector3 shootDirection = _cameraTransform != null ? _cameraTransform.forward : _bowFirePoint.forward;

            // チャージ量に応じた矢の速度を計算
            float arrowSpeed = Mathf.Lerp(_minArrowSpeed, _maxArrowSpeed, chargeAmount);
            Vector3 velocity = shootDirection * arrowSpeed;

            // 矢のスポーン位置を前方にオフセット（馬との衝突を防ぐ）
            Vector3 spawnPosition = _bowFirePoint.position + shootDirection * 0.5f;

            // 矢の親オブジェクトを作成（スケール1,1,1を維持、物理演算用）
            GameObject arrowParent = new GameObject("Arrow");
            arrowParent.transform.position = spawnPosition;
            arrowParent.transform.rotation = Quaternion.LookRotation(shootDirection);


            // VFX矢プレハブを親の子としてインスタンス化
            GameObject arrowVisual = Instantiate(arrowPrefabToUse, arrowParent.transform);
            arrowVisual.transform.localPosition = Vector3.zero;
            arrowVisual.transform.localRotation = Quaternion.identity;

            // ビジュアルにスケールを適用
            arrowVisual.transform.localScale = Vector3.one * _arrowScale;

            // VFXプレハブにRigidbodyがある場合は無効化（親で物理制御するため）
            Rigidbody? visualRb = arrowVisual.GetComponent<Rigidbody>();
            if (visualRb != null)
            {
                visualRb.isKinematic = true;
            }

            // ArrowProjectileコンポーネントを親に追加（RequireComponentでRigidbodyも自動追加される）
            var arrowProjectile = arrowParent.AddComponent<ArrowProjectile>();

            // 発射者と馬を無視対象に設定（衝突判定の前に設定する）
            arrowProjectile.AddIgnoredObject(gameObject);
            // 親（馬）も無視対象に追加
            if (transform.parent != null)
            {
                arrowProjectile.AddIgnoredObject(transform.parent.gameObject);
            }
            // ルートオブジェクトも追加（階層が深い場合）
            arrowProjectile.AddIgnoredObject(transform.root.gameObject);

            // 速度とチャージ量を設定
            arrowProjectile.SetVelocity(velocity);
            arrowProjectile.SetChargeAmount(chargeAmount);

            // ヒットエフェクトを設定（ArrowTypeConfigから）
            arrowProjectile.SetHitEffectPrefab(_currentHitEffectPrefab);

            // Rigidbodyを取得して設定（ArrowProjectileのRequireComponentで自動追加済み）
            Rigidbody arrowRb = arrowParent.GetComponent<Rigidbody>();
            arrowRb.useGravity = true;
            arrowRb.linearVelocity = velocity;

            // コライダーを親に追加（矢の当たり判定）
            // 注: isTrigger = false にすることで、相手がTriggerでも衝突を検出できる
            var collider = arrowParent.AddComponent<SphereCollider>();
            collider.radius = 0.15f;  // 少し大きくして当たりやすく
            collider.isTrigger = false;  // 非Triggerで物理衝突を検出

            // 高速移動でも衝突を検出できるようにContinuous Dynamicに設定
            arrowRb.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;

            // 発射者と馬のコライダーとの衝突を物理的に無視（Physics.IgnoreCollision）
            IgnoreCollisionWithOwner(collider);

            // 射撃アニメーション再生
            _riderController?.SetAnimationState(RiderAnimationState.Shooting);

            // 射撃音を再生
            PlayShootSound();

            // TrainingManagerに通知
            TrainingManager.Instance?.RecordArrowFired();

            // MatchManagerに通知（マッチモード用）
            if (MatchManager.Instance != null)
            {
                // ローカルプレイヤーID（NetworkManager未使用の場合は0）
                ulong localPlayerId = Unity.Netcode.NetworkManager.Singleton?.LocalClientId ?? 0;
                MatchManager.Instance.RecordArrowFiredLocal(localPlayerId);
            }
        }

        /// <summary>
        /// 矢を発射できるかどうかをチェックします
        /// </summary>
        /// <returns>発射可能な場合はtrue</returns>
        private bool CanFireArrow()
        {
            // トレーニングモードでは常に発射可能
            if (TrainingManager.Instance != null)
            {
                return true;
            }

            // マッチモードでない場合は発射可能
            if (MatchManager.Instance == null)
            {
                return true;
            }

            // ルーム設定を取得
            var roomSettings = MatchManager.Instance.RoomSettings;

            // 矢が無制限の場合（ArrowLimit == 0）は発射可能
            if (roomSettings.ArrowLimit == 0)
            {
                return true;
            }

            // プレイヤーの残り矢数を取得
            ulong localPlayerId = Unity.Netcode.NetworkManager.Singleton?.LocalClientId ?? 0;
            var playerScore = MatchManager.Instance.GetPlayerScore(localPlayerId);

            if (playerScore == null)
            {
                // プレイヤースコアが見つからない場合は発射可能（初期化前など）
                return true;
            }

            // 残り矢数が0以下の場合は発射不可
            return playerScore.Value.RemainingArrows > 0;
        }

        #endregion

        #region Audio

        /// <summary>
        /// 射撃音を再生します
        /// </summary>
        private void PlayShootSound()
        {
            if (_shootSfx != null && _audioService != null)
            {
                _audioService.PlaySfx(_shootSfx);
            }
        }

        #endregion

        #region Visual Effects

        /// <summary>
        /// マズルエフェクトを生成します（MasterStylizedProjectiles）
        /// </summary>
        private void SpawnMuzzleEffect()
        {
            // カスタマイズで設定されたマズルエフェクトを優先、なければデフォルトを使用
            GameObject? muzzlePrefab = _currentMuzzleEffectPrefab ?? _muzzleEffectPrefab;

            if (muzzlePrefab == null || _bowFirePoint == null)
            {
                return;
            }

            // マズルエフェクトを発射位置に生成
            GameObject muzzle = Instantiate(muzzlePrefab, _bowFirePoint.position, _bowFirePoint.rotation);

            // 自動削除（パーティクルシステムの場合は自動で消えるが念のため）
            Destroy(muzzle, 3f);
        }

        /// <summary>
        /// チャージエフェクトを生成します
        /// </summary>
        private void SpawnChargingEffect()
        {
            if (_chargingEffectPrefab == null || _bowFirePoint == null)
            {
                return;
            }

            // 既存のエフェクトがあれば破棄
            DestroyChargingEffect();

            // チャージエフェクトを弓の発射位置に生成し、子として設定
            _chargingEffectInstance = Instantiate(_chargingEffectPrefab, _bowFirePoint.position, _bowFirePoint.rotation, _bowFirePoint);
            _chargingEffectInstance.transform.localPosition = Vector3.zero;

            // 初期スケールを最小に設定
            float initialScale = _chargingEffectMinScale;
            _chargingEffectInstance.transform.localScale = Vector3.one * initialScale;
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

        // 上半身（Spine）とHead のキャッシュ
        private Transform? _spineTransform;
        private Transform? _headTransform;
        private float _currentSpineYRotation = 0f;
        private float _currentSpineXRotation = 0f;
        private bool _hairReparented = false;
        private bool _isResettingSpineRotation = false;
        private Quaternion _lastSpineWorldRotation = Quaternion.identity;
        private bool _hasLastSpineRotation = false;

        /// <summary>
        /// 騎手の上半身をカメラの向きに回転させます（エイム中）
        /// </summary>
        /// <remarks>
        /// 騎乗中にエイムしている場合、騎手の上半身（Spine）がカメラの水平方向を向くように回転します。
        /// アニメーションの回転に追加の回転を乗せる形で適用します。
        /// </remarks>
        private void RotateRiderTowardCamera()
        {
            // カメラ参照がない場合は取得を試みる
            if (_cameraTransform == null)
            {
                UnityEngine.Camera mainCamera = UnityEngine.Camera.main;
                if (mainCamera != null)
                {
                    _cameraTransform = mainCamera.transform;
                }
                else
                {
                    return;
                }
            }

            if (_riderController == null)
            {
                return;
            }

            // SpineボーンとHeadボーンを取得（初回のみ）
            if (_spineTransform == null || _headTransform == null)
            {
                Animator? animator = _riderController.Animator;
                if (animator != null)
                {
                    _spineTransform = animator.GetBoneTransform(HumanBodyBones.Spine);
                    _headTransform = animator.GetBoneTransform(HumanBodyBones.Head);
                    if (_spineTransform == null)
                    {
                        return;
                    }
                    if (_headTransform != null)
                    {
                        // 髪をHeadボーンの子に移動（初回のみ）
                        if (!_hairReparented)
                        {
                            ReparentHairToHead(animator);
                            _hairReparented = true;
                        }
                    }
                }
                else
                {
                    return;
                }
            }

            // カメラの水平方向（Y軸回転のみ）を取得
            Vector3 cameraForward = _cameraTransform.forward;
            cameraForward.y = 0f;

            if (cameraForward.sqrMagnitude < 0.001f)
            {
                return;
            }

            cameraForward.Normalize();

            // 馬（マウント）の向きを取得
            Transform mountTransform = _riderController.transform.parent;
            if (mountTransform == null)
            {
                return;
            }

            // 馬の向きとカメラの向きの角度差を計算
            Vector3 mountForward = mountTransform.forward;
            mountForward.y = 0f;
            mountForward.Normalize();

            float targetAngleY = Vector3.SignedAngle(mountForward, cameraForward, Vector3.up);

            // 90度オフセットを追加（角度0で左を向いているため、+90で前を向く）
            targetAngleY += 90f;

            // 垂直角度を取得（カメラの上下向き）
            float targetAngleX = -_cameraTransform.eulerAngles.x;
            // 角度を-180～180の範囲に正規化
            if (targetAngleX < -180f) targetAngleX += 360f;
            if (targetAngleX > 180f) targetAngleX -= 360f;

            // スムーズに目標角度に近づける
            _currentSpineYRotation = Mathf.LerpAngle(_currentSpineYRotation, targetAngleY, _rotationSpeed * Time.deltaTime);
            _currentSpineXRotation = Mathf.LerpAngle(_currentSpineXRotation, targetAngleX, _rotationSpeed * Time.deltaTime);

            // アニメーションの回転に追加の回転を乗せる（Y軸とX軸）
            Quaternion additionalRotation = Quaternion.Euler(_currentSpineXRotation, _currentSpineYRotation, 0f);
            _spineTransform.rotation = _spineTransform.rotation * additionalRotation;

            // 現在のワールド回転を保存（リセット時に使用）
            _lastSpineWorldRotation = _spineTransform.rotation;
            _hasLastSpineRotation = true;
        }

        /// <summary>
        /// 髪オブジェクトをHeadボーンの子に移動します
        /// </summary>
        /// <param name="animator">キャラクターのAnimator</param>
        private void ReparentHairToHead(Animator animator)
        {
            if (_headTransform == null)
            {
                return;
            }

            // P09モデルのルートを取得
            Transform root = animator.transform;

            // Hair を含む名前のオブジェクトを検索
            var allTransforms = root.GetComponentsInChildren<Transform>(true);
            foreach (var t in allTransforms)
            {
                // Hair_ で始まるオブジェクト（例: Hair_01, Hair_02）を検索
                if (t.name.StartsWith("Hair_") && t.parent != _headTransform)
                {
                    // 現在のワールド位置と回転を保存
                    Vector3 worldPos = t.position;
                    Quaternion worldRot = t.rotation;

                    // Headの子に移動
                    t.SetParent(_headTransform);

                    // ワールド位置と回転を維持
                    t.position = worldPos;
                    t.rotation = worldRot;
                }
            }
        }

        /// <summary>
        /// 上半身の回転をリセットします（徐々に戻す）
        /// </summary>
        private void ResetSpineRotation()
        {
            // フラグを立てて、LateUpdateで徐々にリセット
            _isResettingSpineRotation = true;

            // 現在のワールド回転を保存（まだ保存されていない場合）
            if (!_hasLastSpineRotation && _spineTransform != null)
            {
                _lastSpineWorldRotation = _spineTransform.rotation;
                _hasLastSpineRotation = true;
            }
        }

        /// <summary>
        /// 上半身の回転オーバーレイを徐々に0に戻します（アニメーションに任せる）
        /// </summary>
        /// <remarks>
        /// エイム中は騎手がカメラ方向を向くように回転オーバーレイを追加しています。
        /// 射撃/キャンセル後は、オーバーレイを0に戻してアニメーションの回転に任せます。
        /// </remarks>
        private void SmoothResetSpineRotation()
        {
            if (_spineTransform == null || _riderController == null)
            {
                _isResettingSpineRotation = false;
                _hasLastSpineRotation = false;
                _currentSpineYRotation = 0f;
                _currentSpineXRotation = 0f;
                return;
            }

            // より速いリセット速度を使用
            float resetSpeed = _rotationSpeed * 2f;

            // 目標：オーバーレイを0に戻す（アニメーションの回転のみにする）
            _currentSpineYRotation = Mathf.LerpAngle(_currentSpineYRotation, 0f, resetSpeed * Time.deltaTime);
            _currentSpineXRotation = Mathf.LerpAngle(_currentSpineXRotation, 0f, resetSpeed * Time.deltaTime);

            // オーバーレイ回転を適用（0に近づいていく）
            if (Mathf.Abs(_currentSpineYRotation) > 0.5f || Mathf.Abs(_currentSpineXRotation) > 0.5f)
            {
                Quaternion additionalRotation = Quaternion.Euler(_currentSpineXRotation, _currentSpineYRotation, 0f);
                _spineTransform.rotation = _spineTransform.rotation * additionalRotation;
            }

            // ほぼ0になったらリセット完了
            if (Mathf.Abs(_currentSpineYRotation) < 1f && Mathf.Abs(_currentSpineXRotation) < 1f)
            {
                _isResettingSpineRotation = false;
                _hasLastSpineRotation = false;
                _currentSpineYRotation = 0f;
                _currentSpineXRotation = 0f;
            }
        }

        #endregion

        #region Mount System

        /// <summary>
        /// 乗馬/降馬の切り替え処理
        /// </summary>
        private void HandleMountToggle()
        {
            if (_inputService == null)
            {
                return;
            }

            if (_inputService.GetMountButtonDown())
            {
                if (_isMounted)
                {
                    Dismount();
                }
                else
                {
                    TryMount();
                }
            }
        }

        /// <summary>
        /// 馬に乗ろうと試みます
        /// </summary>
        private void TryMount()
        {
            // 最も近い馬を見つける
            GameObject? nearestHorse = FindNearestHorse();

            if (nearestHorse == null)
            {
                return;
            }

            // 距離チェック
            float distance = Vector3.Distance(transform.position, nearestHorse.transform.position);
            if (distance > _mountDistance)
            {
                return;
            }

            // 騎乗成功
            _isMounted = true;
            _riderController?.SetAnimationState(RiderAnimationState.MountedIdle);

            // プレイヤーを馬のMountPointに移動
            Transform? horseMountPoint = nearestHorse.transform.Find("MountPoint");
            if (horseMountPoint != null)
            {
                transform.position = horseMountPoint.position;
                transform.rotation = horseMountPoint.rotation;
                transform.SetParent(nearestHorse.transform);
            }
            else
            {
                // MountPointがない場合は馬の位置に配置
                transform.position = nearestHorse.transform.position + Vector3.up * 1.5f;
                transform.rotation = nearestHorse.transform.rotation;
                transform.SetParent(nearestHorse.transform);
            }
        }

        /// <summary>
        /// 最も近い馬を見つけます
        /// </summary>
        /// <returns>最も近い馬のGameObject（見つからない場合null）</returns>
        private GameObject? FindNearestHorse()
        {
            // "Horse"タグまたはレイヤーで馬を検索
            GameObject[] horses = GameObject.FindGameObjectsWithTag("Horse");

            // タグで見つからない場合は、名前に"Horse"を含むオブジェクトを検索
            if (horses.Length == 0)
            {
                GameObject[] allObjects = FindObjectsByType<GameObject>(FindObjectsSortMode.None);
                horses = System.Array.FindAll(allObjects, obj => obj.name.Contains("Horse"));
            }

            if (horses.Length == 0)
            {
                return null;
            }

            // 最も近い馬を見つける
            GameObject? nearestHorse = null;
            float nearestDistance = float.MaxValue;

            foreach (GameObject horse in horses)
            {
                float distance = Vector3.Distance(transform.position, horse.transform.position);
                if (distance < nearestDistance)
                {
                    nearestDistance = distance;
                    nearestHorse = horse;
                }
            }

            return nearestHorse;
        }

        /// <summary>
        /// 馬から降ります
        /// </summary>
        private void Dismount()
        {
            _isMounted = false;
            _riderController?.SetAnimationState(RiderAnimationState.Idle);

            // 馬から降りる（親子関係を解除）
            if (transform.parent != null)
            {
                // 地面に降りる位置を計算（馬の横）
                Vector3 dismountPosition = transform.position + transform.right * 1.5f;
                transform.SetParent(null);
                transform.position = dismountPosition;
            }
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// カメラTransformを設定します
        /// </summary>
        /// <param name="cameraTransform">カメラのTransform</param>
        public void SetCameraTransform(Transform cameraTransform)
        {
            _cameraTransform = cameraTransform;
        }

        /// <summary>
        /// 騎乗状態を取得します
        /// </summary>
        public bool IsMounted => _isMounted;

        #endregion

        #region Arrow Customization

        /// <summary>
        /// カスタマイズサービスから矢タイプを適用します
        /// </summary>
        private void ApplyArrowTypeFromCustomization()
        {
            var customizationService = ServiceLocator.Instance.Get<ICustomizationService>();
            if (customizationService == null)
            {
                return;
            }

            ArrowType arrowType = customizationService.CurrentCharacter.ArrowType;
            SetArrowType(arrowType);
        }

        /// <summary>
        /// 矢タイプを設定します
        /// </summary>
        /// <param name="arrowType">設定する矢タイプ</param>
        public void SetArrowType(ArrowType arrowType)
        {
            _currentArrowType = arrowType;

            if (_arrowTypeConfig == null)
            {
                _currentArrowPrefab = null;
                _currentMuzzleEffectPrefab = null;
                _currentHitEffectPrefab = null;
                return;
            }

            // ScriptableObjectからプレハブを取得
            var prefabs = _arrowTypeConfig.GetAllPrefabs(arrowType);
            _currentArrowPrefab = prefabs.arrow;
            _currentMuzzleEffectPrefab = prefabs.muzzle;
            _currentHitEffectPrefab = prefabs.hit;
        }

        /// <summary>
        /// 現在の矢タイプを取得します
        /// </summary>
        public ArrowType CurrentArrowType => _currentArrowType;

        #endregion

        #region Collision Helpers

        /// <summary>
        /// 発射者と馬のコライダーとの衝突を無視します
        /// </summary>
        /// <param name="arrowCollider">矢のコライダー</param>
        private void IgnoreCollisionWithOwner(Collider arrowCollider)
        {
            // 自身のすべてのコライダーを取得して無視
            Collider[] myColliders = GetComponentsInChildren<Collider>();
            foreach (var col in myColliders)
            {
                Physics.IgnoreCollision(arrowCollider, col, true);
            }

            // 親（馬）のすべてのコライダーを取得して無視
            if (transform.parent != null)
            {
                Collider[] parentColliders = transform.parent.GetComponentsInChildren<Collider>();
                foreach (var col in parentColliders)
                {
                    Physics.IgnoreCollision(arrowCollider, col, true);
                }
            }

            // ルートオブジェクトのすべてのコライダーも無視（階層が深い場合）
            if (transform.root != transform && transform.root != transform.parent)
            {
                Collider[] rootColliders = transform.root.GetComponentsInChildren<Collider>();
                foreach (var col in rootColliders)
                {
                    Physics.IgnoreCollision(arrowCollider, col, true);
                }
            }
        }

        #endregion
    }
}
