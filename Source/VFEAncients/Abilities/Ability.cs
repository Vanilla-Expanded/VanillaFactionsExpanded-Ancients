using VFECore.Abilities;

namespace VFEAncients
{
    public class Ability : VFECore.Abilities.Ability
    {
        public override float GetRadiusForPawn() => def.GetModExtension<AbilityExtension_Projectile>() is {projectile: {projectile: {explosionRadius: var radius and > 0f}}}
            ? radius
            : base.GetRadiusForPawn();
    }
}