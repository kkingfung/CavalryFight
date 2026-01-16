#nullable enable

using UnityEngine;
using Unity.Cinemachine;
using CavalryFight.Core.Services;
using CavalryFight.Services.Input;

namespace CavalryFight.Gameplay.Camera
{
    /// <summary>
    /// プレイヤーカメラの制御（Third Person / First Person切り替え）
    /// </summary>
    /// <remarks>
    /// Cinemachine Virtual Cameraを使用して、Third PersonとFirst Personの視点を切り替えます。
    /// カメラ回転はフォローターゲットを回転させることで実現します。
    /// </remarks>
    public class PlayerCameraController : MonoBehaviour
    {
        #region Serialized Fields

        [Header("Virtual Cameras")]
        [SerializeField] private CinemachineCamera? _thirdPersonCamera;
        [SerializeField] private CinemachineCamera? _firstPersonCamera;

        [Header("Camera Settings")]
        [SerializeField] private float _mouseSensitivity = 5f;
        [SerializeField] private float _gamepadSensitivity = 150f;
        [SerializeField] private float _minVerticalAngle = -40f;
        [SerializeField] private float _maxVerticalAngle = 60f;

        [Header("Horizontal Rotation Limits (通常時)")]
        [Tooltip("水平回転制限を有効にするか（騎乗時は馬の向き基準で制限）")]
        [SerializeField] private bool _enableHorizontalLimits = true;
        [Tooltip("馬の向きからの最大水平回転角度（左右）")]
        [SerializeField] private float _maxHorizontalAngleFromMount = 150f;

        [Header("Aiming Camera Limits (エイム時)")]
        [Tooltip("エイム時の垂直角度制限（下方向）- RiderAimControllerと同じ値にすること")]
        [SerializeField] private float _aimMinVerticalAngle = -30f;
        [Tooltip("エイム時の垂直角度制限（上方向）- RiderAimControllerと同じ値にすること")]
        [SerializeField] private float _aimMaxVerticalAngle = 60f;
        [Tooltip("エイム時の水平角度制限（左方向、負の値）")]
        [SerializeField] private float _aimMinHorizontalAngle = -35f;
        [Tooltip("エイム時の水平角度制限（右方向、正の値）")]
        [SerializeField] private float _aimMaxHorizontalAngle = 125f;

        [Header("Smoothing")]
        [Tooltip("カメラ位置の追従スムージング（高いほど滑らか、0で即座に追従）")]
        [SerializeField] private float _positionLerpSpeed = 10f;
        [Tooltip("カメラ回転のスムージング（高いほど滑らか、0で即座に回転）")]
        [SerializeField] private float _rotationLerpSpeed = 15f;

        [Header("Target")]
        [SerializeField] private Transform? _followTarget;
        [SerializeField] private Transform? _lookAtTarget;

        [Header("Player Reference")]
        [SerializeField] private Player.PlayerController? _playerController;

        #endregion

        #region Private Fields

        private IInputService? _inputService;
        private CameraMode _currentMode = CameraMode.ThirdPerson;
        private CameraMode _defaultMode = CameraMode.ThirdPerson;
        private bool _isAttacking = false;
        private float _verticalRotation;
        private float _horizontalRotation;

        // カメラ回転用のピボット（フォローターゲットとして使用）- ワールド空間に配置
        private Transform? _rotationPivot;

        // 追従対象（馬またはライダー）
        private Transform? _actualTarget;

        // 馬のTransform（水平回転制限の基準）
        private Transform? _mountTransform;

        // FirstPerson用のターゲット（弓の発射位置）
        private Transform? _firstPersonTarget;

        #endregion

        #region Enums

        /// <summary>
        /// カメラモード
        /// </summary>
        public enum CameraMode
        {
            ThirdPerson,
            FirstPerson
        }

        #endregion

        #region Unity Lifecycle

        private void Awake()
        {
            _inputService = ServiceLocator.Instance.Get<IInputService>();

            if (_inputService == null)
            {
                Debug.LogError("[PlayerCameraController] IInputService が取得できませんでした！");
            }

            // 初期状態をThird Personに設定
            SetCameraMode(CameraMode.ThirdPerson);
        }

