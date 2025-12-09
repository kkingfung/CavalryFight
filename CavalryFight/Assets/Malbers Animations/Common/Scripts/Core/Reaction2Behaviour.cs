using System.Collections.Generic;
using UnityEngine;

namespace MalbersAnimations.Reactions
{
    public class Reaction2Behaviour : StateMachineBehaviour
    {
        [Tooltip("List of reactions to send to the animator")]
        public List<Reaction2B> reactionsOnAnimator = new();

        override public void OnStateEnter(Animator anim, AnimatorStateInfo stateInfo, int layerIndex)
        {
            foreach (var item in reactionsOnAnimator)
            {
                item.sent = false;

                if (item.Time == 0) item.React(anim);
                GetIgnoreTransitionHash(item);
            }
        }

        override public void OnStateUpdate(Animator anim, AnimatorStateInfo state, int layer)
        {
            var NextAnim = anim.GetNextAnimatorStateInfo(layer).shortNameHash;
            var InTransition = anim.IsInTransition(layer) && state.shortNameHash != NextAnim; //Check only the Exit Transition not the Start Transition
            var time = state.normalizedTime % 1;


            foreach (var e in reactionsOnAnimator)
            {
                if (e.sent) continue; //If the effect was already sent keep looking for the next one

                if (InTransition)
                {
                    if (e.IgnoreInTransitionHash.Contains(NextAnim))
                    {
                        e.sent = true;
                    }
                    else if (e.Time == 1 && e.ExitInTransition) //If is a quick exit transition
                    {
                        e.React(anim);
                        return;
                    }
                }

                //Regular Update Check for the Effect
                if (time >= e.Time)
                {
                    e.React(anim);
                }
            }
        }

        override public void OnStateExit(Animator anim, AnimatorStateInfo state, int layer)
        {
            if (anim.GetCurrentAnimatorStateInfo(layer).fullPathHash == state.fullPathHash) return; //means is transitioning to it self

            foreach (var reaction in reactionsOnAnimator)
            {
                if (reaction.Time == 1 && !reaction.sent)
                {
                    reaction.React(anim);
                }
            }
        }

        private void GetIgnoreTransitionHash(Reaction2B item)
        {
            //Gather all the hashes the first time only
            if (item.IgnoreInTransitionHash == null)
            {
                item.IgnoreInTransitionHash = new List<int>();

                if (item.IgnoreInTransition != null && item.IgnoreInTransition.Count > 0)
                {
                    foreach (var hash in item.IgnoreInTransition)
                    {
                        item.IgnoreInTransitionHash.Add(Animator.StringToHash(hash));
                    }
                }
            }
        }

        private void OnValidate()
        {
            for (int i = 0; i < reactionsOnAnimator.Count; i++)
            {
                var react = reactionsOnAnimator[i];
                react.display = $"[Reaction ({react.reactions.reactions.Length})]";

                if (react.Time == 0)
                    react.display += $"  -  [On Enter]";
                else if (react.Time == 1)
                    react.display += $"  -  [On Exit]";
                else
                    react.display += $"  -  [OnTime] ({react.Time:F2})";

                if (react.ExitInTransition && react.Time == 1) react.display += "[In Transition]";

                react.showExecute = react.Time != 1 && react.Time != 0;
                react.showExitInTransition = react.Time == 1;
            }
        }
    }

    [System.Serializable]
    public class Reaction2B
    {
        [HideInInspector] public string display;
        [HideInInspector] public bool showExecute;
        [HideInInspector] public bool showExitInTransition;

        [Range(0, 1)]
        public float Time;
        public Reaction2 reactions;

        public bool sent { get; set; }
        [Tooltip("If the animation was interrupted by a transition and the Time has not played yet, execute the Reaction anyways")]
        [Hide(nameof(showExecute))]
        public bool ExecuteOnExit = true;

        [Tooltip("If the animation is interrupted, Execute the Reaction as soon as it start transition to another Animation State")]
        [Hide(nameof(showExitInTransition))]
        public bool ExitInTransition = true;

        [Tooltip("Ignore the Reaction if Execute is called in a transition for the next anim state. included in any of these State List")]
        public List<string> IgnoreInTransition = new();
        public List<int> IgnoreInTransitionHash { get; set; }

        public void React(Component target)
        {
            reactions.React(target);
            sent = true;
        }
    }
}
