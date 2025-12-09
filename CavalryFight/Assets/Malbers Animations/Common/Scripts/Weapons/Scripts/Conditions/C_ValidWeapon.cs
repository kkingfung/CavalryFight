namespace MalbersAnimations.Conditions
{
    [System.Serializable, AddTypeMenu("Weapons/Is Valid Weapon")]
    public class C_ValidWeapon : MWeaponConditions
    {
        protected override bool _Evaluate()
        {
            return Target != null;
        }
    }
}