        private void Start()
        {
            // カーソルをロック
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }

        private void OnDestroy()
        {
            // 回転ピボットをクリーンアップ
            if (_rotationPivot != null)
            {
                Destroy(_rotationPivot.gameObject);
                _rotationPivot = null;
            }
        }

        private void LateUpdate()
        {
            // ピボット位置の更新（入力無効時でも行う）
            // LateUpdateで実行することで、アニメーション・物理演算後の位置を使用
            UpdatePivotPosition();

            if (_inputService == null || !_inputService.InputEnabled)
            {
                return;
            }

            // デバッグ: PlayerControllerの状態を確認
            if (_playerController == null)
            {
                // PlayerControllerがまだ設定されていない場合は何もしない（警告は出さない）
            }

            HandleAutoAimCamera();
            HandleCameraRotation();
        }

        /// <summary>
        /// ピボット位置をターゲットにスムーズに追従させます
        /// </summary>
        private void UpdatePivotPosition()
        {
            // Unity特有: DestroyされたオブジェクトはC#参照としてはnullではないが、
            // Unityの==演算子でnullと判定される。両方チェックする
            if (_rotationPivot == null)
            {
                // 最初の数フレームはまだ設定されていない可能性がある
                // 連続で出力しないように1回だけ警告
                return;
            }

            // ターゲットが破棄されている場合はスキップ
            // Unity の == 演算子は破棄されたオブジェクトを null と判定する
            if (_actualTarget == null)
            {
                return;
            }

            // 追加チェック: GameObjectがアクティブかどうか確認
            if (!_actualTarget.gameObject.activeInHierarchy)
            {
                return;
            }

            // ターゲット位置を取得
            Vector3 targetPosition = _actualTarget.position;

            // スムーズに追従（lerpSpeedが0の場合は即座に追従）
            if (_positionLerpSpeed > 0)
            {
                _rotationPivot.position = Vector3.Lerp(
                    _rotationPivot.position,
                    targetPosition,
                    _positionLerpSpeed * Time.deltaTime
                );
            }
            else
            {
                _rotationPivot.position = targetPosition;
            }
        }

        #endregion

        #region Camera Control

        /// <summary>
        /// 攻撃時の自動エイムカメラ処理
        /// </summary>
        /// <remarks>
        /// チャージ中はFirst Personに切り替わり、
        /// チャージ終了（発射またはキャンセル）時にデフォルトモードに戻ります。
        /// </remarks>
        private void HandleAutoAimCamera()
        {
            if (_playerController == null)
            {
                return;
            }

            bool isChargingNow = _playerController.IsCharging;

            // チャージ開始
            if (isChargingNow && !_isAttacking)
            {
                _isAttacking = true;
                SetCameraMode(CameraMode.FirstPerson);
                Debug.Log("[PlayerCameraController] Charge started - switched to First Person");
            }
            // チャージ終了（発射またはキャンセル）
            else if (!isChargingNow && _isAttacking)
            {
                _isAttacking = false;
                SetCameraMode(_defaultMode);

                // 垂直回転をリセット（前方を向くように）
                _verticalRotation = 0f;
                ApplyCameraRotation();

                Debug.Log($"[PlayerCameraController] Charge ended - switched to {_defaultMode}");
            }
        }

