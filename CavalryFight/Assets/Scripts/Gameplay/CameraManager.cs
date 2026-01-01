#nullable enable

using System;
using UnityEngine;
using MalbersAnimations;
using MalbersAnimations.Scriptables;

namespace CavalryFight.Gameplay
{
    /// <summary>
    /// カメラマネージャー
    /// </summary>
    /// <remarks>
    /// Malbers ThirdPersonFollowTargetと連携してカメラターゲットを管理します。
    /// PlayerSpawnerからスポーン完了時にターゲットを設定します。
    /// </remarks>
    public class CameraManager : MonoBehaviour
    {
        #region Serialized Fields

        [Header("Camera References")]
        [Tooltip("Malbers ThirdPersonFollowTarget コンポーネントへの参照")]
        [SerializeField] private ThirdPersonFollowTarget? _thirdPersonCamera;

        [Tooltip("TransformVar（Malbersのカメラターゲット変数）")]
        [SerializeField] private TransformVar? _cameraTargetVar;

        [Header("Target Settings")]
        [Tooltip("デフォルトでマウント（馬）をターゲットにするか")]
        [SerializeField] private bool _targetMountByDefault = true;

        [Tooltip("ターゲットのオフセット（ローカル座標）")]
        [SerializeField] private Vector3 _targetOffset = new Vector3(0, 1.5f, 0);

        [Header("Debug")]
        [Tooltip("デバッグログを出力するか")]
        [SerializeField] private bool _debugLog = true;

        #endregion

        #region Private Fields

        private Transform? _currentTarget;
        private GameObject? _targetOffsetObject;

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

            // ThirdPersonFollowTargetを自動検索
            if (_thirdPersonCamera == null)
            {
                _thirdPersonCamera = FindFirstObjectByType<ThirdPersonFollowTarget>();
            }
        }

        private void Start()
        {
            // PlayerSpawnerのイベントを購読
            if (Player.PlayerSpawner.Instance != null)
            {
                SubscribeToPlayerSpawner(Player.PlayerSpawner.Instance);
            }
        }

        private void OnDestroy()
        {
            if (Instance == this)
            {
                Instance = null;
            }

            // オフセットオブジェクトをクリーンアップ
            if (_targetOffsetObject != null)
            {
                Destroy(_targetOffsetObject);
            }
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// PlayerSpawnerのイベントを購読します
        /// </summary>
        /// <param name="spawner">PlayerSpawner</param>
        public void SubscribeToPlayerSpawner(Player.PlayerSpawner spawner)
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
        public void SetTargetFromSpawner(Player.PlayerSpawner spawner)
        {
            if (spawner == null)
            {
                return;
            }

            if (_targetMountByDefault && spawner.SpawnedMount != null)
            {
                SetTarget(spawner.SpawnedMount.transform);
            }
            else if (spawner.SpawnedRider != null)
            {
                SetTarget(spawner.SpawnedRider.transform);
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
            Transform actualTarget = CreateOrUpdateOffsetTarget(target);

            // ThirdPersonFollowTargetに設定
            if (_thirdPersonCamera != null)
            {
                _thirdPersonCamera.SetTarget(actualTarget);

                if (_debugLog)
                {
                    Debug.Log($"[CameraManager] カメラターゲットを設定: {target.name}");
                }
            }

            // TransformVarに設定（他のMalbersコンポーネント用）
            if (_cameraTargetVar != null)
            {
                _cameraTargetVar.Value = actualTarget;

                if (_debugLog)
                {
                    Debug.Log($"[CameraManager] TransformVar を更新: {actualTarget.name}");
                }
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

        /// <summary>
        /// カメラをテレポートします（ターゲットの後ろに即座に移動）
        /// </summary>
        public void TeleportCamera()
        {
            if (_thirdPersonCamera != null)
            {
                _thirdPersonCamera.TargetTeleport(true);

                if (_debugLog)
                {
                    Debug.Log("[CameraManager] カメラをテレポートしました。");
                }
            }
        }

        #endregion

        #region Private Methods

        /// <summary>
        /// オフセット付きのターゲットオブジェクトを作成または更新します
        /// </summary>
        /// <param name="parent">親Transform</param>
        /// <returns>オフセット付きターゲットのTransform</returns>
        private Transform CreateOrUpdateOffsetTarget(Transform parent)
        {
            // オフセットがゼロなら親をそのまま返す
            if (_targetOffset == Vector3.zero)
            {
                return parent;
            }

            // オフセットオブジェクトがなければ作成
            if (_targetOffsetObject == null)
            {
                _targetOffsetObject = new GameObject("CameraTarget_Offset");
            }

            // 親に設定してオフセットを適用
            _targetOffsetObject.transform.SetParent(parent);
            _targetOffsetObject.transform.localPosition = _targetOffset;
            _targetOffsetObject.transform.localRotation = Quaternion.identity;

            return _targetOffsetObject.transform;
        }

        #endregion
    }
}
