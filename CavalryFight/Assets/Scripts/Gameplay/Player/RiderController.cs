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

        [Header("Debug")]
        [SerializeField] private bool _debugLog = false;

        #endregion

        #region Private Fields

        private bool _isMounted = false;
        private Transform? _mountPoint;

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

        #endregion

        #region Public Methods - Animation

        /// <summary>
        /// アニメーション状態を設定します
        /// </summary>
        /// <param name="state">設定するアニメーション状態</param>
        public void SetAnimationState(RiderAnimationState state)
        {
            if (_animator == null)
            {
                return;
            }

            switch (state)
            {
                case RiderAnimationState.Idle:
                    // 通常のアイドル
                    _animator.SetBool("IsMounted", false);
                    _animator.SetBool("IsAiming", false);
                    break;

                case RiderAnimationState.MountedIdle:
                    // 騎乗アイドル
                    _animator.SetBool("IsMounted", true);
                    _animator.SetBool("IsAiming", false);
                    break;

                case RiderAnimationState.Aiming:
                    // エイム中
                    _animator.SetBool("IsAiming", true);
                    break;

                case RiderAnimationState.Shooting:
                    // 射撃トリガー
                    _animator.SetTrigger("Shoot");
                    break;
            }

            if (_debugLog)
            {
                Debug.Log($"[RiderController] Animation state: {state}");
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

            _animator.SetFloat("ChargeAmount", Mathf.Clamp01(chargeAmount));
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
