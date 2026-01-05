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

        // カメラ回転用のピボット（フォローターゲットとして使用）
        private Transform? _rotationPivot;

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

        private void Update()
        {
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

            if (cameraInput.magnitude < 0.01f)
            {
                return;
            }

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

            // 垂直回転を制限
            _verticalRotation = Mathf.Clamp(_verticalRotation, _minVerticalAngle, _maxVerticalAngle);

            // カメラの回転を適用
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

            // 回転ピボットを回転させる
            // Cinemachineカメラはこのピボットをフォローするため、カメラも回転する
            _rotationPivot.rotation = Quaternion.Euler(_verticalRotation, _horizontalRotation, 0f);
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

            Debug.Log($"[PlayerCameraController] SetTargets called: followTarget={followTarget.name}, lookAt={lookAtTarget.name}");

            // ターゲットを直接使用（CameraManagerからはPlayerRiderが渡される）
            // 階層: Mount -> MountPoint -> PlayerRider (followTarget)
            // または: Mount -> CameraFollow_Offset (旧形式)
            Transform target = followTarget;

            Debug.Log($"[PlayerCameraController] Target: {target.name}");

            // 回転ピボットをターゲットの子として作成
            CreateRotationPivot(target);

            // 初期回転をターゲットの向きに合わせる
            _horizontalRotation = target.eulerAngles.y;

            Debug.Log($"[PlayerCameraController] Initial horizontal rotation: {_horizontalRotation}");
        }

        /// <summary>
        /// 回転ピボットを作成します（ターゲットの子として）
        /// </summary>
        /// <param name="target">ターゲット（PlayerRider）のTransform</param>
        private void CreateRotationPivot(Transform target)
        {
            // 既存のピボットを削除
            if (_rotationPivot != null)
            {
                Destroy(_rotationPivot.gameObject);
            }

            // 回転ピボットを作成（PlayerRiderの子として配置）
            GameObject pivotObj = new GameObject("CameraRotationPivot");
            pivotObj.transform.SetParent(target);
            pivotObj.transform.localPosition = Vector3.zero;
            pivotObj.transform.localRotation = Quaternion.identity;

            _rotationPivot = pivotObj.transform;

            // フォロー用のオフセットオブジェクトをピボットの子として作成
            // ライダーの頭の高さあたりにカメラを配置
            GameObject followOffset = new GameObject("CameraFollow");
            followOffset.transform.SetParent(_rotationPivot);
            followOffset.transform.localPosition = new Vector3(0f, 0.5f, 0f); // ライダーからの相対的な高さ
            followOffset.transform.localRotation = Quaternion.identity;

            // ルックアット用のオフセットオブジェクト（前方に配置してカメラが前を向くようにする）
            GameObject lookAtOffset = new GameObject("CameraLookAt");
            lookAtOffset.transform.SetParent(_rotationPivot);
            lookAtOffset.transform.localPosition = new Vector3(0f, 0.5f, 10f); // 前方10mに配置
            lookAtOffset.transform.localRotation = Quaternion.identity;

            // Cinemachineカメラにターゲットを設定
            if (_thirdPersonCamera != null)
            {
                _thirdPersonCamera.Follow = followOffset.transform;
                _thirdPersonCamera.LookAt = lookAtOffset.transform;
                Debug.Log($"[PlayerCameraController] ThirdPersonCamera Follow={followOffset.name}, LookAt={lookAtOffset.name}");
            }

            if (_firstPersonCamera != null)
            {
                _firstPersonCamera.Follow = followOffset.transform;
                _firstPersonCamera.LookAt = lookAtOffset.transform;
                Debug.Log($"[PlayerCameraController] FirstPersonCamera targets set");
            }

            Debug.Log($"[PlayerCameraController] Rotation pivot created as child of: {target.name}");
        }

        /// <summary>
        /// プレイヤーコントローラーを設定します
        /// </summary>
        /// <param name="playerController">プレイヤーコントローラー</param>
        public void SetPlayerController(Player.PlayerController playerController)
        {
            _playerController = playerController;

            // 弓の発射位置を取得してFirstPersonカメラのターゲットに設定
            _firstPersonTarget = playerController.BowFirePoint;

            if (_firstPersonTarget != null && _firstPersonCamera != null)
            {
                // FirstPersonカメラは弓の発射位置をフォロー
                _firstPersonCamera.Follow = _firstPersonTarget;

                // LookAtは発射方向（前方）
                // 発射位置の前方を向くために、発射位置自体をLookAtにするか、
                // 別のオブジェクトを作成する
                // ここではFollowと同じ位置にして、カメラの向きは発射位置の回転に従う
                _firstPersonCamera.LookAt = null; // LookAtを無効にしてFollowの回転に従う

                Debug.Log($"[PlayerCameraController] FirstPersonCamera set to BowFirePoint: {_firstPersonTarget.name}");
            }
            else if (_firstPersonTarget == null)
            {
                Debug.LogWarning("[PlayerCameraController] BowFirePoint not found on PlayerController!");
            }
        }

        #endregion
    }
}
