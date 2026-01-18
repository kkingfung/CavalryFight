#nullable enable

using UnityEngine;
using CavalryFight.Core.Services;
using CavalryFight.Services.Input;
using InputHelper = UnityEngine.Input;

namespace CavalryFight.Services.Replay
{
    /// <summary>
    /// フリーカメラコントローラー
    /// </summary>
    /// <remarks>
    /// リプレイ中にプレイヤーが自由にカメラを操作できます。
    /// IInputServiceを使用して移動・カメラ入力を取得します。
    /// スクロールで速度変更が可能です。
    /// スクリーンショット撮影に最適です。
    /// </remarks>
    public class FreeCameraController : IReplayCameraController
    {
        #region Fields

        private Camera? _camera;
        private bool _enabled = true;
        private Vector3 _initialPosition;
        private Quaternion _initialRotation;
        private IInputService? _inputService;
        private IInputBindingService? _inputBindingService;

        // 移動設定
        private float _moveSpeed = 5f;
        private float _fastMoveSpeed = 15f;
        private float _slowMoveSpeed = 1f;
        private float _currentMoveSpeed;

        // 境界設定
        private Vector3 _boundaryMin = new Vector3(-100f, 1f, -100f);
        private Vector3 _boundaryMax = new Vector3(100f, 50f, 100f);

        // 視点設定
        private float _lookSensitivity = 100f;
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

        /// <summary>
        /// 境界の最小値を取得または設定します
        /// </summary>
        public Vector3 BoundaryMin
        {
            get { return _boundaryMin; }
            set { _boundaryMin = value; }
        }

        /// <summary>
        /// 境界の最大値を取得または設定します
        /// </summary>
        public Vector3 BoundaryMax
        {
            get { return _boundaryMax; }
            set { _boundaryMax = value; }
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

            // 初期位置のNaNチェック
            if (float.IsNaN(_initialPosition.x) || float.IsNaN(_initialPosition.y) || float.IsNaN(_initialPosition.z))
            {
                _initialPosition = Vector3.zero;
                Debug.LogWarning("[FreeCameraController] カメラの初期位置がNaNでした。(0,0,0)に設定します。");
            }

            // 初期回転からピッチとヨーを計算
            Vector3 eulerAngles = camera.transform.eulerAngles;
            _pitch = eulerAngles.x;
            _yaw = eulerAngles.y;

            // 速度を初期化
            _currentVelocity = Vector3.zero;
            _currentMoveSpeed = _moveSpeed;

            // InputServiceとInputBindingServiceを取得
            _inputService = ServiceLocator.Instance.Get<IInputService>();
            _inputBindingService = ServiceLocator.Instance.Get<IInputBindingService>();

            if (_inputService == null)
            {
                Debug.LogWarning("[FreeCameraController] InputService not found. Using fallback input.");
            }

            if (_inputBindingService == null)
            {
                Debug.LogWarning("[FreeCameraController] InputBindingService not found. Using default key codes.");
            }

            Debug.Log($"[FreeCameraController] Initialized. Position: {_initialPosition}, MoveSpeed: {_moveSpeed}");
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

            Vector2 cameraInput = Vector2.zero;

            // IInputServiceからカメラ入力を取得（キーバインディングに従う）
            if (_inputService != null)
            {
                cameraInput = _inputService.GetCameraInput();
            }
            else
            {
                // フォールバック: マウス入力を直接使用
                cameraInput.x = InputHelper.GetAxis("Mouse X");
                cameraInput.y = InputHelper.GetAxis("Mouse Y");
            }

            // NaNチェック
            if (float.IsNaN(cameraInput.x) || float.IsNaN(cameraInput.y))
            {
                cameraInput = Vector2.zero;
            }

            // カメラ入力がある場合のみ回転
            if (cameraInput.sqrMagnitude > 0.0001f)
            {
                _yaw += cameraInput.x * _lookSensitivity;
                _pitch -= cameraInput.y * _lookSensitivity;

                // ピッチを-90～90度に制限
                _pitch = Mathf.Clamp(_pitch, -90f, 90f);

                _camera.transform.rotation = Quaternion.Euler(_pitch, _yaw, 0f);
            }
        }

