using Verse;

namespace VFEAncients
{
    public class PowerWorker_Break : PowerWorker
    {
        public PowerWorker_Break(PowerDef def) : base(def)
        {
        }

        public override void TickLong(Pawn_PowerTracker parent)
        {
            base.TickLong(parent);
            if (parent.Pawn is not null && GetData<WorkerData_Break>() is {BreakChance: var chance, Break: { } breakDef} && Rand.Chance(chance))
                breakDef.Worker.TryStart(parent.Pawn, null, false);
        }
    }
}