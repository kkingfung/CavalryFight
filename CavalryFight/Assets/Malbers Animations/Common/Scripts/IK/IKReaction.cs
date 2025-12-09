using MalbersAnimations.Reactions;
using MalbersAnimations.Scriptables;
using System;
using UnityEngine;

namespace MalbersAnimations.IK
{
    [System.Serializable, AddTypeMenu("Malbers/IK")]
    public class IKReaction : Reaction
    {
        public override string DynamicName => $"IK Reaction [{action}: {IKSet.Value}]";

        public override Type ReactionType => typeof(IIKSource);
        public enum IKReactionType { Activate, Deactivate, SetTargets, ClearTargets }

        public IKReactionType action = IKReactionType.Activate;
        public StringReference IKSet = new("IKSetName");

        [Tooltip("The targets to set for the IK Source. When Activate, or Set Targets is called")]
        public TransformReference[] targets;

        protected override bool _TryReact(Component reactor)
        {
            if (reactor is not IIKSource IK) return false; //If the source is null, return false (No Component to React_)

            switch (action)
            {
                case IKReactionType.Activate: IK.Set_Enable(IKSet); break;
                case IKReactionType.Deactivate: IK.Set_Disable(IKSet); break;
                case IKReactionType.SetTargets:
                    var targets = new Transform[this.targets.Length];
                    for (int i = 0; i < this.targets.Length; i++) targets[i] = this.targets[i].Value;
                    IK.Target_Set(IKSet, targets);
                    break;
                case IKReactionType.ClearTargets: IK.Target_Clear(IKSet); break;
                default: break;
            }
            return true;
        }
    }
}