using MalbersAnimations.Weapons;
using UnityEngine;

namespace MalbersAnimations.Conditions
{
    [System.Serializable, MDescription("Is Weapon gameobject")]
    public abstract class MWeaponConditions : ConditionCore
    {
        public MWeapon Target;
        public virtual void SetTarget(MWeapon n) => Target = n;
        protected override void _SetTarget(Object target) => MTools.VerifyComponent(target, Target);
    }
}