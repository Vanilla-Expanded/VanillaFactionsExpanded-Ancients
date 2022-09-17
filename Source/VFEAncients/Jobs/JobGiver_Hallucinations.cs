using System.Linq;
using RimWorld;
using Verse;
using Verse.AI;

namespace VFEAncients
{
    public class JobGiver_Hallucinations : ThinkNode_JobGiver
    {
        public override Job TryGiveJob(Pawn pawn)
        {
            var verb = pawn.CurrentEffectiveVerb;
            if (verb != null && Rand.Chance(0.25f) && GenRadial.RadialDistinctThingsAround(pawn.Position, pawn.Map, verb.verbProps.range, false).Where(t => verb.CanHitTarget(t))
                .TryRandomElement(out var target))
            {
                var job = JobMaker.MakeJob(JobDefOf.AttackStatic, target);
                job.maxNumStaticAttacks = Rand.Range(1, 3);
                job.verbToUse = verb;
                return job;
            }

            return null;
        }
    }
}