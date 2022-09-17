using System.Linq;
using RimWorld;
using Verse;

namespace VFEAncients
{
    public class PowerWorker_Claws : PowerWorker
    {
        public PowerWorker_Claws(PowerDef def) : base(def)
        {
        }

        public override void Notify_Added(Pawn_PowerTracker parent)
        {
            base.Notify_Added(parent);
            AddClaws(parent.Pawn);
        }

        public override void Notify_Removed(Pawn_PowerTracker parent)
        {
            base.Notify_Removed(parent);
            RemoveClaws(parent.Pawn);
        }

        public override void TickLong(Pawn_PowerTracker parent)
        {
            base.TickLong(parent);
            AddClaws(parent.Pawn);
        }

        private static void AddClaws(Pawn pawn)
        {
            foreach (var part in pawn.health.hediffSet.GetNotMissingParts()
                .Except(pawn.health.hediffSet.hediffs.Where(hediff => hediff.def == VFEA_DefOf.VFEA_PlasteelClaw).Select(hediff => hediff.Part))
                .Where(part => part is {def: var bodyPartDef} && bodyPartDef == BodyPartDefOf.Hand))
                pawn.health.AddHediff(VFEA_DefOf.VFEA_PlasteelClaw, part);
        }

        private static void RemoveClaws(Pawn pawn)
        {
            pawn.health.hediffSet.hediffs.RemoveAll(hediff => hediff.def == VFEA_DefOf.VFEA_PlasteelClaw);
        }
    }
}