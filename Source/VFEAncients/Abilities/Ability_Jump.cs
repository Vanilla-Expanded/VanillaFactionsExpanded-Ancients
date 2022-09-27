using RimWorld;
using RimWorld.Planet;
using Verse;

namespace VFEAncients;

public class Ability_Jump : Ability
{
    public override void Cast(params GlobalTargetInfo[] targets)
    {
        base.Cast(targets);
        var map = Caster.Map;
        var flyer = (SuperJumpingPawn)PawnFlyer.MakeFlyer(VFEA_DefOf.VFEA_SuperJumpingPawn, CasterPawn, targets[0].Cell, null, null);
        flyer.ability = this;
        flyer.target = targets[0].Cell.ToVector3();
        GenSpawn.Spawn(flyer, Caster.Position, map);
    }
}