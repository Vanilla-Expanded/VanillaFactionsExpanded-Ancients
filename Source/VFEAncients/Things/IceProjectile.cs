using RimWorld;
using UnityEngine;
using Verse;
using VFECore;

namespace VFEAncients;

public class IceProjectile : ExpandableProjectile
{
    public override void DoDamage(IntVec3 pos)
    {
        base.DoDamage(pos);
        try
        {
            if (pos != launcher.Position && launcher.Map != null && pos.InBounds(launcher.Map))
            {
                var list = launcher.Map.thingGrid.ThingsListAt(pos);
                for (var num = list.Count - 1; num >= 0; num--)
                    if (list[num].def != def && list[num] != launcher && list[num].def != ThingDefOf.Fire && !(list[num] is Mote) && !(list[num] is Filth))
                    {
                        customImpact = true;
                        base.Impact(list[num]);
                        customImpact = false;
                        if (list[num] is Pawn p)
                        {
                            p.stances?.stagger?.StaggerFor(300);

                            if (p.health.hediffSet.GetFirstHediffOfDef(HediffDefOf.Hypothermia) is not { } hediff)
                                hediff = p.health.AddHediff(HediffDefOf.Hypothermia);
                            hediff.Severity = Mathf.Max(hediff.Severity, 0.7f);
                        }
                    }
            }
        }
        catch { }

        ;
    }
}
