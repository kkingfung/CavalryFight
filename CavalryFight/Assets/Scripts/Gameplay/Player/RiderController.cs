#nullable enable

using UnityEngine;

namespace CavalryFight.Gameplay.Player
{
    /// <summary>
    /// 騎手（プレイヤーキャラクター）を制御するコンポーネント
    /// </summary>
    /// <remarks>
    /// P09モデルのラッパーとして機能し、アニメーション制御と
    /// カスタマイズ適用のインターフェースを提供します。
    /// MountControllerと連携して騎乗状態を管理します。
    /// </remarks>
    public class RiderController : MonoBehaviour
    {
        #region Serialized Fields

        [Header("P09 Model")]
        [Tooltip("P09キャラクターモデル（子オブジェクト）")]
        [SerializeField] private GameObject? _p09Model;

        [Tooltip("P09モデルのAnimator")]
        [SerializeField] private Animator? _animator;

        [Header("Attachment Points")]
        [Tooltip("武器を取り付けるポイント（右手）")]
        [SerializeField] private Transform? _rightHandAttachment;

        [Tooltip("矢筒を取り付けるポイント（背中）")]
        [SerializeField] private Transform? _quiverAttachment;

        [Header("Archer Controller")]
        [Tooltip("アーチャーコントローラー（弓のアニメーション制御）")]
        [SerializeField] private RiderArcherController? _archerController;

        [Header("Animation Gender Swapper")]
        [Tooltip("性別に応じたアニメーション切り替え（オプション）")]
        [SerializeField] private RiderAnimationGenderSwapper? _genderSwapper;

        [Header("Debug")]
        [SerializeField] private bool _debugLog = false;

        #endregion

        #region Private Fields

        private bool _isMounted = false;
        private Transform? _mountPoint;
        private bool _originalApplyRootMotion = false;

        #endregion

        #region Archer Controller Property

        /// <summary>
        /// アーチャーコントローラーを取得します
        /// </summary>
        public RiderArcherController? ArcherController => _archerController;

        /// <summary>
        /// アニメーション性別スワッパーを取得します
        /// </summary>
        public RiderAnimationGenderSwapper? GenderSwapper => _genderSwapper;

        #endregion

        #region Properties

        /// <summary>
        /// P09モデルのGameObjectを取得します
        /// </summary>
        /// <remarks>
        /// カスタマイズ適用時にこのオブジェクトを対象とします
        /// </remarks>
        public GameObject? P09Model => _p09Model;

        /// <summary>
        /// Animatorを取得します
        /// </summary>
        public Animator? Animator => _animator;

        /// <summary>
        /// 騎乗中かどうかを取得します
        /// </summary>
        public bool IsMounted => _isMounted;

        /// <summary>
        /// 右手のアタッチメントポイントを取得します
        /// </summary>
        public Transform? RightHandAttachment => _rightHandAttachment;

        /// <summary>
        /// 矢筒のアタッチメントポイントを取得します
        /// </summary>
        public Transform? QuiverAttachment => _quiverAttachment;

        #endregion

        #region Unity Lifecycle

        private void Awake()
        {
            ValidateReferences();
        }

        private void Start()
        {
            // 初期アニメーション状態を設定
            if (_animator != null)
            {
                // デフォルトはアイドル状態
                SetAnimationState(RiderAnimationState.Idle);
            }
        }

        private void LateUpdate()
        {
            // 騎乗中は位置と回転をマウントポイントに強制的に同期
            // これはアニメーションのルートモーションが位置/回転を変えてしまうのを防ぐ
            if (_isMounted && _mountPoint != null)
            {
                transform.localPosition = Vector3.zero;
                transform.localRotation = Quaternion.identity;
            }
        }

        #endregion

        #region Public Methods - Animation

        // アニメーターパラメータ名（ハッシュ化して高速アクセス）
        private static readonly int IsMountedHash = Animator.StringToHash("IsMounted");
        private static readonly int IsAimingHash = Animator.StringToHash("IsAiming");
        private static readonly int ShootHash = Animator.StringToHash("Shoot");
        private static readonly int ChargeAmountHash = Animator.StringToHash("ChargeAmount");
        private static readonly int AimHorizontalHash = Animator.StringToHash("AimHorizontal");
        private static readonly int AimVerticalHash = Animator.StringToHash("AimVertical");

        // Aimレイヤーインデックス
        private int _aimLayerIndex = -1;

        /// <summary>
        /// Aimレイヤーのインデックスを取得・キャッシュします
        /// </summary>
        private int GetAimLayerIndex()
        {
            if (_aimLayerIndex < 0 && _animator != null)
            {
                _aimLayerIndex = _animator.GetLayerIndex("Aim");
            }
            return _aimLayerIndex;
        }

        /// <summary>
        /// Animatorにパラメータが存在するか確認します
        /// </summary>
        /// <param name="paramName">パラメータ名</param>
        /// <returns>存在すればtrue</returns>
        private bool HasParameter(string paramName)
        {
            if (_animator == null)
            {
                return false;
            }

            foreach (var param in _animator.parameters)
            {
                if (param.name == paramName)
                {
                    return true;
                }
            }
            return false;
        }

