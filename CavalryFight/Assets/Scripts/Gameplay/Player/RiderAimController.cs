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

        [Tooltip("エイムの水平角度制限（左方向、負の値）")]
        [SerializeField] private float _minHorizontalAngle = -35f;

        [Tooltip("エイムの水平角度制限（右方向、正の値）")]
        [SerializeField] private float _maxHorizontalAngle = 125f;

        [Header("Smoothing")]
        [Tooltip("エイム開始時のスムージング速度")]
        [SerializeField] private float _aimInSpeed = 10f;

        [Tooltip("エイム終了時のスムージング速度")]
        [SerializeField] private float _aimOutSpeed = 5f;

        [Header("Hair Follow")]
        [Tooltip("頭に追従させる髪オブジェクト（複数可）- 自動的に頭ボーンの子になります")]
        [SerializeField] private Transform[]? _hairTransforms;

        [Header("Debug")]
        [SerializeField] private bool _debugLog = true; // デバッグログを有効化（Runtime時も常にtrue）

        #endregion

        #region Private Fields

        private Transform? _spine;
        private Transform? _chest;
        private Transform? _head;
        private UnityEngine.Camera? _cachedCamera;
        private bool _isAiming = false;
        private float _currentAimWeight = 0f;
        private RiderController? _riderController;
        private int _aimLayerIndex = -1;
        private bool _useAnimatorAimLayer = false;

        #endregion

        #region Unity Lifecycle

        private void Awake()
        {
            // デバッグのため常に有効化
            _debugLog = true;

            Debug.Log($"[AIM_DEBUG] RiderAimController.Awake() called, instanceId={GetInstanceID()}, gameObject={gameObject.name}");

            // RiderControllerを取得（先に取得してAnimatorを参照する）
            _riderController = GetComponent<RiderController>();
            if (_riderController == null)
            {
                _riderController = GetComponentInParent<RiderController>();
            }

            // Animatorは RiderController から取得するか、シリアライズされたものを使用
            if (_animator == null && _riderController != null)
            {
                _animator = _riderController.Animator;
            }

            // それでもなければ自分で探す
            if (_animator == null)
            {
                _animator = GetComponent<Animator>();
                if (_animator == null)
                {
                    _animator = GetComponentInChildren<Animator>();
                }
            }

            Debug.Log($"[AIM_DEBUG] Animator found: {_animator != null}, isHuman: {_animator?.isHuman}, runtimeController: {_animator?.runtimeAnimatorController?.name ?? "null"}");

            if (_animator != null && _animator.isHuman)
            {
                _spine = _animator.GetBoneTransform(HumanBodyBones.Spine);
                _chest = _animator.GetBoneTransform(HumanBodyBones.Chest);
                _head = _animator.GetBoneTransform(HumanBodyBones.Head);

                Debug.Log($"[AIM_DEBUG] Bones found - Spine: {_spine?.name}, Chest: {_chest?.name}, Head: {_head?.name}");

                // Aimレイヤーが存在するか確認
                _aimLayerIndex = _animator.GetLayerIndex("Aim");
                _useAnimatorAimLayer = _aimLayerIndex >= 0;
                Debug.Log($"[AIM_DEBUG] Aim layer index: {_aimLayerIndex}, useAnimatorAimLayer: {_useAnimatorAimLayer}");

                // Aimレイヤーが見つからない場合の警告
                if (!_useAnimatorAimLayer)
                {
                    Debug.LogWarning($"[AIM_DEBUG] Aim layer not found in AnimatorController '{_animator.runtimeAnimatorController?.name}'. Will use bone rotation fallback.");
                }
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

            // MountTransformがAwakeで見つからなかった場合、再度探す
            if (_mountTransform == null)
            {
                TryFindMountTransform();
            }
        }

        /// <summary>
        /// MountTransformを探して設定します
        /// </summary>
        private void TryFindMountTransform()
        {
            Transform? parent = transform.parent;
            while (parent != null)
            {
                if (parent.GetComponent<MountController>() != null)
                {
                    _mountTransform = parent;
                    Debug.Log($"[AIM_DEBUG] MountTransform found in Start: {_mountTransform.name}");
                    return;
                }
                parent = parent.parent;
            }

            Debug.LogWarning("[AIM_DEBUG] MountTransform still not found in Start!");
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
            float prevWeight = _currentAimWeight;
            _currentAimWeight = Mathf.Lerp(_currentAimWeight, targetWeight, speed * Time.deltaTime);

            // デバッグ: ウェイト変化を追跡（アイム中のみ）
            if (_isAiming)
            {
                Debug.Log($"[AIM_DEBUG] Update: isAiming={_isAiming}, weight={prevWeight:F3}->{_currentAimWeight:F3}, enabled={enabled}, gameObject.active={gameObject.activeInHierarchy}");
            }
        }

        private void LateUpdate()
        {
            // 常にLateUpdateが呼ばれているか確認
            if (_isAiming && _debugLog)
            {
                Debug.Log($"[AIM_DEBUG] LateUpdate START: weight={_currentAimWeight:F3}, useAnimatorLayer={_useAnimatorAimLayer}");
            }

            // AnimatorのAimレイヤーを使用する場合（腕のアニメーション用）
            if (_useAnimatorAimLayer && _animator != null && _riderController != null)
            {
                // Aimレイヤーのウェイトを設定（腕のポーズ用）
                _riderController.SetAimLayerWeight(_currentAimWeight);
            }

            // ウェイトが十分小さい場合はスキップ
            if (_currentAimWeight < 0.01f)
            {
                return;
            }

            // ボーン回転は常に適用（Aimレイヤーの有無に関わらず）
            // Aimレイヤーは腕のポーズ、ボーン回転は体の向きを担当

            // animator/spineチェック
            if (_animator == null || _spine == null)
            {
                if (_debugLog)
                {
                    Debug.LogWarning($"[AIM_DEBUG] LateUpdate early return: animator={_animator != null}, spine={_spine != null}");
                }
                return;
            }

            // カメラからエイム方向を取得
            UnityEngine.Camera? mainCam = GetActiveCamera();
            if (mainCam == null)
            {
                if (_debugLog)
                {
                    Debug.LogWarning("[AIM_DEBUG] No camera found!");
                }
                return;
            }

            // シンプルなアプローチ: カメラの向きに上半身を回転させる
            Vector3 camFwd = mainCam.transform.forward;
            Vector3 camFwdFlat = new Vector3(camFwd.x, 0f, camFwd.z).normalized;

            // 馬の向き
            Transform refXform = _mountTransform != null ? _mountTransform : transform;
            Vector3 mountFwd = refXform.forward;
            Vector3 mountFwdFlat = new Vector3(mountFwd.x, 0f, mountFwd.z).normalized;

            if (_debugLog)
            {
                Debug.Log($"[AIM_DEBUG] RefTransform: {refXform.name}, MountTransform: {(_mountTransform != null ? _mountTransform.name : "NULL")}, CamFwd: {camFwd}, MountFwd: {mountFwd}");
            }

            // カメラと馬の水平角度差を計算
            float hAngle = Vector3.SignedAngle(mountFwdFlat, camFwdFlat, Vector3.up);

            // 垂直角度
            float vAngle = Mathf.Asin(Mathf.Clamp(camFwd.y, -1f, 1f)) * Mathf.Rad2Deg;

            // 角度を制限（オフセット適用前に制限）- 非対称制限
            hAngle = Mathf.Clamp(hAngle, _minHorizontalAngle, _maxHorizontalAngle);
            vAngle = Mathf.Clamp(vAngle, _minVerticalAngle, _maxVerticalAngle);

            // ボーンのローカル軸がずれているため補正（制限後に適用）
            hAngle += 135f;

            // ウェイトを適用
            hAngle *= _currentAimWeight;
            vAngle *= _currentAimWeight;

            if (_debugLog)
            {
                Debug.Log($"[AIM_DEBUG] Bone rotation: H_Angle: {hAngle:F1}°, V_Angle: {vAngle:F1}°, Weight: {_currentAimWeight:F2}");
            }

            // 回転を適用（アニメーション後に追加回転）
            // ボーンのローカルY軸で水平回転、ローカルX軸で垂直回転
            float spineH = hAngle * 0.4f;
            float spineV = vAngle * 0.4f;
            float chestH = hAngle * 0.6f;
            float chestV = vAngle * 0.6f;

            // Spineに回転を適用（ローカル空間）
            // ローカルY軸で水平回転、ローカルX軸で垂直回転
            _spine.Rotate(0f, spineH, 0f, Space.Self);
            _spine.Rotate(spineV, 0f, 0f, Space.Self);

            // Chestに回転を適用（ローカル空間）
            if (_chest != null)
            {
                _chest.Rotate(0f, chestH, 0f, Space.Self);
                _chest.Rotate(chestV, 0f, 0f, Space.Self);
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
                Debug.Log($"[AIM_DEBUG] SetAiming: {isAiming}, instanceId={GetInstanceID()}, enabled={enabled}, gameObject={gameObject.name}, active={gameObject.activeInHierarchy}");

                // SetAiming(true)が呼ばれた時にMountTransformがまだNULLなら再度探す
                if (isAiming && _mountTransform == null)
                {
                    TryFindMountTransform();
                }
            }
        }

        /// <summary>
        /// MountTransformを外部から設定します
        /// </summary>
        /// <param name="mountTransform">馬のTransform</param>
        public void SetMountTransform(Transform mountTransform)
        {
            _mountTransform = mountTransform;
            Debug.Log($"[AIM_DEBUG] MountTransform set externally: {mountTransform?.name ?? "NULL"}");
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
