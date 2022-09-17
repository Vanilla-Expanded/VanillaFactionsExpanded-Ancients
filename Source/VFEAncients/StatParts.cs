using System;
using RimWorld;
using Verse;

namespace VFEAncients
{
    public class StatPart_PawnStat : StatPart
    {
        public StatDef statFactor;
        public StatDef statOffset;

        public override void TransformValue(StatRequest req, ref float val)
        {
            if (req.HasThing)
            {
                Pawn pawn;
                if (req.Thing.ParentHolder is Pawn_ApparelTracker ap) pawn = ap.pawn;
                else if (req.Thing.ParentHolder is Pawn_EquipmentTracker eq) pawn = eq.pawn;
                else return;
                if (pawn == null) return;

                if (statFactor != null)
                    val *= pawn.GetStatValue(statFactor);
                if (statOffset != null)
                    val += pawn.GetStatValue(statOffset);
            }
        }

        public override string ExplanationPart(StatRequest req)
        {
            if (req.HasThing)
            {
                Pawn pawn;
                if (req.Thing.ParentHolder is Pawn_ApparelTracker ap) pawn = ap.pawn;
                else if (req.Thing.ParentHolder is Pawn_EquipmentTracker eq) pawn = eq.pawn;
                else return "";

                var str = "";
                if (statFactor != null)
                {
                    var factor = pawn.GetStatValue(statFactor);
                    if (Math.Abs(factor - 1f) > 0.0001f)
                        str += (str.NullOrEmpty() ? "" : "\n") + pawn.LabelCap + ": " + statFactor.Worker.ValueToString(factor, false, ToStringNumberSense.Factor);
                }

                if (statOffset != null)
                {
                    var offset = pawn.GetStatValue(statOffset);
                    if (offset != 0f)
                        str += (str.NullOrEmpty() ? "" : "\n") + pawn.LabelCap + ": " + statOffset.Worker.ValueToString(offset, false, ToStringNumberSense.Offset);
                }

                return str;
            }

            return "";
        }
    }
}