        /// <summary>
        /// アニメーション状態を設定します
        /// </summary>
        /// <param name="state">設定するアニメーション状態</param>
        /// <remarks>
        /// P09_Rider_Controller.controller が設定されている場合、
        /// IsMounted/IsAiming/Shootパラメータを使用してアニメーションを制御します。
        /// 設定されていない場合は、ログ出力のみ行います。
        /// </remarks>
        public void SetAnimationState(RiderAnimationState state)
        {
            if (_debugLog)
            {
                Debug.Log($"[RiderController] Animation state: {state}");
            }

            // Animatorが存在しない場合はスキップ
            if (_animator == null)
            {
                return;
            }

            // パラメータが存在するか確認（初回のみ）
            bool hasIsMounted = HasParameter("IsMounted");
            bool hasIsAiming = HasParameter("IsAiming");

            if (!hasIsMounted && !hasIsAiming)
            {
                // P09_Rider_Controllerが設定されていない場合
                if (_debugLog)
                {
                    Debug.LogWarning("[RiderController] AnimatorController にパラメータが存在しません。P09_Rider_Controller を設定してください。");
                }
                return;
            }

            switch (state)
            {
                case RiderAnimationState.Idle:
                    if (hasIsMounted)
                    {
                        _animator.SetBool(IsMountedHash, false);
                    }
                    if (hasIsAiming)
                    {
                        _animator.SetBool(IsAimingHash, false);
                    }
                    break;

                case RiderAnimationState.MountedIdle:
                    if (hasIsMounted)
                    {
                        _animator.SetBool(IsMountedHash, true);
                    }
                    if (hasIsAiming)
                    {
                        _animator.SetBool(IsAimingHash, false);
                    }
                    break;

                case RiderAnimationState.Aiming:
                    if (hasIsAiming)
                    {
                        _animator.SetBool(IsAimingHash, true);
                    }
                    break;

                case RiderAnimationState.Shooting:
                    if (HasParameter("Shoot"))
                    {
                        _animator.SetTrigger(ShootHash);
                    }
                    break;
            }
        }

        /// <summary>
        /// チャージ量を設定します（弓を引く強さ）
        /// </summary>
        /// <param name="chargeAmount">0.0〜1.0のチャージ量</param>
        public void SetChargeAmount(float chargeAmount)
        {
            if (_animator == null)
            {
                return;
            }

            if (HasParameter("ChargeAmount"))
            {
                _animator.SetFloat(ChargeAmountHash, Mathf.Clamp01(chargeAmount));
            }
        }

        /// <summary>
        /// エイム方向を設定します（Blend Tree用）
        /// </summary>
        /// <param name="horizontal">水平方向 (-1 to 1)</param>
        /// <param name="vertical">垂直方向 (-1 to 1)</param>
        public void SetAimDirection(float horizontal, float vertical)
        {
            if (_animator == null)
            {
                return;
            }

            if (HasParameter("AimHorizontal"))
            {
                _animator.SetFloat(AimHorizontalHash, Mathf.Clamp(horizontal, -1f, 1f));
            }
            if (HasParameter("AimVertical"))
            {
                _animator.SetFloat(AimVerticalHash, Mathf.Clamp(vertical, -1f, 1f));
            }
        }

        /// <summary>
        /// Aimレイヤーのウェイトを設定します
        /// </summary>
        /// <param name="weight">0.0〜1.0のウェイト</param>
        public void SetAimLayerWeight(float weight)
        {
            if (_animator == null)
            {
                return;
            }

            int layerIndex = GetAimLayerIndex();
            if (layerIndex >= 0)
            {
                _animator.SetLayerWeight(layerIndex, Mathf.Clamp01(weight));
            }
        }

        #endregion

        #region Public Methods - Mounting

        /// <summary>
        /// 馬に騎乗します
        /// </summary>
        /// <param name="mountPoint">騎乗ポイントのTransform</param>
        public void MountTo(Transform mountPoint)
        {
            if (mountPoint == null)
            {
                Debug.LogWarning("[RiderController] MountPoint is null");
                return;
            }

            _mountPoint = mountPoint;
            _isMounted = true;

            // 物理演算を無効化（馬との衝突を防ぐ）
            DisablePhysics();

            // ルートモーションを無効化（アニメーションが位置/回転を変えるのを防ぐ）
            if (_animator != null)
            {
                _originalApplyRootMotion = _animator.applyRootMotion;
                _animator.applyRootMotion = false;
            }

            // 騎乗ポイントの子として配置
            transform.SetParent(mountPoint);
            transform.localPosition = Vector3.zero;
            transform.localRotation = Quaternion.identity;

            // 騎乗アニメーションに切り替え
            SetAnimationState(RiderAnimationState.MountedIdle);

            if (_debugLog)
            {
                Debug.Log($"[RiderController] Mounted to: {mountPoint.name}");
            }
        }

