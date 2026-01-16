#nullable enable

using UnityEngine;

namespace CavalryFight.Gameplay.Player
{
    /// <summary>
    /// 弓の抜き/収納アクション種類
    /// </summary>
    public enum UnsheatheAction
    {
        Unsheathe,
        Sheathe
    }

    /// <summary>
    /// 弓のUnsheathe/Sheatheをアニメーションステートに連動させるStateMachineBehaviour
    /// </summary>
    /// <remarks>
    /// Kevin IglesiasのHumanArcherUnsheatheBowSMBをRiderArcherController用にアダプト。
    /// ステートに入った時に弓を抜く/収納するアニメーションを実行します。
    /// </remarks>
    public class RiderUnsheatheBowSMB : StateMachineBehaviour
    {
        [Tooltip("実行するアクション")]
        public UnsheatheAction action = UnsheatheAction.Unsheathe;

        [Tooltip("アクション開始までの遅延（秒）")]
        public float delay = 0f;

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
                Debug.LogWarning("[RiderUnsheatheBowSMB] RiderArcherController が見つかりません");
                return;
            }

            switch (action)
            {
                case UnsheatheAction.Unsheathe:
                    _riderArcherController.UnsheatheBow(delay);
                    break;

                case UnsheatheAction.Sheathe:
                    _riderArcherController.SheatheBow(delay);
                    break;
            }
        }
    }
}
