#nullable enable

using UnityEngine;
using CavalryFight.Core.Services;
using CavalryFight.Services.Input;
using CavalryFight.Services.Audio;
using CavalryFight.Services.Training;
using CavalryFight.Services.Customization;
using CavalryFight.Gameplay.Projectiles;

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

        [Header("Audio")]
        [SerializeField] private AudioClip? _shootSfx;

        [Header("Mount Settings")]
        [SerializeField] private float _mountDistance = 2f;

        [Header("References")]
        [Tooltip("騎手コントローラー（P09モデルのラッパー）")]
        [SerializeField] private RiderController? _riderController;
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

        // カスタマイズから適用された現在の矢タイプ
        private ArrowType _currentArrowType = ArrowType.Arrow;
        private GameObject? _currentArrowPrefab;
        private GameObject? _currentMuzzleEffectPrefab;
        private GameObject? _currentHitEffectPrefab;

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
                // カメラが設定されていない場合は、メインカメラを使用
                UnityEngine.Camera mainCamera = UnityEngine.Camera.main;
                if (mainCamera != null)
                {
                    _cameraTransform = mainCamera.transform;
                }
            }

            // カスタマイズサービスから矢タイプを適用
            ApplyArrowTypeFromCustomization();
        }

        private void Start()
        {
            // 注意: 騎乗はPlayerSpawnerが処理します
            // PlayerSpawner.SpawnRider() → RiderController.MountTo() で騎乗
            // AutoMountAtStart() は不要になりました
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

            Debug.Log($"[PlayerController] Auto-mounted on: {nearestHorse.name} at scene start");
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
            }

            // 重力適用
            _velocity.y += _gravity * Time.deltaTime;
            _characterController.Move(_velocity * Time.deltaTime);

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

                // RiderControllerでチャージ量を更新
                _riderController?.SetChargeAmount(_currentCharge);

                // 矢を発射（ボタン離した時）
                if (_inputService.GetAttackButtonUp())
                {
                    FireArrow(_currentCharge);
                    _isCharging = false;
                    _currentCharge = 0f;
                    _riderController?.SetChargeAmount(0f);

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

            // エイムアニメーションを開始
            _riderController?.SetAnimationState(RiderAnimationState.Aiming);

            // TrainingManagerにチャージ開始を通知
            if (TrainingManager.Instance != null)
            {
                TrainingManager.Instance.NotifyChargingStarted();
            }
            else
            {
                Debug.LogWarning("[PlayerController] TrainingManager.Instance is null! Add TrainingManager to the scene.");
            }

            Debug.Log("[PlayerController] Charge started");
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

            // TrainingManagerにチャージ終了を通知
            TrainingManager.Instance?.NotifyChargingEnded();

            Debug.Log("[PlayerController] Charge canceled");
        }

        /// <summary>
        /// 矢を発射します
        /// </summary>
        /// <param name="chargeAmount">チャージ量（0.0～1.0）</param>
        private void FireArrow(float chargeAmount)
        {
            // カスタマイズで設定された矢プレハブを優先、なければデフォルトを使用
            GameObject? arrowPrefabToUse = _currentArrowPrefab ?? _arrowPrefab;

            if (arrowPrefabToUse == null || _bowFirePoint == null)
            {
                Debug.LogWarning("[PlayerController] Arrow prefab or bow fire point not assigned!");
                return;
            }

            // マズルエフェクトを生成（MasterStylizedProjectiles）
            SpawnMuzzleEffect();

            // チャージ量に応じた矢の速度を計算
            float arrowSpeed = Mathf.Lerp(_minArrowSpeed, _maxArrowSpeed, chargeAmount);
            Vector3 velocity = _bowFirePoint.forward * arrowSpeed;

            // 矢の親オブジェクトを作成（スケール1,1,1を維持、物理演算用）
            GameObject arrowParent = new GameObject("Arrow");
            arrowParent.transform.position = _bowFirePoint.position;
            arrowParent.transform.rotation = _bowFirePoint.rotation;

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
                arrowProjectile.SetVelocity(velocity);

                // チャージ量も設定（スコア計算に使用）
                arrowProjectile.SetChargeAmount(chargeAmount);

            // ヒットエフェクトを設定（ArrowTypeConfigから）
                    arrowProjectile.SetHitEffectPrefab(_currentHitEffectPrefab);

            // 発射者を設定（自分自身との衝突を無視するため）
            arrowProjectile.SetOwner(gameObject);

            // Rigidbodyを取得して設定（ArrowProjectileのRequireComponentで自動追加済み）
            Rigidbody arrowRb = arrowParent.GetComponent<Rigidbody>();
            arrowRb.useGravity = true;
            arrowRb.linearVelocity = velocity;

            // コライダーを親に追加（矢の当たり判定）
            var collider = arrowParent.AddComponent<SphereCollider>();
            collider.radius = 0.1f;
            collider.isTrigger = true;

            // 射撃アニメーション再生
            _riderController?.SetAnimationState(RiderAnimationState.Shooting);

            // 射撃音を再生
            PlayShootSound();

            // TrainingManagerに通知
            TrainingManager.Instance?.RecordArrowFired();

            Debug.Log($"[PlayerController] Arrow fired! Type: {_currentArrowType}, Charge: {chargeAmount:F2}, Speed: {arrowSpeed:F1}");
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

        #region Arrow Customization

        /// <summary>
        /// カスタマイズサービスから矢タイプを適用します
        /// </summary>
        private void ApplyArrowTypeFromCustomization()
        {
            var customizationService = ServiceLocator.Instance.Get<ICustomizationService>();
            if (customizationService == null)
            {
                Debug.Log("[PlayerController] ICustomizationService が取得できませんでした。デフォルトの矢を使用します。");
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
                Debug.LogWarning("[PlayerController] ArrowTypeConfig が設定されていません。デフォルトの矢を使用します。");
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

            Debug.Log($"[PlayerController] 矢タイプを設定: {arrowType} (Arrow: {(_currentArrowPrefab != null ? _currentArrowPrefab.name : "null")})");
        }

        /// <summary>
        /// 現在の矢タイプを取得します
        /// </summary>
        public ArrowType CurrentArrowType => _currentArrowType;

        #endregion
    }
}
