#nullable enable

using UnityEngine;
using CavalryFight.Core.Services;
using CavalryFight.Services.Input;
using CavalryFight.Services.Audio;
using CavalryFight.Gameplay.Projectiles;
using CavalryFight.Gameplay.Training;

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
        [SerializeField] private GameObject? _arrowPrefab;
        [SerializeField] private float _minArrowSpeed = 15f;
        [SerializeField] private float _maxArrowSpeed = 50f;
        [SerializeField] private float _maxChargeTime = 2f;

        [Header("Audio")]
        [SerializeField] private AudioClip? _shootSfx;

        [Header("Mount Settings")]
        [SerializeField] private Transform? _mountPoint;
        [SerializeField] private float _mountDistance = 2f;

        [Header("References")]
        [SerializeField] private Animator? _animator;
        [SerializeField] private Transform? _cameraTransform;

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

        /// <summary>Animatorパラメータ: Speed</summary>
        private static readonly int SpeedParam = Animator.StringToHash("Speed");
        /// <summary>Animatorパラメータ: IsGrounded</summary>
        private static readonly int IsGroundedParam = Animator.StringToHash("IsGrounded");
        /// <summary>Animatorパラメータ: Jump</summary>
        private static readonly int JumpParam = Animator.StringToHash("Jump");
        /// <summary>Animatorパラメータ: IsMounted</summary>
        private static readonly int IsMountedParam = Animator.StringToHash("IsMounted");
        /// <summary>Animatorパラメータ: Shoot</summary>
        private static readonly int ShootParam = Animator.StringToHash("Shoot");
        /// <summary>Animatorパラメータ: Charge</summary>
        private static readonly int ChargeParam = Animator.StringToHash("Charge");

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
                // カメラが設定されていない場合は、メインカメラを使用
                Camera mainCamera = Camera.main;
                if (mainCamera != null)
                {
                    _cameraTransform = mainCamera.transform;
                }
            }
        }

        private void Start()
        {
            // トレーニング/マッチ開始時に自動的に馬に騎乗
            AutoMountAtStart();
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
            _animator?.SetBool(IsMountedParam, true);

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

            Debug.Log($"[PlayerController] Auto-mounted on: {nearestHorse.name} at scene start");
        }

        private void Update()
        {
            if (_inputService == null || !_inputService.InputEnabled)
            {
                return;
            }

            // トレーニングモードでは常に馬に乗っている状態
            // マッチ開始時に自動騎乗し、終了まで降りない
            if (_isMounted)
            {
                HandleMountedState();
            }
            else
            {
                HandleGroundedState();
            }

            HandleChargeAttack();
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

            // 移動速度（シフトキーでスプリント）
            float speed = Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift)
                ? _sprintSpeed
                : _walkSpeed;

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
                _animator?.SetTrigger(JumpParam);
            }

            // 重力適用
            _velocity.y += _gravity * Time.deltaTime;
            _characterController.Move(_velocity * Time.deltaTime);

            // Animator更新
            if (_animator != null)
            {
                _animator.SetFloat(SpeedParam, moveDirection.magnitude * speed);
                _animator.SetBool(IsGroundedParam, _isGrounded);
            }
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
                // チャージキャンセル
                if (_inputService.GetCancelAttackButtonDown())
                {
                    CancelCharge();
                    return;
                }

                // チャージ蓄積
                float chargeTime = Time.time - _chargeStartTime;
                _currentCharge = Mathf.Clamp01(chargeTime / _maxChargeTime);

                // Animator更新
                _animator?.SetFloat(ChargeParam, _currentCharge);

                // 矢を発射（ボタン離した時）
                if (_inputService.GetAttackButtonUp())
                {
                    FireArrow(_currentCharge);
                    _isCharging = false;
                    _currentCharge = 0f;
                    _animator?.SetFloat(ChargeParam, 0f);
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

            Debug.Log("[PlayerController] Charge started");
        }

        /// <summary>
        /// チャージをキャンセルします
        /// </summary>
        private void CancelCharge()
        {
            _isCharging = false;
            _currentCharge = 0f;
            _animator?.SetFloat(ChargeParam, 0f);

            Debug.Log("[PlayerController] Charge canceled");
        }

        /// <summary>
        /// 矢を発射します
        /// </summary>
        /// <param name="chargeAmount">チャージ量（0.0～1.0）</param>
        private void FireArrow(float chargeAmount)
        {
            if (_arrowPrefab == null || _bowFirePoint == null)
            {
                Debug.LogWarning("[PlayerController] Arrow prefab or bow fire point not assigned!");
                return;
            }

            // チャージ量に応じた矢の速度を計算
            float arrowSpeed = Mathf.Lerp(_minArrowSpeed, _maxArrowSpeed, chargeAmount);

            // 矢をインスタンス化
            GameObject arrowObj = Instantiate(_arrowPrefab, _bowFirePoint.position, _bowFirePoint.rotation);

            // ArrowProjectileコンポーネントを取得して速度を設定
            var arrowProjectile = arrowObj.GetComponent<ArrowProjectile>();
            if (arrowProjectile != null)
            {
                Vector3 velocity = _bowFirePoint.forward * arrowSpeed;
                arrowProjectile.SetVelocity(velocity);

                // チャージ量も設定（スコア計算に使用）
                arrowProjectile.SetChargeAmount(chargeAmount);
            }
            else
            {
                // ArrowProjectileがない場合はRigidbodyに直接設定
                Rigidbody? arrowRb = arrowObj.GetComponent<Rigidbody>();
                if (arrowRb != null)
                {
                    arrowRb.linearVelocity = _bowFirePoint.forward * arrowSpeed;
                }
            }

            // 射撃アニメーション再生
            _animator?.SetTrigger(ShootParam);

            // 射撃音を再生
            PlayShootSound();

            // TrainingManagerに通知
            TrainingManager.Instance?.NotifyArrowFired();

            Debug.Log($"[PlayerController] Arrow fired! Charge: {chargeAmount:F2}, Speed: {arrowSpeed:F1}");
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
                Debug.Log("[PlayerController] No horse nearby to mount.");
                return;
            }

            // 距離チェック
            float distance = Vector3.Distance(transform.position, nearestHorse.transform.position);
            if (distance > _mountDistance)
            {
                Debug.Log($"[PlayerController] Horse too far away: {distance:F1}m (max: {_mountDistance}m)");
                return;
            }

            // 騎乗成功
            _isMounted = true;
            _animator?.SetBool(IsMountedParam, true);

            // プレイヤーを馬の位置に移動（Malbers Mountポイントを使用する場合は後で調整）
            if (_mountPoint != null && nearestHorse.transform.Find("MountPoint") != null)
            {
                Transform horseMountPoint = nearestHorse.transform.Find("MountPoint");
                transform.position = horseMountPoint.position;
                transform.SetParent(nearestHorse.transform);
            }

            Debug.Log($"[PlayerController] Mounted on: {nearestHorse.name}");
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
                GameObject[] allObjects = FindObjectsOfType<GameObject>();
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
            _animator?.SetBool(IsMountedParam, false);

            // 馬から降りる（親子関係を解除）
            if (transform.parent != null)
            {
                // 地面に降りる位置を計算（馬の横）
                Vector3 dismountPosition = transform.position + transform.right * 1.5f;
                transform.SetParent(null);
                transform.position = dismountPosition;
            }

            Debug.Log("[PlayerController] Dismounted!");
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
    }
}
