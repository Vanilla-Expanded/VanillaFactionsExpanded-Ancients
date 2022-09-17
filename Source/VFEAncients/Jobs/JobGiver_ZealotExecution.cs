using System.Linq;
using Verse;
using Verse.AI;

namespace VFEAncients
{
    public class JobGiver_ZealotExecution : ThinkNode_JobGiver
    {
        public override Job TryGiveJob(Pawn pawn)
        {
            return pawn.Map.mapPawns.AllPawnsSpawned
                .Where(p => p.IsPrisoner && p.guest.HostFaction == pawn.Faction && pawn.CanReserveAndReach(p, PathEndMode.ClosestTouch, Danger.Deadly))
                .Select(p => JobMaker.MakeJob(VFEA_DefOf.VFEA_ZealotExecution, p)).FirstOrDefault();
        }
    }
}