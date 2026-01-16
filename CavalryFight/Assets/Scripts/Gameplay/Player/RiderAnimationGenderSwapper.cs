#nullable enable

using UnityEngine;
using CavalryFight.Services.Customization;

namespace CavalryFight.Gameplay.Player
{
    /// <summary>
    /// キャラクターの性別に応じてアニメーションを切り替えるコンポーネント
    /// </summary>
    /// <remarks>
    /// Kevin Iglesiasのアニメーションは男性用（HumanM@）と女性用（HumanF@）が別々に提供されています。
    /// AnimatorOverrideControllerを使用して、性別に応じたアニメーションに切り替えます。
    /// </remarks>
    public class RiderAnimationGenderSwapper : MonoBehaviour
    {
        #region Serialized Fields

        [Header("Animator")]
        [Tooltip("対象のAnimator（自動取得）")]
        [SerializeField] private Animator? _animator;

        [Header("Override Controllers")]
        [Tooltip("男性用のAnimatorOverrideController")]
        [SerializeField] private AnimatorOverrideController? _maleOverrideController;

        [Tooltip("女性用のAnimatorOverrideController")]
        [SerializeField] private AnimatorOverrideController? _femaleOverrideController;

        [Header("Debug")]
        [SerializeField] private bool _debugLog = false;

        #endregion

        #region Private Fields

        private RuntimeAnimatorController? _originalController;

        #endregion

        #region Unity Lifecycle

        private void Awake()
        {
            if (_animator == null)
            {
                _animator = GetComponent<Animator>();
                if (_animator == null)
                {
                    _animator = GetComponentInChildren<Animator>();
                }
            }

            if (_animator != null)
            {
                _originalController = _animator.runtimeAnimatorController;
            }
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// 性別に応じたアニメーションコントローラーを設定します
        /// </summary>
        /// <param name="gender">キャラクターの性別</param>
        public void SetGender(Gender gender)
        {
            if (_animator == null)
            {
                Debug.LogWarning("[RiderAnimationGenderSwapper] Animator が設定されていません");
                return;
            }

            AnimatorOverrideController? overrideController = gender == Gender.Male
                ? _maleOverrideController
                : _femaleOverrideController;

            if (overrideController != null)
            {
                _animator.runtimeAnimatorController = overrideController;

                if (_debugLog)
                {
                    Debug.Log($"[RiderAnimationGenderSwapper] アニメーションを {gender} 用に切り替えました");
                }
            }
            else
            {
                // Override Controllerが設定されていない場合は元のコントローラーを使用
                if (_originalController != null)
                {
                    _animator.runtimeAnimatorController = _originalController;
                }

                if (_debugLog)
                {
                    Debug.Log($"[RiderAnimationGenderSwapper] {gender} 用のOverrideControllerが設定されていないため、デフォルトを使用");
                }
            }
        }

        /// <summary>
        /// 元のアニメーションコントローラーに戻します
        /// </summary>
        public void ResetToOriginal()
        {
            if (_animator != null && _originalController != null)
            {
                _animator.runtimeAnimatorController = _originalController;

                if (_debugLog)
                {
                    Debug.Log("[RiderAnimationGenderSwapper] 元のアニメーションコントローラーに戻しました");
                }
            }
        }

        #endregion
    }
}
