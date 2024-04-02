using RimWorld;
using RimWorld.Planet;
using Verse;
using VFECore.Abilities;

namespace VFEAncients;

public class Ability_Jump : Ability
{
    public override void Cast(params GlobalTargetInfo[] targets)
    {
        base.Cast(targets);
        var map = Caster.Map;
        var flyer = (AbilityPawnFlyer)PawnFlyer.MakeFlyer(VFEA_DefOf.VFEA_SuperJumpingPawn, CasterPawn, targets[0].Cell, null, null);
        flyer.ability = this;
        GenSpawn.Spawn(flyer, Caster.Position, map);
    }
}
