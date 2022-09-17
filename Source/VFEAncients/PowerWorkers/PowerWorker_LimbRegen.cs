using System.Collections.Generic;
using Verse;

namespace VFEAncients
{
    public class PowerWorker_LimbRegen : PowerWorker
    {
        public PowerWorker_LimbRegen(PowerDef def) : base(def)
        {
        }

        public override void Tick(Pawn_PowerTracker parent)
        {
            base.Tick(parent);
            var pawn = parent.Pawn;
            var toRemove = new List<Hediff>();
            var toAdd = new List<Hediff>();
            foreach (var hediff in pawn.health.hediffSet.hediffs)
            {
                if (hediff is Hediff_MissingPart && !pawn.health.hediffSet.PartIsMissing(hediff.Part.parent) &&
                    !pawn.health.hediffSet.PartOrAnyAncestorHasDirectlyAddedParts(hediff.Part))
                {
                    var part = hediff.Part;
                    var flag = true;
                    while (part != null)
                        if (pawn.health.hediffSet.hediffs.Any(hd => hd.Part == part && hd.def == VFEA_DefOf.VFEA_RegrowingPart))
                        {
                            flag = false;
                            part = null;
                        }
                        else
                        {
                            part = part.parent;
                        }

                    if (flag)
                    {
                        var newHediff = HediffMaker.MakeHediff(VFEA_DefOf.VFEA_RegrowingPart, pawn, hediff.Part);
                        newHediff.Severity = 0.01f;
                        toAdd.Add(newHediff);
                        toRemove.Add(hediff);
                    }
                }

                if (hediff.def == VFEA_DefOf.VFEA_RegrowingPart)
                {
                    hediff.Severity += 1f / 180000f;
                    if (hediff.Severity >= 1f) toRemove.Add(hediff);
                }
            }

            foreach (var hediff in toRemove) pawn.health.RemoveHediff(hediff);

            foreach (var hediff in toAdd) pawn.health.AddHediff(hediff);
        }
    }
}