#nullable enable

using UnityEngine;

namespace CavalryFight.Services.Replay
{
    /// <summary>
    /// シネマティックカメラコントローラー
    /// </summary>
    /// <remarks>
    /// リプレイを自動的にシネマティックな角度で撮影します。
    /// プレイヤーを追従し、アクションを効果的に捉えます。
    /// リザルトシーンでのスコア再生に最適です。
    /// </remarks>
    public class CinematicCameraController : IReplayCameraController
    {
        #region Enums

        /// <summary>
        /// シネマティックカメラのモード
        /// </summary>
        public enum CinematicMode
        {
            /// <summary>
            /// プレイヤーを追従
            /// </summary>
            FollowPlayer,

            /// <summary>
            /// プレイヤーの周りを周回
            /// </summary>
            OrbitPlayer,

            /// <summary>
            /// アクションの中心を注視
            /// </summary>
            FocusAction
        }

        #endregion

        #region Fields

        private Camera? _camera;
        private bool _enabled = true;
        private Vector3 _initialPosition;
        private Quaternion _initialRotation;

        // 追従設定
        private CinematicMode _currentMode = CinematicMode.FollowPlayer;
        private float _followDistance = 8f;
        private float _followHeight = 3f;
        private float _followSmoothTime = 0.3f;
        private Vector3 _followVelocity = Vector3.zero;

        // 周回設定
        private float _orbitRadius = 10f;
        private float _orbitHeight = 3f;
        private float _orbitSpeed = 20f; // 度/秒
        private float _orbitAngle = 0f;

        // 視点設定
        private float _lookSmoothTime = 0.2f;

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
        /// シネマティックモードを取得または設定します
        /// </summary>
        public CinematicMode Mode
        {
            get { return _currentMode; }
            set { _currentMode = value; }
        }

        /// <summary>
        /// 追従距離を取得または設定します
        /// </summary>
        public float FollowDistance
        {
            get { return _followDistance; }
            set { _followDistance = Mathf.Max(1f, value); }
        }

        /// <summary>
        /// 周回半径を取得または設定します
        /// </summary>
        public float OrbitRadius
        {
            get { return _orbitRadius; }
            set { _orbitRadius = Mathf.Max(1f, value); }
        }

        /// <summary>
        /// 周回速度を取得または設定します（度/秒）
        /// </summary>
        public float OrbitSpeed
        {
            get { return _orbitSpeed; }
            set { _orbitSpeed = value; }
        }

        #endregion

        #region Constructor

        /// <summary>
        /// CinematicCameraControllerの新しいインスタンスを初期化します
        /// </summary>
        public CinematicCameraController()
        {
        }

        /// <summary>
        /// CinematicCameraControllerの新しいインスタンスを初期化します
        /// </summary>
        /// <param name="mode">シネマティックモード</param>
        public CinematicCameraController(CinematicMode mode)
        {
            _currentMode = mode;
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

            Debug.Log($"[CinematicCameraController] Initialized in {_currentMode} mode.");
        }

        /// <summary>
        /// カメラの更新処理
        /// </summary>
        /// <param name="deltaTime">前フレームからの経過時間</param>
        /// <param name="currentFrame">現在のリプレイフレーム</param>
        public void UpdateCamera(float deltaTime, ReplayFrame? currentFrame)
        {
            if (!_enabled || _camera == null || currentFrame == null)
            {
                return;
            }

            // プレイヤーエンティティを探す
            var playerEntity = GetPlayerEntity(currentFrame);
            if (playerEntity == null)
            {
                return;
            }

            Vector3 playerPosition = playerEntity.Position;

            // モードに応じてカメラ位置を更新
            switch (_currentMode)
            {
                case CinematicMode.FollowPlayer:
                    UpdateFollowMode(deltaTime, playerEntity);
                    break;

                case CinematicMode.OrbitPlayer:
                    UpdateOrbitMode(deltaTime, playerPosition);
                    break;

                case CinematicMode.FocusAction:
                    UpdateFocusActionMode(deltaTime, currentFrame);
                    break;
            }
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
            _followVelocity = Vector3.zero;
            _orbitAngle = 0f;

            Debug.Log("[CinematicCameraController] Reset to initial position.");
        }

        /// <summary>
        /// カメラを破棄します
        /// </summary>
        public void Dispose()
        {
            _camera = null;
            Debug.Log("[CinematicCameraController] Disposed.");
        }

        #endregion

        #region Private Methods - Mode Updates

