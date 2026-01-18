#nullable enable

using UnityEngine;

namespace CavalryFight.Services.Replay
{
    /// <summary>
    /// リプレイ用カメラモード
    /// </summary>
    public enum ReplayCameraMode
    {
        /// <summary>プレイヤー追従</summary>
        Follow,
        /// <summary>フリーカメラ</summary>
        Free,
        /// <summary>シネマティック</summary>
        Cinematic
    }

    /// <summary>
    /// リプレイ用カメラマネージャー
    /// </summary>
    /// <remarks>
    /// リプレイシーンでのカメラ制御を管理します。
    /// フリーカメラ、追従カメラ、シネマティックカメラを切り替えます。
    /// Replayシーンに配置してください。
    /// </remarks>
    public class ReplayCameraManager : MonoBehaviour
    {
        #region Serialized Fields

        [Header("Camera")]
        [Tooltip("リプレイ用カメラ（未設定の場合はメインカメラを使用）")]
        [SerializeField] private Camera? _camera;

        [Header("Follow Camera Settings")]
        [Tooltip("追従距離")]
        [SerializeField] private float _followDistance = 8f;

        [Tooltip("追従高さ")]
        [SerializeField] private float _followHeight = 3f;

        [Tooltip("追従のスムージング時間")]
        [SerializeField] private float _followSmoothTime = 0.3f;

        [Header("Free Camera Settings")]
        [Tooltip("移動速度")]
        [SerializeField] private float _freeMoveSpeed = 5f;

        [Tooltip("高速移動速度")]
        [SerializeField] private float _freeFastMoveSpeed = 15f;

        [Tooltip("低速移動速度")]
        [SerializeField] private float _freeSlowMoveSpeed = 1f;

        [Tooltip("視点感度")]
        [SerializeField] private float _freeLookSensitivity = 100f;

        [Header("Camera Boundary")]
        [Tooltip("境界の最小値")]
        [SerializeField] private Vector3 _boundaryMin = new Vector3(-100f, 1f, -100f);

        [Tooltip("境界の最大値")]
        [SerializeField] private Vector3 _boundaryMax = new Vector3(100f, 50f, 100f);

        [Header("Cinematic Camera Settings")]
        [Tooltip("周回半径")]
        [SerializeField] private float _cinematicOrbitRadius = 10f;

        [Tooltip("周回速度（度/秒）")]
        [SerializeField] private float _cinematicOrbitSpeed = 20f;

        [Header("Debug")]
        [SerializeField] private bool _debugLog = true;

        #endregion

        #region Private Fields

        private IReplayCameraController? _currentController;
        private FreeCameraController? _freeController;
        private CinematicCameraController? _cinematicController;
        private FollowCameraController? _followController;
        private ReplayCameraMode _currentMode = ReplayCameraMode.Follow;
        private ReplayFrame? _currentFrame;

        #endregion

        #region Properties

        /// <summary>
        /// シングルトンインスタンス
        /// </summary>
        public static ReplayCameraManager? Instance { get; private set; }

        /// <summary>
        /// 現在のカメラモード
        /// </summary>
        public ReplayCameraMode CurrentMode => _currentMode;

        #endregion

        #region Unity Lifecycle

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Debug.LogWarning("[ReplayCameraManager] Duplicate instance detected. Destroying this one.");
                Destroy(gameObject);
                return;
            }

            Instance = this;

            // カメラを取得
            if (_camera == null)
            {
                _camera = Camera.main;
            }

            if (_camera == null)
            {
                Debug.LogError("[ReplayCameraManager] カメラが見つかりません。");
                return;
            }

            // カメラコントローラーを初期化
            InitializeControllers();

