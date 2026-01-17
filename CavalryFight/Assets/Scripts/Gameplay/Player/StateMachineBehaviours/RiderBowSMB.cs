#nullable enable

using UnityEngine;

namespace CavalryFight.Gameplay.Player
{
    /// <summary>
    /// 弓のPull/Releaseアクションの条件
    /// </summary>
    public enum BowConditions
    {
        OnEnter,
        OnExit
    }

    /// <summary>
    /// 弓のアクション種類
    /// </summary>
    public enum BowActions
    {
        Pull,
        Release,
        Cancel
    }

    /// <summary>
    /// 弓のPull/Releaseをアニメーションステートに連動させるStateMachineBehaviour
    /// </summary>
    /// <remarks>
    /// Kevin IglesiasのHumanArcherBowSMBをRiderArcherController用にアダプト。
    /// ステートに入った時/出た時に弓を引く/放すアニメーションを実行します。
    /// </remarks>
    public class RiderBowSMB : StateMachineBehaviour
    {
        [Tooltip("いつアクションを実行するか")]
        public BowConditions condition = BowConditions.OnEnter;

        [Tooltip("実行するアクション")]
        public BowActions bowAction = BowActions.Pull;

        [Tooltip("アクション開始までの遅延（秒）")]
        public float delay = 0f;

        [Tooltip("アクションの持続時間（秒）")]
        public float duration = 0.2f;

        private RiderArcherController? _riderArcherController;

        public override void OnStateEnter(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
        {
            if (condition != BowConditions.OnEnter)
            {
                return;
            }

            ExecuteAction(animator);
        }

        public override void OnStateExit(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
        {
            if (condition != BowConditions.OnExit)
            {
                return;
            }

            ExecuteAction(animator);
        }

        private void ExecuteAction(Animator animator)
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
                Debug.LogWarning("[RiderBowSMB] RiderArcherController が見つかりません");
                return;
            }

            switch (bowAction)
            {
                case BowActions.Pull:
                    _riderArcherController.LoadBow(delay, duration);
                    break;

                case BowActions.Release:
                    _riderArcherController.ShootArrow(delay, duration);
                    break;

                case BowActions.Cancel:
                    _riderArcherController.CancelLoadBow(delay, duration);
                    break;
            }
        }
    }
}
