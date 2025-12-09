using MalbersAnimations.Reactions;
using System;
using UnityEngine;

namespace MalbersAnimations.Weapons
{
    [System.Serializable]
    [AddTypeMenu("Tools/Projectile Thrower")]

    public class MProjectileThrowerReaction : Reaction
    {

        public override string DynamicName
        {
            get
            {
                var display = $"Projectile Thrower [{action}]"; //Name of the Reaction
                switch (action)
                {
                    case ProjectileThrowerActions.SetProjectile:
                        display += $" [{(projectile != null ? projectile.name : "None")}]";
                        break;
                    case ProjectileThrowerActions.SetTarget:
                        display += $" [{(target != null ? target.name : "None")}]";
                        break;
                    case ProjectileThrowerActions.SetDamageMultiplier:
                    case ProjectileThrowerActions.SetScaleMultiplier:
                    case ProjectileThrowerActions.SetForceMultiplier:
                    case ProjectileThrowerActions.SetForce:
                    case ProjectileThrowerActions.SetAngle:
                    case ProjectileThrowerActions.SetAfterDistance:
                        display += $" [{value}]";
                        break;
                    default:
                        break;
                }
                return display;
            }
        }


        public enum ProjectileThrowerActions { SetProjectile, SetTarget, SetDamageMultiplier, SetScaleMultiplier, SetForceMultiplier, SetForce, SetAngle, SetAfterDistance, Fire }

        public ProjectileThrowerActions action = ProjectileThrowerActions.SetProjectile;

        [Hide("action", (int)ProjectileThrowerActions.SetProjectile)]
        public GameObject projectile;
        [Hide("action", (int)ProjectileThrowerActions.SetTarget)]
        public Transform target;
        [Hide("action", true, (int)ProjectileThrowerActions.SetProjectile, (int)ProjectileThrowerActions.SetTarget, (int)ProjectileThrowerActions.Fire)]
        public float value;

        public override Type ReactionType => typeof(MProjectileThrower);

        protected override bool _TryReact(Component reactor)
        {
            if (reactor is MProjectileThrower thrower)
            {
                switch (action)
                {
                    case ProjectileThrowerActions.SetProjectile:
                        thrower.SetProjectile(projectile);
                        break;
                    case ProjectileThrowerActions.SetTarget:
                        thrower.SetTarget(target);
                        break;
                    case ProjectileThrowerActions.SetDamageMultiplier:
                        thrower.SetDamageMultiplier(value);
                        break;
                    case ProjectileThrowerActions.SetScaleMultiplier:
                        thrower.SetScaleMultiplier(value);
                        break;
                    case ProjectileThrowerActions.SetForceMultiplier:
                        thrower.SetForceMultiplier(value);
                        break;
                    case ProjectileThrowerActions.SetForce:
                        thrower.Power = value;
                        break;
                    case ProjectileThrowerActions.SetAngle:
                        thrower.Angle = value;
                        break;
                    case ProjectileThrowerActions.SetAfterDistance:
                        thrower.AfterDistance = value;
                        break;
                    case ProjectileThrowerActions.Fire:
                        thrower.Fire();
                        break;
                    default:
                        break;
                }
            }
            return true;
        }
    }
}