        private void UpdateMovement(float deltaTime)
        {
            if (_camera == null || deltaTime <= 0f)
            {
                return;
            }

            Vector3 moveDirection = Vector3.zero;

            // IInputServiceから移動入力を取得
            Vector2 moveInput = Vector2.zero;

            if (_inputService != null)
            {
                moveInput = _inputService.GetMovementInput();
            }
            else
            {
                // フォールバック: キーバインディングまたはデフォルトキー
                if (IsActionPressed(InputAction.MoveForward))
                {
                    moveInput.y += 1f;
                }
                if (IsActionPressed(InputAction.MoveBackward))
                {
                    moveInput.y -= 1f;
                }
                if (IsActionPressed(InputAction.MoveRight))
                {
                    moveInput.x += 1f;
                }
                if (IsActionPressed(InputAction.MoveLeft))
                {
                    moveInput.x -= 1f;
                }
            }

            // NaNチェック
            if (float.IsNaN(moveInput.x) || float.IsNaN(moveInput.y))
            {
                moveInput = Vector2.zero;
            }

            // カメラの向きを取得（NaNチェック付き）
            Vector3 forward = _camera.transform.forward;
            Vector3 right = _camera.transform.right;

            // カメラの向きがNaNの場合はデフォルト値を使用
            if (float.IsNaN(forward.x) || float.IsNaN(forward.y) || float.IsNaN(forward.z))
            {
                forward = Vector3.forward;
            }
            if (float.IsNaN(right.x) || float.IsNaN(right.y) || float.IsNaN(right.z))
            {
                right = Vector3.right;
            }

            // 移動方向を計算（カメラ向きに基づく）
            moveDirection += forward * moveInput.y;
            moveDirection += right * moveInput.x;

            // 上下移動（MountとJumpを使用）
            bool moveUp = false;
            bool moveDown = false;

            if (_inputService != null)
            {
                // Jump = 上昇、Mount = 下降
                moveUp = _inputService.GetJumpButton();
                moveDown = IsActionPressed(InputAction.Mount);
            }
            else
            {
                moveUp = IsActionPressed(InputAction.Jump);
                moveDown = IsActionPressed(InputAction.Mount);
            }

            if (moveUp)
            {
                moveDirection += Vector3.up;
            }
            if (moveDown)
            {
                moveDirection -= Vector3.up;
            }

            // 正規化
            if (moveDirection.sqrMagnitude > 0.01f)
            {
                moveDirection.Normalize();
            }

            // moveDirectionのNaNチェック
            if (float.IsNaN(moveDirection.x) || float.IsNaN(moveDirection.y) || float.IsNaN(moveDirection.z))
            {
                moveDirection = Vector3.zero;
            }

            // 高速/低速移動モディファイア
            float speed = _currentMoveSpeed;

            if (_inputService != null)
            {
                // SprintでFast、CancelAttack（Ctrl）でSlow
                if (_inputService.GetSprintButton())
                {
                    speed = _fastMoveSpeed;
                }
                else if (IsActionPressed(InputAction.CancelAttack))
                {
                    speed = _slowMoveSpeed;
                }
            }
            else
            {
                if (IsActionPressed(InputAction.Attack))
                {
                    speed = _fastMoveSpeed;
                }
                else if (IsActionPressed(InputAction.CancelAttack))
                {
                    speed = _slowMoveSpeed;
                }
            }

            // 入力がない場合はスムーズに減速
            Vector3 targetVelocity = moveDirection * speed;

            // SmoothDampの代わりにシンプルな補間を使用（NaN防止）
            float lerpFactor = Mathf.Clamp01(deltaTime / _smoothTime);
            _currentVelocity = Vector3.Lerp(_currentVelocity, targetVelocity, lerpFactor);

            // NaNチェック
            if (float.IsNaN(_currentVelocity.x) || float.IsNaN(_currentVelocity.y) || float.IsNaN(_currentVelocity.z))
            {
                _currentVelocity = Vector3.zero;
            }

            // 現在位置を取得（NaNチェック付き）
            Vector3 currentPosition = _camera.transform.position;
            if (float.IsNaN(currentPosition.x) || float.IsNaN(currentPosition.y) || float.IsNaN(currentPosition.z))
            {
                // 現在位置がNaNの場合は初期位置にリセット
                currentPosition = _initialPosition;
                _camera.transform.position = _initialPosition;
                _currentVelocity = Vector3.zero;
                Debug.LogWarning("[FreeCameraController] カメラ位置がNaNでした。初期位置にリセットしました。");
                return;
            }

            // 新しい位置を計算
            Vector3 newPosition = currentPosition + _currentVelocity * deltaTime;

            // NaNチェック
            if (float.IsNaN(newPosition.x) || float.IsNaN(newPosition.y) || float.IsNaN(newPosition.z))
            {
                Debug.LogWarning($"[FreeCameraController] 新しい位置がNaN: velocity={_currentVelocity}, deltaTime={deltaTime}");
                _currentVelocity = Vector3.zero;
                return; // 無効な位置は適用しない
            }

            // 境界内に制限
            newPosition = ClampToBoundary(newPosition);
            _camera.transform.position = newPosition;
        }

        /// <summary>
        /// 位置を境界内に制限します
        /// </summary>
        private Vector3 ClampToBoundary(Vector3 position)
        {
            return new Vector3(
                Mathf.Clamp(position.x, _boundaryMin.x, _boundaryMax.x),
                Mathf.Clamp(position.y, _boundaryMin.y, _boundaryMax.y),
                Mathf.Clamp(position.z, _boundaryMin.z, _boundaryMax.z)
            );
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

            return (binding.PrimaryKey != KeyCode.None && InputHelper.GetKey(binding.PrimaryKey)) ||
                   (binding.SecondaryKey != KeyCode.None && InputHelper.GetKey(binding.SecondaryKey));
        }

        private bool IsDefaultKeyPressed(InputAction action)
        {
            // InputBindingServiceが利用できない場合のフォールバック
            return action switch
            {
                InputAction.MoveForward => InputHelper.GetKey(KeyCode.W),
                InputAction.MoveBackward => InputHelper.GetKey(KeyCode.S),
                InputAction.MoveLeft => InputHelper.GetKey(KeyCode.A),
                InputAction.MoveRight => InputHelper.GetKey(KeyCode.D),
                InputAction.Jump => InputHelper.GetKey(KeyCode.Space),
                InputAction.Mount => InputHelper.GetKey(KeyCode.E),
                InputAction.Attack => InputHelper.GetKey(KeyCode.LeftShift),
                InputAction.CancelAttack => InputHelper.GetKey(KeyCode.LeftControl),
                _ => false
            };
        }

        private void UpdateSpeed()
        {
            // マウススクロールで速度変更
            float scroll = InputHelper.GetAxis("Mouse ScrollWheel");
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
