using System.Linq;
using RimWorld.Planet;
using Verse;

namespace VFEAncients;

public class Ability_Heal : Ability
{
    public override void Cast(params GlobalTargetInfo[] targets)
    {
        base.Cast(targets);
        foreach (var target in targets)
            if (target.Thing is Pawn p)
                foreach (var injury in p.health.hediffSet.hediffs.OfType<Hediff_Injury>().ToList())
                    p.health.RemoveHediff(injury);
    }
}