        /// <summary>
        /// カメラ回転処理
        /// </summary>
        private void HandleCameraRotation()
        {
            if (_inputService == null || _rotationPivot == null)
            {
                return;
            }

            // カメラ入力を取得
            Vector2 cameraInput = _inputService.GetCameraInput();

            bool hasInput = cameraInput.magnitude >= 0.01f;

            if (hasInput)
            {
                // 感度を適用（マウスかゲームパッドかで異なる）
                float sensitivity = _mouseSensitivity;

                // ゲームパッドの場合はより高い感度を使用
                if (Mathf.Abs(cameraInput.x) > 1f || Mathf.Abs(cameraInput.y) > 1f)
                {
                    sensitivity = _gamepadSensitivity * Time.deltaTime;
                }

                // 水平・垂直回転を計算
                _horizontalRotation += cameraInput.x * sensitivity;
                _verticalRotation -= cameraInput.y * sensitivity;
            }

            // 垂直回転を制限（エイム中はより厳しい制限）
            if (_isAttacking)
            {
                _verticalRotation = Mathf.Clamp(_verticalRotation, _aimMinVerticalAngle, _aimMaxVerticalAngle);
            }
            else
            {
                _verticalRotation = Mathf.Clamp(_verticalRotation, _minVerticalAngle, _maxVerticalAngle);
            }

            // 水平回転を制限（馬の向き基準）- 入力がなくても常に適用（馬が回転した場合に追従）
            if (_enableHorizontalLimits && _mountTransform != null)
            {
                // 馬の現在の向き（Y軸回転のみ）
                float mountYRotation = _mountTransform.eulerAngles.y;

                // カメラの水平回転と馬の向きの差を計算
                float angleDiff = Mathf.DeltaAngle(mountYRotation, _horizontalRotation);

                // エイム中はより厳しい非対称制限を適用
                if (_isAttacking)
                {
                    // 非対称制限: 左は-35°、右は+125°
                    float clampedAngleDiff = Mathf.Clamp(angleDiff, _aimMinHorizontalAngle, _aimMaxHorizontalAngle);
                    _horizontalRotation = mountYRotation + clampedAngleDiff;
                }
                else
                {
                    // 通常時は対称制限
                    float clampedAngleDiff = Mathf.Clamp(angleDiff, -_maxHorizontalAngleFromMount, _maxHorizontalAngleFromMount);
                    _horizontalRotation = mountYRotation + clampedAngleDiff;
                }
            }

            // カメラの回転を適用（入力または制限がある場合）
            ApplyCameraRotation();
        }

        /// <summary>
        /// カメラ回転をTransformに適用します
        /// </summary>
        private void ApplyCameraRotation()
        {
            if (_rotationPivot == null)
            {
                return;
            }

            // ターゲット回転を計算
            Quaternion targetRotation = Quaternion.Euler(_verticalRotation, _horizontalRotation, 0f);

            // スムーズに回転（lerpSpeedが0の場合は即座に回転）
            if (_rotationLerpSpeed > 0)
            {
                _rotationPivot.rotation = Quaternion.Lerp(
                    _rotationPivot.rotation,
                    targetRotation,
                    _rotationLerpSpeed * Time.deltaTime
                );
            }
            else
            {
                _rotationPivot.rotation = targetRotation;
            }
        }

        #endregion

        #region Camera Mode

        /// <summary>
        /// デフォルトカメラモードを取得します
        /// </summary>
        public CameraMode DefaultMode => _defaultMode;

        /// <summary>
        /// カメラモードを設定します
        /// </summary>
        /// <param name="mode">設定するカメラモード</param>
        public void SetCameraMode(CameraMode mode)
        {
            _currentMode = mode;

            if (_thirdPersonCamera == null || _firstPersonCamera == null)
            {
                Debug.LogWarning("[PlayerCameraController] Virtual Cameras are not assigned!");
                return;
            }

            switch (mode)
            {
                case CameraMode.ThirdPerson:
                    _thirdPersonCamera.Priority = 10;
                    _firstPersonCamera.Priority = 5;
                    Debug.Log("[PlayerCameraController] Switched to Third Person camera.");
                    break;

                case CameraMode.FirstPerson:
                    _thirdPersonCamera.Priority = 5;
                    _firstPersonCamera.Priority = 10;
                    Debug.Log("[PlayerCameraController] Switched to First Person camera.");
                    break;
            }
        }

        /// <summary>
        /// 現在のカメラモードを取得します
        /// </summary>
        public CameraMode CurrentMode => _currentMode;

        #endregion

        #region Public Methods

