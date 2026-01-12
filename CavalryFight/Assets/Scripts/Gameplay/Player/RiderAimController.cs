#nullable enable

using UnityEngine;

namespace CavalryFight.Gameplay.Player
{
    /// <summary>
    /// 騎手のエイム（上半身回転）を制御するコンポーネント
    /// </summary>
    /// <remarks>
    /// チャージ中にカメラ方向に向かって上半身を回転させます。
    /// LateUpdateでアニメーション後に適用し、射撃終了時に元に戻します。
    /// 髪オブジェクトは頭ボーンに追従するように設定します。
    /// </remarks>
    public class RiderAimController : MonoBehaviour
    {
        #region Serialized Fields

        [Header("References")]
        [Tooltip("Animator（自動取得）")]
        [SerializeField] private Animator? _animator;

        [Tooltip("馬のTransform（エイム方向計算の基準）")]
        [SerializeField] private Transform? _mountTransform;

        [Header("Aim Settings")]
        [Tooltip("エイムの垂直角度制限（下方向）")]
        [SerializeField] private float _minVerticalAngle = -30f;

        [Tooltip("エイムの垂直角度制限（上方向）")]
        [SerializeField] private float _maxVerticalAngle = 60f;

        [Tooltip("エイムの水平角度制限")]
        [SerializeField] private float _maxHorizontalAngle = 90f;

        [Header("Smoothing")]
        [Tooltip("エイム開始時のスムージング速度")]
        [SerializeField] private float _aimInSpeed = 10f;

        [Tooltip("エイム終了時のスムージング速度")]
        [SerializeField] private float _aimOutSpeed = 5f;

        [Header("Hair Follow")]
        [Tooltip("頭に追従させる髪オブジェクト（複数可）- 自動的に頭ボーンの子になります")]
        [SerializeField] private Transform[]? _hairTransforms;

        [Header("Debug")]
        [SerializeField] private bool _debugLog = true; // デバッグログを有効化

        #endregion

        #region Private Fields

        private Transform? _spine;
        private Transform? _chest;
        private Transform? _head;
        private UnityEngine.Camera? _cachedCamera;
        private bool _isAiming = false;
        private float _currentAimWeight = 0f;

        #endregion

        #region Unity Lifecycle

        private void Awake()
        {
            Debug.Log("[AIM_DEBUG] RiderAimController.Awake() called");

            if (_animator == null)
            {
                _animator = GetComponent<Animator>();
                if (_animator == null)
                {
                    _animator = GetComponentInChildren<Animator>();
                }
            }

            Debug.Log($"[AIM_DEBUG] Animator found: {_animator != null}, isHuman: {_animator?.isHuman}");

            if (_animator != null && _animator.isHuman)
            {
                _spine = _animator.GetBoneTransform(HumanBodyBones.Spine);
                _chest = _animator.GetBoneTransform(HumanBodyBones.Chest);
                _head = _animator.GetBoneTransform(HumanBodyBones.Head);

                Debug.Log($"[AIM_DEBUG] Bones found - Spine: {_spine?.name}, Chest: {_chest?.name}, Head: {_head?.name}");
            }
            else
            {
                Debug.LogWarning("[AIM_DEBUG] Humanoid Animator が見つかりません。");
            }

            // 馬のTransformを自動取得（親の親を探す）
            if (_mountTransform == null)
            {
                // PlayerRider -> MountPoint -> PlayerMount の階層を想定
                Transform? parent = transform.parent;
                while (parent != null)
                {
                    if (parent.GetComponent<MountController>() != null)
                    {
                        _mountTransform = parent;
                        break;
                    }
                    parent = parent.parent;
                }

                Debug.Log($"[AIM_DEBUG] MountTransform: {(_mountTransform != null ? _mountTransform.name : "NULL")}");
            }
        }

        private void Start()
        {
            // Start で髪を頭ボーンの子にする（1フレーム待ってアニメーション適用後に実行）
            StartCoroutine(ParentHairToHeadDelayed());
        }

        private System.Collections.IEnumerator ParentHairToHeadDelayed()
        {
            // 1フレーム待つ（アニメーションが適用された後）
            yield return null;
            ParentHairToHead();
        }

        /// <summary>
        /// 髪オブジェクトを頭ボーンの子に設定します
        /// </summary>
        private void ParentHairToHead()
        {
            if (_head == null || _hairTransforms == null || _hairTransforms.Length == 0)
            {
                return;
            }

            foreach (var hairTransform in _hairTransforms)
            {
                if (hairTransform == null)
                {
                    continue;
                }

                // 既に頭の子なら何もしない
                if (hairTransform.parent == _head)
                {
                    continue;
                }

                // ワールド座標を保持したまま親を変更
                hairTransform.SetParent(_head, worldPositionStays: true);
                Debug.Log($"[AIM_DEBUG] Hair '{hairTransform.name}' parented to head bone '{_head.name}'");
            }
        }

