using RimWorld;
using Verse;
using Verse.AI;

namespace VFEAncients
{
    public class WorkGiver_Warden_Interrogate : WorkGiver_Warden
    {
        public override Job JobOnThing(Pawn pawn, Thing t, bool forced = false)
        {
            if (!ShouldTakeCareOfPrisoner(pawn, t, forced)) return null;
            var p = (Pawn) t;
            if (p.guest.interactionMode == VFEA_DefOf.VFEA_Interrogate && p.guest.ScheduledForInteraction && p.guest.IsPrisoner && !p.Downed &&
                pawn.health.capacities.CapableOf(PawnCapacityDefOf.Talking) && pawn.CanReserveAndReach(t, PathEndMode.Touch, Danger.Some) && p.Awake())
                return JobMaker.MakeJob(VFEA_DefOf.VFEA_PrisonerInterrogate, t);
            return null;
        }
    }
}