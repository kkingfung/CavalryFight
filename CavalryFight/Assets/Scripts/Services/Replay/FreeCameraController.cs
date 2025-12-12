#nullable enable

using UnityEngine;
using CavalryFight.Core.Services;
using CavalryFight.Services.Input;

namespace CavalryFight.Services.Replay
{
    /// <summary>
    /// フリーカメラコントローラー
    /// </summary>
    /// <remarks>
    /// リプレイ中にプレイヤーが自由にカメラを操作できます。
    /// キーバインディング設定に従って移動、マウスで視点変更、スクロールで速度変更が可能です。
    /// スクリーンショット撮影に最適です。
    /// </remarks>
    public class FreeCameraController : IReplayCameraController
    {
        #region Fields

        private Camera? _camera;
        private bool _enabled = true;
        private Vector3 _initialPosition;
        private Quaternion _initialRotation;
        private IInputBindingService? _inputBindingService;

        // 移動設定
        private float _moveSpeed = 10f;
        private float _fastMoveSpeed = 50f;
        private float _slowMoveSpeed = 2f;
        private float _currentMoveSpeed;

        // 視点設定
        private float _lookSensitivity = 2f;
        private float _pitch = 0f;
        private float _yaw = 0f;

        // スムージング
        private float _smoothTime = 0.1f;
        private Vector3 _currentVelocity = Vector3.zero;

        #endregion

        #region Properties

        /// <summary>
        /// カメラが有効かどうかを取得または設定します
        /// </summary>
        public bool Enabled
        {
            get { return _enabled; }
            set { _enabled = value; }
        }

        /// <summary>
        /// 移動速度を取得または設定します（m/s）
        /// </summary>
        public float MoveSpeed
        {
            get { return _moveSpeed; }
            set { _moveSpeed = Mathf.Max(0.1f, value); }
        }

        /// <summary>
        /// 視点感度を取得または設定します
        /// </summary>
        public float LookSensitivity
        {
            get { return _lookSensitivity; }
            set { _lookSensitivity = Mathf.Max(0.1f, value); }
        }

        #endregion

        #region Constructor

        /// <summary>
        /// FreeCameraControllerの新しいインスタンスを初期化します
        /// </summary>
        public FreeCameraController()
        {
            _currentMoveSpeed = _moveSpeed;
        }

        #endregion

        #region IReplayCameraController Implementation

        /// <summary>
        /// カメラを初期化します
        /// </summary>
        /// <param name="camera">制御するカメラ</param>
        public void Initialize(Camera camera)
        {
            _camera = camera;
            _initialPosition = camera.transform.position;
            _initialRotation = camera.transform.rotation;

            // 初期回転からピッチとヨーを計算
            Vector3 eulerAngles = camera.transform.eulerAngles;
            _pitch = eulerAngles.x;
            _yaw = eulerAngles.y;

            // InputBindingServiceを取得
            _inputBindingService = ServiceLocator.Instance.Get<IInputBindingService>();

            if (_inputBindingService == null)
            {
                Debug.LogWarning("[FreeCameraController] InputBindingService not found. Using default key codes.");
            }

            Debug.Log("[FreeCameraController] Initialized.");
        }

        /// <summary>
        /// カメラの更新処理
        /// </summary>
        /// <param name="deltaTime">前フレームからの経過時間</param>
        /// <param name="currentFrame">現在のリプレイフレーム</param>
        public void UpdateCamera(float deltaTime, ReplayFrame? currentFrame)
        {
            if (!_enabled || _camera == null)
            {
                return;
            }

            // マウスルック
            UpdateLook();

            // 移動
            UpdateMovement(deltaTime);

            // 速度変更（マウススクロール）
            UpdateSpeed();
        }

        /// <summary>
        /// カメラをリセットします（初期位置に戻す）
        /// </summary>
        public void Reset()
        {
            if (_camera == null)
            {
                return;
            }

            _camera.transform.position = _initialPosition;
            _camera.transform.rotation = _initialRotation;

            Vector3 eulerAngles = _camera.transform.eulerAngles;
            _pitch = eulerAngles.x;
            _yaw = eulerAngles.y;
            _currentVelocity = Vector3.zero;

            Debug.Log("[FreeCameraController] Reset to initial position.");
        }

