using RimWorld;
using RimWorld.Planet;
using Verse;

namespace VFEAncients;

public class Ability_Berserk : Ability
{
    public override void Cast(params GlobalTargetInfo[] targets)
    {
        base.Cast(targets);
        foreach (var target in targets)
            if (target is { HasThing: true, Thing: Pawn pawn })
                pawn.mindState.mentalStateHandler.TryStartMentalState(MentalStateDefOf.Berserk, null, true, false, false, null, false, false, true);
    }
}