        private void Update()
        {
            // ウェイトをスムーズに変化させる
            float targetWeight = _isAiming ? 1f : 0f;
            float speed = _isAiming ? _aimInSpeed : _aimOutSpeed;
            _currentAimWeight = Mathf.Lerp(_currentAimWeight, targetWeight, speed * Time.deltaTime);
        }

        private void LateUpdate()
        {
            if (_animator == null || _spine == null)
            {
                return;
            }

            // ウェイトが十分小さい場合はスキップ
            if (_currentAimWeight < 0.01f)
            {
                return;
            }

            // カメラからエイム方向を取得
            UnityEngine.Camera? mainCam = GetActiveCamera();
            if (mainCam == null)
            {
                Debug.LogWarning("[AIM_DEBUG] No camera found!");
                return;
            }

            // シンプルなアプローチ: カメラの向きに上半身を回転させる
            Vector3 camForward = mainCam.transform.forward;
            Vector3 camForwardFlat = new Vector3(camForward.x, 0f, camForward.z).normalized;

            // 馬の向き
            Transform refTransform = _mountTransform != null ? _mountTransform : transform;
            Vector3 mountForward = refTransform.forward;
            Vector3 mountForwardFlat = new Vector3(mountForward.x, 0f, mountForward.z).normalized;

            // カメラと馬の水平角度差を計算
            float horizontalAngle = Vector3.SignedAngle(mountForwardFlat, camForwardFlat, Vector3.up);

            // 垂直角度
            float verticalAngle = Mathf.Asin(Mathf.Clamp(camForward.y, -1f, 1f)) * Mathf.Rad2Deg;

            // 角度を制限
            horizontalAngle = Mathf.Clamp(horizontalAngle, -_maxHorizontalAngle, _maxHorizontalAngle);
            verticalAngle = Mathf.Clamp(verticalAngle, _minVerticalAngle, _maxVerticalAngle);

            // ウェイトを適用
            horizontalAngle *= _currentAimWeight;
            verticalAngle *= _currentAimWeight;

            if (_debugLog)
            {
                Debug.Log($"[AIM_DEBUG] CamForward: {camForward}, MountForward: {mountForward}, H_Angle: {horizontalAngle:F1}°, V_Angle: {verticalAngle:F1}°, Weight: {_currentAimWeight:F2}");
            }

            // シンプルに回転を適用（アニメーション後に追加回転）
            // Rotate()を使用してアニメーションの上に追加
            float spineH = horizontalAngle * 0.4f;
            float spineV = verticalAngle * 0.4f;
            float chestH = horizontalAngle * 0.6f;
            float chestV = verticalAngle * 0.6f;

            // Spineに回転を適用
            _spine.Rotate(Vector3.up, spineH, Space.World);
            _spine.Rotate(refTransform.right, spineV, Space.World);

            // Chestに回転を適用
            if (_chest != null)
            {
                _chest.Rotate(Vector3.up, chestH, Space.World);
                _chest.Rotate(refTransform.right, chestV, Space.World);
            }
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// エイム状態を設定します
        /// </summary>
        /// <param name="isAiming">エイム中かどうか</param>
        public void SetAiming(bool isAiming)
        {
            if (_isAiming != isAiming)
            {
                _isAiming = isAiming;
                Debug.Log($"[AIM_DEBUG] SetAiming: {isAiming}");
            }
        }

        #endregion

        #region Private Methods

        /// <summary>
        /// アクティブなカメラを取得します
        /// </summary>
        /// <returns>カメラ</returns>
        private UnityEngine.Camera? GetActiveCamera()
        {
            // キャッシュされたカメラが有効ならそれを返す
            if (_cachedCamera != null && _cachedCamera.isActiveAndEnabled)
            {
                return _cachedCamera;
            }

            // Camera.mainを試す（MainCameraタグが設定されている場合）
            _cachedCamera = UnityEngine.Camera.main;
            if (_cachedCamera != null)
            {
                return _cachedCamera;
            }

            // Camera.mainがnullの場合、現在レンダリング中のカメラを探す
            _cachedCamera = UnityEngine.Camera.current;
            if (_cachedCamera != null)
            {
                return _cachedCamera;
            }

            // 最後の手段：シーン内のすべてのカメラから有効なものを探す
            var allCameras = UnityEngine.Camera.allCameras;
            foreach (var cam in allCameras)
            {
                if (cam.isActiveAndEnabled && cam.gameObject.activeInHierarchy)
                {
                    _cachedCamera = cam;
                    return _cachedCamera;
                }
            }

            return null;
        }

        #endregion
    }
}