        /// <summary>
        /// カメラを破棄します
        /// </summary>
        public void Dispose()
        {
            _camera = null;
            Debug.Log("[FreeCameraController] Disposed.");
        }

        #endregion

        #region Private Methods

        private void UpdateLook()
        {
            if (_camera == null)
            {
                return;
            }

            // 右クリックでカメラ回転
            if (Input.GetMouseButton(1))
            {
                float mouseX = Input.GetAxis("Mouse X") * _lookSensitivity;
                float mouseY = Input.GetAxis("Mouse Y") * _lookSensitivity;

                _yaw += mouseX;
                _pitch -= mouseY;

                // ピッチを-90～90度に制限
                _pitch = Mathf.Clamp(_pitch, -90f, 90f);

                _camera.transform.rotation = Quaternion.Euler(_pitch, _yaw, 0f);
            }
        }

        private void UpdateMovement(float deltaTime)
        {
            if (_camera == null)
            {
                return;
            }

            Vector3 moveDirection = Vector3.zero;

            // 前後左右移動（キーバインディング対応）
            if (IsActionPressed(InputAction.MoveForward))
            {
                moveDirection += _camera.transform.forward;
            }
            if (IsActionPressed(InputAction.MoveBackward))
            {
                moveDirection -= _camera.transform.forward;
            }
            if (IsActionPressed(InputAction.MoveRight))
            {
                moveDirection += _camera.transform.right;
            }
            if (IsActionPressed(InputAction.MoveLeft))
            {
                moveDirection -= _camera.transform.right;
            }

            // 上下移動（キーバインディング対応）
            if (IsActionPressed(InputAction.Mount))
            {
                moveDirection += Vector3.up;
            }
            if (IsActionPressed(InputAction.Jump))
            {
                moveDirection -= Vector3.up;
            }

            // 正規化
            if (moveDirection.sqrMagnitude > 0.01f)
            {
                moveDirection.Normalize();
            }

            // 高速/低速移動モディファイア（キーバインディング対応）
            float speed = _currentMoveSpeed;
            if (IsActionPressed(InputAction.Attack))
            {
                speed = _fastMoveSpeed;
            }
            else if (IsActionPressed(InputAction.CancelAttack))
            {
                speed = _slowMoveSpeed;
            }

            // スムージング付き移動
            Vector3 targetVelocity = moveDirection * speed;
            Vector3 smoothVelocity = Vector3.SmoothDamp(_currentVelocity, targetVelocity, ref _currentVelocity, _smoothTime);
            _camera.transform.position += smoothVelocity * deltaTime;
        }

        private bool IsActionPressed(InputAction action)
        {
            if (_inputBindingService == null)
            {
                // フォールバック：デフォルトキーを使用
                return IsDefaultKeyPressed(action);
            }

            var binding = _inputBindingService.GetBinding(action);
            if (binding == null)
            {
                return false;
            }

            return Input.GetKey(binding.PrimaryKey) ||
                   (binding.SecondaryKey.HasValue && Input.GetKey(binding.SecondaryKey.Value));
        }

        private bool IsDefaultKeyPressed(InputAction action)
        {
            // InputBindingServiceが利用できない場合のフォールバック
            return action switch
            {
                InputAction.MoveForward => Input.GetKey(KeyCode.W),
                InputAction.MoveBackward => Input.GetKey(KeyCode.S),
                InputAction.MoveLeft => Input.GetKey(KeyCode.A),
                InputAction.MoveRight => Input.GetKey(KeyCode.D),
                InputAction.Jump => Input.GetKey(KeyCode.Q),
                InputAction.Mount => Input.GetKey(KeyCode.E),
                InputAction.Attack => Input.GetKey(KeyCode.LeftShift),
                InputAction.CancelAttack => Input.GetKey(KeyCode.LeftControl),
                _ => false
            };
        }

        private void UpdateSpeed()
        {
            // マウススクロールで速度変更
            float scroll = Input.GetAxis("Mouse ScrollWheel");
            if (Mathf.Abs(scroll) > 0.01f)
            {
                _currentMoveSpeed += scroll * 10f;
                _currentMoveSpeed = Mathf.Clamp(_currentMoveSpeed, _slowMoveSpeed, _fastMoveSpeed);
                Debug.Log($"[FreeCameraController] Move speed: {_currentMoveSpeed:F1} m/s");
            }
        }

        #endregion
    }
}