        private void UpdateFollowMode(float deltaTime, EntitySnapshot playerEntity)
        {
            if (_camera == null)
            {
                return;
            }

            Vector3 playerPosition = playerEntity.Position;
            Vector3 playerForward = playerEntity.Rotation * Vector3.forward;

            // プレイヤーの後ろに配置
            Vector3 targetPosition = playerPosition - playerForward * _followDistance + Vector3.up * _followHeight;

            // スムーズに移動
            _camera.transform.position = Vector3.SmoothDamp(
                _camera.transform.position,
                targetPosition,
                ref _followVelocity,
                _followSmoothTime
            );

            // プレイヤーを注視
            Vector3 lookTarget = playerPosition + Vector3.up * 1.5f; // Player head height
            Vector3 direction = lookTarget - _camera.transform.position;
            if (direction.sqrMagnitude > 0.01f)
            {
                Quaternion targetRotation = Quaternion.LookRotation(direction);
                _camera.transform.rotation = Quaternion.Slerp(
                    _camera.transform.rotation,
                    targetRotation,
                    deltaTime / _lookSmoothTime
                );
            }
        }

        private void UpdateOrbitMode(float deltaTime, Vector3 playerPosition)
        {
            if (_camera == null)
            {
                return;
            }

            // 周回角度を更新
            _orbitAngle += _orbitSpeed * deltaTime;
            if (_orbitAngle >= 360f)
            {
                _orbitAngle -= 360f;
            }

            // 周回位置を計算
            float radians = _orbitAngle * Mathf.Deg2Rad;
            Vector3 offset = new Vector3(
                Mathf.Sin(radians) * _orbitRadius,
                _orbitHeight,
                Mathf.Cos(radians) * _orbitRadius
            );

            Vector3 targetPosition = playerPosition + offset;
            _camera.transform.position = targetPosition;

            // プレイヤーを注視
            Vector3 lookTarget = playerPosition + Vector3.up * 1.5f;
            Vector3 direction = lookTarget - _camera.transform.position;
            if (direction.sqrMagnitude > 0.01f)
            {
                _camera.transform.rotation = Quaternion.LookRotation(direction);
            }
        }

        private void UpdateFocusActionMode(float deltaTime, ReplayFrame currentFrame)
        {
            if (_camera == null)
            {
                return;
            }

            // アクションの中心（全エンティティの平均位置）を計算
            Vector3 centerPosition = CalculateActionCenter(currentFrame);

            // アクションの範囲を計算
            float actionRadius = CalculateActionRadius(currentFrame, centerPosition);

            // カメラ距離を調整（全体が見えるように）
            float targetDistance = Mathf.Max(actionRadius * 2f, 10f);

            // カメラ位置を計算（斜め上から見下ろす）
            Vector3 targetPosition = centerPosition + new Vector3(0f, targetDistance * 0.6f, -targetDistance * 0.8f);

            // スムーズに移動
            _camera.transform.position = Vector3.SmoothDamp(
                _camera.transform.position,
                targetPosition,
                ref _followVelocity,
                _followSmoothTime
            );

            // アクション中心を注視
            Vector3 direction = centerPosition - _camera.transform.position;
            if (direction.sqrMagnitude > 0.01f)
            {
                Quaternion targetRotation = Quaternion.LookRotation(direction);
                _camera.transform.rotation = Quaternion.Slerp(
                    _camera.transform.rotation,
                    targetRotation,
                    deltaTime / _lookSmoothTime
                );
            }
        }

        #endregion

        #region Private Methods - Utility

        private EntitySnapshot? GetPlayerEntity(ReplayFrame frame)
        {
            var players = frame.GetEntitiesByType(EntityType.Player);
            if (players.Count > 0)
            {
                return players[0];
            }
            return null;
        }

        private Vector3 CalculateActionCenter(ReplayFrame frame)
        {
            Vector3 sum = Vector3.zero;
            int count = 0;

            foreach (var entity in frame.Entities)
            {
                if (entity.IsAlive)
                {
                    sum += entity.Position;
                    count++;
                }
            }

            if (count > 0)
            {
                return sum / count;
            }

            return Vector3.zero;
        }

        private float CalculateActionRadius(ReplayFrame frame, Vector3 center)
        {
            float maxDistanceSq = 0f;

            foreach (var entity in frame.Entities)
            {
                if (entity.IsAlive)
                {
                    float distanceSq = (entity.Position - center).sqrMagnitude;
                    if (distanceSq > maxDistanceSq)
                    {
                        maxDistanceSq = distanceSq;
                    }
                }
            }

            return Mathf.Sqrt(maxDistanceSq);
        }

        #endregion
    }
}
