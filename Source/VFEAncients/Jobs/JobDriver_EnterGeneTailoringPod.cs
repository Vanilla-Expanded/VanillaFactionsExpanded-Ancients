using System.Collections.Generic;
using RimWorld;
using Verse;
using Verse.AI;

namespace VFEAncients
{
    public class JobDriver_EnterGeneTailoringPod : JobDriver
    {
        public override bool TryMakePreToilReservations(bool errorOnFailed) => pawn.Reserve(job.targetA, job, 1, -1, null, errorOnFailed);

        public override IEnumerable<Toil> MakeNewToils()
        {
            this.FailOnDespawnedOrNull(TargetIndex.A);
            this.FailOn(() => !job.targetA.Thing.TryGetComp<CompGeneTailoringPod>().CanAccept(GetActor()));
            yield return Toils_Goto.GotoThing(TargetIndex.A, PathEndMode.InteractionCell);
            yield return PrepareToEnterToil(TargetIndex.A);
            var enter = new Toil();
            enter.initAction = delegate
            {
                var actor = enter.actor;
                var compBiosculpterPod = actor.CurJob.targetA.Thing.TryGetComp<CompGeneTailoringPod>();
                if (compBiosculpterPod == null) return;

                compBiosculpterPod.TryAcceptPawn(actor);
            };
            enter.defaultCompleteMode = ToilCompleteMode.Instant;
            yield return enter;
        }

        public static Toil PrepareToEnterToil(TargetIndex podIndex)
        {
            var prepare = Toils_General.Wait(JobDriver_EnterBiosculpterPod.EnterPodDelay);
            prepare.FailOnCannotTouch(podIndex, PathEndMode.InteractionCell);
            prepare.WithProgressBarToilDelay(podIndex);
            return prepare;
        }
    }
}