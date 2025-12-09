using System;
using UnityEngine;

namespace MalbersAnimations.Reactions
{
    [Serializable, AddTypeMenu("Unity/Behaviour")]
    public class BehaviourReaction : Reaction
    {
        public override string DynamicName
        {
            get
            {
                var display = $"Behaviour Reaction [{action}]"; //Name of the Reaction

                switch (action)
                {
                    case Behaviour_Reaction.SetEnable:
                        display += $" [{value}]";
                        break;
                    case Behaviour_Reaction.Destroy:
                        display += $" [Time: {time}]";
                        break;
                    default:
                        break;
                }

                return display;
            }
        }

        public enum Behaviour_Reaction { SetEnable, Destroy }

        public override Type ReactionType => typeof(Behaviour);

        public Behaviour_Reaction action = Behaviour_Reaction.SetEnable;
        [Hide("action", (int)Behaviour_Reaction.SetEnable)]
        public bool value = true;
        [Hide("action", (int)Behaviour_Reaction.Destroy)]
        public float time = 0;

        protected override bool _TryReact(Component component)
        {
            var beha = component as Behaviour;

            switch (action)
            {
                case Behaviour_Reaction.SetEnable:
                    beha.enabled = value;
                    return true;
                case Behaviour_Reaction.Destroy:
                    Component.Destroy(component, time);
                    return true;
                default:
                    break;
            }
            return false;
        }
    }
}