        /// <summary>
        /// フォロー対象とルックアット対象を設定します
        /// </summary>
        /// <param name="followTarget">フォロー対象のTransform</param>
        /// <param name="lookAtTarget">ルックアット対象のTransform</param>
        public void SetTargets(Transform followTarget, Transform lookAtTarget)
        {
            _followTarget = followTarget;
            _lookAtTarget = lookAtTarget;

            // CameraManagerからはCameraFollow_Offset（ライダーの子）が渡される
            // ワールド空間のピボットが追従すべきは、このオフセットオブジェクトの親（ライダー本体）
            // または、オフセットオブジェクトの位置を直接追従する
            Transform target = followTarget;

            // オフセットオブジェクトの親がある場合、それを実際のターゲットとして使用
            // これにより、ライダーの位置を追従できる
            if (followTarget.parent != null)
            {
                target = followTarget.parent;
            }

            // 回転ピボットを作成（ワールド空間に配置）
            CreateRotationPivot(target);

            // 初期回転をターゲットの向きに合わせる
            _horizontalRotation = target.eulerAngles.y;
        }

        /// <summary>
        /// 回転ピボットを作成します（ワールド空間に配置し、位置はターゲットにスムーズに追従）
        /// </summary>
        /// <param name="target">ターゲット（PlayerRider）のTransform</param>
        private void CreateRotationPivot(Transform target)
        {
            // 既存のピボットを削除（即座に削除してnullクリア）
            if (_rotationPivot != null)
            {
                DestroyImmediate(_rotationPivot.gameObject);
                _rotationPivot = null;
            }

            // 追従対象を保存
            _actualTarget = target;

            Debug.Log($"[PlayerCameraController] CreateRotationPivot - Target: {target.name}, Position: {target.position}, Parent: {target.parent?.name ?? "null"}");

            // 回転ピボットをワールド空間に作成（親なし - これが重要！）
            // 親に設定しないことで、馬のバウンスを継承しない
            GameObject pivotObj = new GameObject("CameraRotationPivot");
            pivotObj.transform.SetParent(null); // ワールド空間に配置
            pivotObj.transform.position = target.position;
            pivotObj.transform.rotation = Quaternion.identity;

            // DontDestroyOnLoadを設定（シーン切り替え時に破棄されないように）
            DontDestroyOnLoad(pivotObj);

            _rotationPivot = pivotObj.transform;

            // フォロー用のオフセットオブジェクトをピボットの子として作成
            // ライダーの頭の高さあたりにカメラを配置
            GameObject followOffset = new GameObject("CameraFollow");
            followOffset.transform.SetParent(_rotationPivot);
            followOffset.transform.localPosition = new Vector3(0f, 1.5f, 0f); // ライダーからの相対的な高さ
            followOffset.transform.localRotation = Quaternion.identity;

            // ルックアット用のオフセットオブジェクト（前方に配置してカメラが前を向くようにする）
            GameObject lookAtOffset = new GameObject("CameraLookAt");
            lookAtOffset.transform.SetParent(_rotationPivot);
            lookAtOffset.transform.localPosition = new Vector3(0f, 1.5f, 10f); // 前方10mに配置
            lookAtOffset.transform.localRotation = Quaternion.identity;

            // Cinemachineカメラにターゲットを設定
            if (_thirdPersonCamera != null)
            {
                _thirdPersonCamera.Follow = followOffset.transform;
                _thirdPersonCamera.LookAt = lookAtOffset.transform;
            }

            if (_firstPersonCamera != null)
            {
                _firstPersonCamera.Follow = followOffset.transform;
                _firstPersonCamera.LookAt = lookAtOffset.transform;
            }
        }

        /// <summary>
        /// プレイヤーコントローラーを設定します
        /// </summary>
        /// <param name="playerController">プレイヤーコントローラー</param>
        public void SetPlayerController(Player.PlayerController playerController)
        {
            _playerController = playerController;

            // 弓の発射位置を取得
            _firstPersonTarget = playerController.BowFirePoint;

            // FirstPersonカメラはThirdPersonと同じピボットを使用
            // これにより、回転入力がFirst Personモードでも有効になる
            // SetTargetsで設定されたピボットのCameraFollow/CameraLookAtを使用
        }

        /// <summary>
        /// 馬のTransformを設定します（水平回転制限の基準）
        /// </summary>
        /// <param name="mountTransform">馬のTransform</param>
        public void SetMountTransform(Transform mountTransform)
        {
            _mountTransform = mountTransform;
            Debug.Log($"[PlayerCameraController] MountTransform set: {mountTransform?.name ?? "null"}");
        }

        #endregion
    }
}
