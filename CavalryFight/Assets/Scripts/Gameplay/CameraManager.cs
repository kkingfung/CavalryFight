#nullable enable

using System;
using UnityEngine;
using CavalryFight.Gameplay.Camera;
using CavalryFight.Gameplay.Player;

namespace CavalryFight.Gameplay
{
    /// <summary>
    /// カメラマネージャー
    /// </summary>
    /// <remarks>
    /// PlayerCameraControllerと連携してカメラターゲットを管理します。
    /// PlayerSpawnerからスポーン完了時にターゲットを設定します。
    /// </remarks>
    public class CameraManager : MonoBehaviour
    {
        #region Serialized Fields

        [Header("Camera Controller")]
        [Tooltip("PlayerCameraController への参照")]
        [SerializeField] private PlayerCameraController? _cameraController;

        [Header("Target Settings")]
        [Tooltip("デフォルトでマウント（馬）をターゲットにするか")]
        [SerializeField] private bool _targetMountByDefault = true;

        [Tooltip("ターゲットのオフセット（ローカル座標）")]
        [SerializeField] private Vector3 _targetOffset = new Vector3(0, 1.5f, 0);

        [Tooltip("LookAtターゲットのオフセット（ローカル座標）")]
        [SerializeField] private Vector3 _lookAtOffset = new Vector3(0, 1.8f, 0);

        [Header("Debug")]
        [Tooltip("デバッグログを出力するか")]
        [SerializeField] private bool _debugLog = true;

        #endregion

        #region Private Fields

        private Transform? _currentTarget;
        private GameObject? _followOffsetObject;
        private GameObject? _lookAtOffsetObject;

        #endregion

        #region Properties

        /// <summary>
        /// シングルトンインスタンス
        /// </summary>
        public static CameraManager? Instance { get; private set; }

        /// <summary>
        /// 現在のカメラターゲット
        /// </summary>
        public Transform? CurrentTarget => _currentTarget;

        /// <summary>
        /// PlayerCameraControllerを取得します
        /// </summary>
        public PlayerCameraController? CameraController => _cameraController;

        #endregion

        #region Unity Lifecycle

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Debug.LogWarning("[CameraManager] Duplicate instance detected. Destroying this one.");
                Destroy(gameObject);
                return;
            }

            Instance = this;

            // PlayerCameraControllerを自動検索
            if (_cameraController == null)
            {
                _cameraController = FindFirstObjectByType<PlayerCameraController>();

                if (_cameraController == null && _debugLog)
                {
                    Debug.LogWarning("[CameraManager] PlayerCameraController が見つかりません。");
                }
            }
        }

        private void Start()
        {
            // PlayerSpawnerのイベントを購読
            if (PlayerSpawner.Instance != null)
            {
                SubscribeToPlayerSpawner(PlayerSpawner.Instance);
            }
        }

        private void OnDestroy()
        {
            if (Instance == this)
            {
                Instance = null;
            }

            // オフセットオブジェクトをクリーンアップ
            if (_followOffsetObject != null)
            {
                Destroy(_followOffsetObject);
            }

            if (_lookAtOffsetObject != null)
            {
                Destroy(_lookAtOffsetObject);
            }
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// PlayerSpawnerのイベントを購読します
        /// </summary>
        /// <param name="spawner">PlayerSpawner</param>
        public void SubscribeToPlayerSpawner(PlayerSpawner spawner)
        {
            if (spawner == null)
            {
                return;
            }

            // スポーン完了時にターゲットを設定
            if (spawner.IsSpawned)
            {
                SetTargetFromSpawner(spawner);
            }
        }

        /// <summary>
        /// PlayerSpawnerからターゲットを設定します
        /// </summary>
        /// <param name="spawner">PlayerSpawner</param>
        public void SetTargetFromSpawner(PlayerSpawner spawner)
        {
            if (spawner == null)
            {
                return;
            }

            // 常にライダーをターゲットにする（カメラはライダーに追従）
            Transform? targetTransform = spawner.SpawnedRider?.transform;

            // ライダーがいない場合はマウントをフォールバック
            if (targetTransform == null && spawner.SpawnedMount != null)
            {
                targetTransform = spawner.SpawnedMount.transform;
            }

            if (targetTransform != null)
            {
                SetTarget(targetTransform);

                // PlayerControllerを設定
                if (spawner.SpawnedRider != null)
                {
                    var playerController = spawner.SpawnedRider.GetComponent<PlayerController>();
                    if (playerController != null && _cameraController != null)
                    {
                        _cameraController.SetPlayerController(playerController);

                        if (_debugLog)
                        {
                            Debug.Log("[CameraManager] PlayerController をカメラに設定しました。");
                        }
                    }
                }
            }
        }

        /// <summary>
        /// カメラターゲットを設定します
        /// </summary>
        /// <param name="target">ターゲットのTransform</param>
        public void SetTarget(Transform? target)
        {
            if (target == null)
            {
                if (_debugLog)
                {
                    Debug.LogWarning("[CameraManager] ターゲットが null です。");
                }
                return;
            }

            _currentTarget = target;

            // オフセット付きのターゲットを作成
            Transform followTarget = CreateOrUpdateOffsetTarget(target, ref _followOffsetObject, "CameraFollow_Offset", _targetOffset);
            Transform lookAtTarget = CreateOrUpdateOffsetTarget(target, ref _lookAtOffsetObject, "CameraLookAt_Offset", _lookAtOffset);

            // PlayerCameraControllerに設定
            if (_cameraController != null)
            {
                _cameraController.SetTargets(followTarget, lookAtTarget);

                if (_debugLog)
                {
                    Debug.Log($"[CameraManager] カメラターゲットを設定: {target.name}");
                }
            }
            else
            {
                Debug.LogWarning("[CameraManager] PlayerCameraController が設定されていません。");
            }
        }

        /// <summary>
        /// ターゲットをマウント（馬）に切り替えます
        /// </summary>
        /// <param name="mount">マウントのTransform</param>
        public void SetTargetToMount(Transform mount)
        {
            SetTarget(mount);
        }

        /// <summary>
        /// ターゲットをライダー（騎手）に切り替えます
        /// </summary>
        /// <param name="rider">ライダーのTransform</param>
        public void SetTargetToRider(Transform rider)
        {
            SetTarget(rider);
        }

        #endregion

        #region Private Methods

        /// <summary>
        /// オフセット付きのターゲットオブジェクトを作成または更新します
        /// </summary>
        /// <param name="parent">親Transform</param>
        /// <param name="offsetObject">オフセットオブジェクトの参照</param>
        /// <param name="name">オブジェクト名</param>
        /// <param name="offset">オフセット値</param>
        /// <returns>オフセット付きターゲットのTransform</returns>
        private Transform CreateOrUpdateOffsetTarget(Transform parent, ref GameObject? offsetObject, string name, Vector3 offset)
        {
            // オフセットがゼロなら親をそのまま返す
            if (offset == Vector3.zero)
            {
                return parent;
            }

            // オフセットオブジェクトがなければ作成
            if (offsetObject == null)
            {
                offsetObject = new GameObject(name);
            }

            // 親に設定してオフセットを適用
            offsetObject.transform.SetParent(parent);
            offsetObject.transform.localPosition = offset;
            offsetObject.transform.localRotation = Quaternion.identity;

            return offsetObject.transform;
        }

        #endregion
    }
}