            if (_debugLog)
            {
                Debug.Log("[ReplayCameraManager] Awake - 初期化しました。");
            }
        }

        private void Update()
        {
            if (_currentController == null || _camera == null)
            {
                return;
            }

            // 現在のフレームを取得（ReplayPlaybackManagerから）
            if (ReplayPlaybackManager.Instance != null && ReplayPlaybackManager.Instance.IsInitialized)
            {
                var replayData = ReplayPlaybackManager.Instance.CurrentReplay;
                // フレームはReplayViewから更新される
            }

            // カメラを更新
            _currentController.UpdateCamera(Time.unscaledDeltaTime, _currentFrame);
        }

        private void OnDestroy()
        {
            if (Instance == this)
            {
                Instance = null;
            }

            // コントローラーを破棄
            _freeController?.Dispose();
            _cinematicController?.Dispose();
            _followController?.Dispose();
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// カメラモードを設定します
        /// </summary>
        /// <param name="mode">カメラモード</param>
        public void SetCameraMode(ReplayCameraMode mode)
        {
            if (_currentMode == mode)
            {
                return;
            }

            _currentMode = mode;

            // 現在のコントローラーを無効化
            if (_currentController != null)
            {
                _currentController.Enabled = false;
            }

            // 新しいコントローラーを選択
            _currentController = mode switch
            {
                ReplayCameraMode.Follow => _followController,
                ReplayCameraMode.Free => _freeController,
                ReplayCameraMode.Cinematic => _cinematicController,
                _ => _followController
            };

            // 新しいコントローラーを有効化
            if (_currentController != null)
            {
                _currentController.Enabled = true;
            }

            if (_debugLog)
            {
                Debug.Log($"[ReplayCameraManager] カメラモードを変更しました: {mode}");
            }
        }

        /// <summary>
        /// 現在のフレームを設定します
        /// </summary>
        /// <param name="frame">リプレイフレーム</param>
        public void SetCurrentFrame(ReplayFrame? frame)
        {
            _currentFrame = frame;
        }

        /// <summary>
        /// カメラをリセットします
        /// </summary>
        public void ResetCamera()
        {
            _currentController?.Reset();

            if (_debugLog)
            {
                Debug.Log("[ReplayCameraManager] カメラをリセットしました。");
            }
        }

        #endregion

        #region Private Methods

        private void InitializeControllers()
        {
            if (_camera == null)
            {
                return;
            }

            // フリーカメラコントローラー
            _freeController = new FreeCameraController
            {
                MoveSpeed = _freeMoveSpeed,
                LookSensitivity = _freeLookSensitivity,
                BoundaryMin = _boundaryMin,
                BoundaryMax = _boundaryMax
            };
            _freeController.Initialize(_camera);
            _freeController.Enabled = false;

            // シネマティックカメラコントローラー
            _cinematicController = new CinematicCameraController
            {
                OrbitRadius = _cinematicOrbitRadius,
                OrbitSpeed = _cinematicOrbitSpeed
            };
            _cinematicController.Initialize(_camera);
            _cinematicController.Enabled = false;

            // 追従カメラコントローラー
            _followController = new FollowCameraController
            {
                FollowDistance = _followDistance,
                FollowHeight = _followHeight,
                SmoothTime = _followSmoothTime
            };
            _followController.Initialize(_camera);
            _followController.Enabled = true;

            // デフォルトは追従カメラ
            _currentController = _followController;

            if (_debugLog)
            {
                Debug.Log("[ReplayCameraManager] カメラコントローラーを初期化しました。");
            }
        }

        #endregion
    }

    /// <summary>
    /// 追従カメラコントローラー
    /// </summary>
    /// <remarks>
    /// プレイヤーを追従するカメラコントローラー。
    /// CinematicCameraControllerのFollowModeをシンプルにしたバージョン。
    /// </remarks>
    public class FollowCameraController : IReplayCameraController
    {
        #region Fields

        private Camera? _camera;
        private bool _enabled = true;
        private Vector3 _initialPosition;
        private Quaternion _initialRotation;
        private Vector3 _currentVelocity = Vector3.zero;

        #endregion

        #region Properties

        /// <summary>
        /// カメラが有効かどうかを取得または設定します
        /// </summary>
        public bool Enabled
        {
            get => _enabled;
            set => _enabled = value;
        }

        /// <summary>
        /// 追従距離
        /// </summary>
        public float FollowDistance { get; set; } = 8f;

        /// <summary>
        /// 追従高さ
        /// </summary>
        public float FollowHeight { get; set; } = 3f;

        /// <summary>
        /// スムージング時間
        /// </summary>
        public float SmoothTime { get; set; } = 0.3f;

        #endregion

        #region IReplayCameraController Implementation

        /// <summary>
        /// カメラを初期化します
        /// </summary>
        public void Initialize(Camera camera)
        {
            _camera = camera;
            _initialPosition = camera.transform.position;
            _initialRotation = camera.transform.rotation;

            Debug.Log("[FollowCameraController] Initialized.");
        }

        /// <summary>
        /// カメラの更新処理
        /// </summary>
        public void UpdateCamera(float deltaTime, ReplayFrame? currentFrame)
        {
            if (!_enabled || _camera == null || currentFrame == null)
            {
                return;
            }

            // プレイヤーを探す
            var players = currentFrame.GetEntitiesByType(EntityType.Player);
            if (players.Count == 0)
            {
                return;
            }

            var player = players[0];
            Vector3 playerPosition = player.Position;
            Vector3 playerForward = player.Rotation * Vector3.forward;

            // プレイヤーの後ろに配置
            Vector3 targetPosition = playerPosition - playerForward * FollowDistance + Vector3.up * FollowHeight;

            // スムーズに移動
            _camera.transform.position = Vector3.SmoothDamp(
                _camera.transform.position,
                targetPosition,
                ref _currentVelocity,
                SmoothTime
            );

            // プレイヤーを注視
            Vector3 lookTarget = playerPosition + Vector3.up * 1.5f;
            Vector3 direction = lookTarget - _camera.transform.position;
            if (direction.sqrMagnitude > 0.01f)
            {
                Quaternion targetRotation = Quaternion.LookRotation(direction);
                _camera.transform.rotation = Quaternion.Slerp(
                    _camera.transform.rotation,
                    targetRotation,
                    deltaTime / 0.2f
                );
            }
        }

        /// <summary>
        /// カメラをリセットします
        /// </summary>
        public void Reset()
        {
            if (_camera == null)
            {
                return;
            }

            _camera.transform.position = _initialPosition;
            _camera.transform.rotation = _initialRotation;
            _currentVelocity = Vector3.zero;

            Debug.Log("[FollowCameraController] Reset to initial position.");
        }

        /// <summary>
        /// カメラを破棄します
        /// </summary>
        public void Dispose()
        {
            _camera = null;
            Debug.Log("[FollowCameraController] Disposed.");
        }

        #endregion
    }
}
