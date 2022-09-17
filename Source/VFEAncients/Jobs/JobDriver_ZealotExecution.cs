using System.Collections.Generic;
using RimWorld;
using Verse;
using Verse.AI;

namespace VFEAncients
{
    internal class JobDriver_ZealotExecution : JobDriver
    {
        protected Pawn Victim => (Pawn) job.targetA.Thing;

        public override bool TryMakePreToilReservations(bool errorOnFailed) => pawn.Reserve(Victim, job, 1, -1, null, errorOnFailed);

        public override IEnumerable<Toil> MakeNewToils()
        {
            this.FailOnAggroMentalState(TargetIndex.A);
            this.FailOnDestroyedOrNull(TargetIndex.A);
            yield return new Toil
            {
                initAction = delegate { pawn.pather.StartPath(Victim, PathEndMode.Touch); },
                defaultCompleteMode = ToilCompleteMode.PatherArrival,
                socialMode = RandomSocialMode.Off
            };
            yield return new Toil
            {
                initAction = delegate
                {
                    ExecutionUtility.DoExecutionByCut(pawn, Victim);
                    ThoughtUtility.GiveThoughtsForPawnExecuted(Victim, pawn, PawnExecutionKind.GenericBrutal);
                    TaleRecorder.RecordTale(TaleDefOf.ExecutedPrisoner, pawn, Victim);
                },
                defaultCompleteMode = ToilCompleteMode.Instant,
                activeSkill = () => SkillDefOf.Melee
            };
        }
    }
}