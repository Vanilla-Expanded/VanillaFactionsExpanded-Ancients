using RimWorld;
using Verse;

namespace VFEAncients
{
    public class ThoughtWorker_OutsideParanoid : ThoughtWorker
    {
        public override ThoughtState CurrentStateInternal(Pawn p) =>
            (p.GetPowerTracker()?.HasPower(VFEA_DefOf.Paranoid) ?? false) && !(p.MapHeld?.areaManager.Home[p.Position] ?? false);
    }
}