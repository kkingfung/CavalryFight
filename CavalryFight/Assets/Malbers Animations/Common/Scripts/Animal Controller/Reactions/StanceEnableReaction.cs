using MalbersAnimations.Controller;
using UnityEngine;

namespace MalbersAnimations.Reactions
{
    [System.Serializable]
    [AddTypeMenu("Malbers/Animal/Stance Enable-Disable")]
    public class StanceEnableReaction : MReaction
    {
        override public string DynamicName =>
        $"Enable-Disable stances [Total: {(stances != null ? stances.Length : 0)}]";

        public IDEnable<StanceID>[] stances;

        protected override bool _TryReact(Component component)
        {
            var animal = component as MAnimal;

            foreach (var id in stances)
            {
                var st = animal.Stance_Get(id.ID);
                st?.Enable(id.enable);
            }
            return true;
        }
    }
}