        /// <summary>
        /// 物理演算を無効化します（騎乗時）
        /// </summary>
        private void DisablePhysics()
        {
            // Rigidbodyを無効化
            var rb = GetComponent<Rigidbody>();
            if (rb != null)
            {
                rb.isKinematic = true;
                rb.detectCollisions = false;
            }

            // CharacterControllerを無効化
            var cc = GetComponent<CharacterController>();
            if (cc != null)
            {
                cc.enabled = false;
            }

            // Colliderを無効化
            var colliders = GetComponentsInChildren<Collider>();
            foreach (var col in colliders)
            {
                col.enabled = false;
            }
        }

        /// <summary>
        /// 物理演算を有効化します（下馬時）
        /// </summary>
        private void EnablePhysics()
        {
            // Rigidbodyを有効化
            var rb = GetComponent<Rigidbody>();
            if (rb != null)
            {
                rb.isKinematic = false;
                rb.detectCollisions = true;
            }

            // CharacterControllerを有効化
            var cc = GetComponent<CharacterController>();
            if (cc != null)
            {
                cc.enabled = true;
            }

            // Colliderを有効化
            var colliders = GetComponentsInChildren<Collider>();
            foreach (var col in colliders)
            {
                col.enabled = true;
            }
        }

        /// <summary>
        /// 下馬します
        /// </summary>
        /// <param name="dismountPosition">下馬後の位置</param>
        public void Dismount(Vector3 dismountPosition)
        {
            if (!_isMounted)
            {
                return;
            }

            _isMounted = false;
            _mountPoint = null;

            // 親から切り離す
            transform.SetParent(null);
            transform.position = dismountPosition;

            // 物理演算を有効化
            EnablePhysics();

            // ルートモーションを元に戻す
            if (_animator != null)
            {
                _animator.applyRootMotion = _originalApplyRootMotion;
            }

            // アイドルアニメーションに切り替え
            SetAnimationState(RiderAnimationState.Idle);

            if (_debugLog)
            {
                Debug.Log($"[RiderController] Dismounted at: {dismountPosition}");
            }
        }

        #endregion

        #region Public Methods - Customization

        /// <summary>
        /// カスタマイズ適用対象のGameObjectを取得します
        /// </summary>
        /// <returns>P09モデルのGameObject、またはこのGameObject</returns>
        public GameObject GetCustomizationTarget()
        {
            return _p09Model != null ? _p09Model : gameObject;
        }

        #endregion

        #region Private Methods

        /// <summary>
        /// 参照の検証を行います
        /// </summary>
        private void ValidateReferences()
        {
            if (_p09Model == null)
            {
                // 子オブジェクトからP09モデルを自動検出
                _p09Model = FindP09ModelInChildren();

                if (_p09Model == null && _debugLog)
                {
                    Debug.LogWarning("[RiderController] P09Model が設定されていません");
                }
            }

            if (_animator == null && _p09Model != null)
            {
                // P09モデルからAnimatorを取得
                _animator = _p09Model.GetComponent<Animator>();

                if (_animator == null)
                {
                    _animator = _p09Model.GetComponentInChildren<Animator>();
                }
            }

            if (_animator == null && _debugLog)
            {
                Debug.LogWarning("[RiderController] Animator が見つかりません");
            }

            // RiderArcherControllerを自動取得
            if (_archerController == null)
            {
                _archerController = GetComponent<RiderArcherController>();
                if (_archerController == null)
                {
                    _archerController = GetComponentInChildren<RiderArcherController>();
                }

                if (_archerController != null && _debugLog)
                {
                    Debug.Log("[RiderController] RiderArcherController を自動取得しました");
                }
            }

            // RiderAnimationGenderSwapperを自動取得
            if (_genderSwapper == null)
            {
                _genderSwapper = GetComponent<RiderAnimationGenderSwapper>();
                if (_genderSwapper == null)
                {
                    _genderSwapper = GetComponentInChildren<RiderAnimationGenderSwapper>();
                }

                if (_genderSwapper != null && _debugLog)
                {
                    Debug.Log("[RiderController] RiderAnimationGenderSwapper を自動取得しました");
                }
            }
        }

        /// <summary>
        /// 子オブジェクトからP09モデルを検索します
        /// </summary>
        /// <returns>見つかったP09モデル</returns>
        private GameObject? FindP09ModelInChildren()
        {
            foreach (Transform child in transform)
            {
                if (child.name.Contains("P09"))
                {
                    return child.gameObject;
                }
            }

            return null;
        }

        #endregion
    }

    /// <summary>
    /// 騎手のアニメーション状態
    /// </summary>
    public enum RiderAnimationState
    {
        /// <summary>通常のアイドル（地上）</summary>
        Idle,

        /// <summary>騎乗中のアイドル</summary>
        MountedIdle,

        /// <summary>エイム中（弓を構えている）</summary>
        Aiming,

        /// <summary>射撃（矢を放つ）</summary>
        Shooting
    }
}
