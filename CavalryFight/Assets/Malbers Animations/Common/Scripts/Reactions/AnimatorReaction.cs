using MalbersAnimations.Utilities;
using System.Collections.Generic;
using UnityEngine;

namespace MalbersAnimations.Reactions
{
    [System.Serializable]

    [AddTypeMenu("Unity/Animator SetParameter")]

    public class AnimatorReaction : Reaction
    {

        public override string DynamicName
        {
            get
            {
                var display = $"Set Animator Parameters ({parameters.Count})"; //Name of the Reaction
                foreach (var param in parameters)
                    display += $"[{param.param} {param.type} param {param.Value.Value}]";
                return display;
            }
        }


        public override System.Type ReactionType => typeof(Animator);

        public List<MAnimatorParameter> parameters = new();

        public void Set(Animator anim)
        {
            foreach (var param in parameters)
                param.Set(anim);
        }

        protected override bool _TryReact(Component component)
        {
            Set(component as Animator);
            return true;
        }
    }
}
