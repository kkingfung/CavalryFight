#nullable enable

using UnityEngine;

namespace CavalryFight.Gameplay.Player
{
    /// <summary>
    /// 矢を取り出すアニメーションステートに連動させるStateMachineBehaviour
    /// </summary>
    /// <remarks>
    /// Kevin IglesiasのHumanArcherGetArrowSMBをRiderArcherController用にアダプト。
    /// ステートに入った時に矢を表示します。
    /// </remarks>
    public class RiderGetArrowSMB : StateMachineBehaviour
    {
        [Tooltip("矢を取り出すまでの遅延（秒）")]
        public float getArrowDelay = 0f;

        private RiderArcherController? _riderArcherController;

        public override void OnStateEnter(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
        {
            if (_riderArcherController == null)
            {
                _riderArcherController = animator.GetComponent<RiderArcherController>();
                if (_riderArcherController == null)
                {
                    _riderArcherController = animator.GetComponentInParent<RiderArcherController>();
                }
            }

            if (_riderArcherController == null)
            {
                Debug.LogWarning("[RiderGetArrowSMB] RiderArcherController が見つかりません");
                return;
            }

            _riderArcherController.GetArrow(getArrowDelay);
        }
    }
}
