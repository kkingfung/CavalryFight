#nullable enable

using UnityEngine;

namespace CavalryFight.Gameplay.Player
{
    /// <summary>
    /// 別のトリガーを発火させるStateMachineBehaviour
    /// </summary>
    /// <remarks>
    /// Kevin IglesiasのHumanArcherTriggerSMBをアダプト。
    /// ステートに入った時に指定したトリガーを発火します。
    /// </remarks>
    public class RiderTriggerSMB : StateMachineBehaviour
    {
        [Tooltip("発火させるトリガー名")]
        public string triggerToAdd = "";

        public override void OnStateEnter(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
        {
            if (!string.IsNullOrEmpty(triggerToAdd))
            {
                animator.SetTrigger(triggerToAdd);
            }
        }
    }
}
