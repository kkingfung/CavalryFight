#nullable enable

using UnityEngine;
using Cinemachine;
using CavalryFight.Core.Services;
using CavalryFight.Services.Input;

namespace CavalryFight.Gameplay.Camera
{
    /// <summary>
    /// プレイヤーカメラの制御（Third Person / First Person切り替え）
    /// </summary>
    /// <remarks>
    /// Cinemachine Virtual Cameraを使用して、Third PersonとFirst Personの視点を切り替えます。
    /// カメラ切り替えはToggleCameraボタン（デフォルト: C）で実行されます。
    /// </remarks>
    public class PlayerCameraController : MonoBehaviour
    {
        #region Serialized Fields

        [Header("Virtual Cameras")]
        [SerializeField] private CinemachineVirtualCamera? _thirdPersonCamera;
        [SerializeField] private CinemachineVirtualCamera? _firstPersonCamera;

        [Header("Camera Settings")]
        [SerializeField] private float _mouseSensitivity = 2f;
        [SerializeField] private float _gamepadSensitivity = 100f;
        [SerializeField] private float _minVerticalAngle = -80f;
        [SerializeField] private float _maxVerticalAngle = 80f;

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
            // カメラターゲットを設定
            if (_followTarget != null && _lookAtTarget != null)
            {
                if (_thirdPersonCamera != null)
                {
                    _thirdPersonCamera.Follow = _followTarget;
                    _thirdPersonCamera.LookAt = _lookAtTarget;
                }

                if (_firstPersonCamera != null)
                {
                    _firstPersonCamera.Follow = _followTarget;
                    _firstPersonCamera.LookAt = _lookAtTarget;
                }
            }
        }

        private void Update()
        {
            if (_inputService == null || !_inputService.InputEnabled)
            {
                return;
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
                Debug.Log($"[PlayerCameraController] Charge ended - switched to {_defaultMode}");
            }
        }

        /// <summary>
        /// カメラ回転処理
        /// </summary>
        private void HandleCameraRotation()
        {
            if (_inputService == null)
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
            if (transform == null)
            {
                return;
            }

            // カメラのTransformに回転を適用
            transform.rotation = Quaternion.Euler(_verticalRotation, _horizontalRotation, 0f);
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

            if (_thirdPersonCamera != null)
            {
                _thirdPersonCamera.Follow = _followTarget;
                _thirdPersonCamera.LookAt = _lookAtTarget;
            }

            if (_firstPersonCamera != null)
            {
                _firstPersonCamera.Follow = _followTarget;
                _firstPersonCamera.LookAt = _lookAtTarget;
            }
        }

        /// <summary>
        /// プレイヤーコントローラーを設定します
        /// </summary>
        /// <param name="playerController">プレイヤーコントローラー</param>
        public void SetPlayerController(Player.PlayerController playerController)
        {
            _playerController = playerController;
        }

        #endregion
    }
}
