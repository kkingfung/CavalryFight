using System.Collections.Generic;
using UnityEngine;

namespace MalbersAnimations.Reactions
{
    [System.Serializable]
    [AddTypeMenu("Malbers/Stats/Stat Modify")]

    public class StatReaction : Reaction
    {
        public override string DynamicName => $"Modify Stats. Total [{modifiers.Count}]"; //Name of the Reaction


        public List<StatModifier> modifiers = new()

        {
            new StatModifier()
            {
                //ID = MTools.GetInstance<StatID>("Health"),
                 modify = StatOption.SubstractValue, MinValue = 15, MaxValue = 20,
            },
        };

        public override System.Type ReactionType => typeof(Stats);

        protected override bool _TryReact(Component reactor)
        {
            var stats = reactor as Stats;

            foreach (var modifier in modifiers)
            {
                modifier.ModifyStat(stats);
            }

            return true;
        }
    }
}
