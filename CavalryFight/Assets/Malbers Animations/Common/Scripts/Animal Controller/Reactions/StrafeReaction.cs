using MalbersAnimations.Controller;
using MalbersAnimations.Scriptables;
using UnityEngine;

namespace MalbersAnimations.Reactions
{
    [System.Serializable, AddTypeMenu("Malbers/Animal/Strafe")]
    public class StrafeReaction : MReaction
    {
        override public string DynamicName => $"Animal Strafe [{Strafe.Value}]";

        public BoolReference Strafe = new();

        protected override bool _TryReact(Component component)
        {
            var animal = component as MAnimal;

            animal.Strafe = Strafe;

            return true;
        }
    }
}
