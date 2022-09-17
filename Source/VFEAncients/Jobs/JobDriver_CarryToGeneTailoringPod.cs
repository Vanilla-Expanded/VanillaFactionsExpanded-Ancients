using System.Collections.Generic;
using RimWorld;
using Verse;
using Verse.AI;

namespace VFEAncients
{
    public class JobDriver_CarryToGeneTailoringPod : JobDriver
    {
        private const TargetIndex TakeeInd = TargetIndex.A;

        private const TargetIndex PodInd = TargetIndex.B;

        private Pawn Takee => job.GetTarget(TakeeInd).Pawn;

        private CompGeneTailoringPod Pod => job.GetTarget(PodInd).Thing.TryGetComp<CompGeneTailoringPod>();

        public override bool TryMakePreToilReservations(bool errorOnFailed) =>
            pawn.Reserve(Takee, job, 1, -1, null, errorOnFailed) && pawn.Reserve(Pod.parent, job, 1, -1, null, errorOnFailed);

        public override IEnumerable<Toil> MakeNewToils()
        {
            this.FailOnDestroyedOrNull(TakeeInd);
            this.FailOnDestroyedOrNull(PodInd);
            this.FailOnAggroMentalState(TakeeInd);
            this.FailOn(() => !Pod.CanAccept(Takee));
            var goToTakee = Toils_Goto.GotoThing(TakeeInd, PathEndMode.OnCell).FailOnDestroyedNullOrForbidden(TakeeInd).FailOnDespawnedNullOrForbidden(PodInd)
                .FailOn(() => Takee.IsColonist && !Takee.Downed).FailOnSomeonePhysicallyInteracting(TakeeInd);
            var startCarryingTakee = Toils_Haul.StartCarryThing(TakeeInd);
            var goToThing = Toils_Goto.GotoThing(PodInd, PathEndMode.InteractionCell);
            yield return Toils_Jump.JumpIf(goToThing, () => pawn.IsCarryingPawn(Takee));
            yield return goToTakee;
            yield return startCarryingTakee;
            yield return goToThing;
            yield return JobDriver_EnterGeneTailoringPod.PrepareToEnterToil(PodInd);
            yield return new Toil
            {
                initAction = delegate { Pod.TryAcceptPawn(Takee); },
                defaultCompleteMode = ToilCompleteMode.Instant
            };
        }
    }
}