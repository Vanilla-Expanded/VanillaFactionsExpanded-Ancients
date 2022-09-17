using RimWorld;
using Verse;

namespace VFEAncients
{
    public abstract class Fail
    {
        public abstract string Label { get; }
        public abstract void RunOnPawn(Pawn pawn);

        public virtual void SendLetter(string letterText, LookTargets targets = null)
        {
            Find.LetterStack.ReceiveLetter("VFEAncients.ExperimentFailed".Translate() + ": " + Label, letterText, LetterDefOf.NegativeEvent, targets);
        }
    }

    public class Fail_Death : Fail
    {
        public override string Label => "VFEAncients.Death".Translate();

        public override void RunOnPawn(Pawn pawn)
        {
            pawn.Kill(null);
            SendLetter("VFEAncients.ExperimentFailed.Death".Translate(), pawn.Corpse);
        }
    }

    public class Fail_Berserk : Fail
    {
        public override string Label => "VFEAncients.Berserk".Translate();

        public override void RunOnPawn(Pawn pawn)
        {
            VFEA_DefOf.Berserk.Worker.TryStart(pawn, "VFEAncients.ExperimentFailed".Translate(), false);
            SendLetter("VFEAncients.ExperimentFailed.Berserk".Translate(), pawn);
        }
    }

    public class Fail_BrainDeath : Fail
    {
        public override string Label => "VFEAncients.BrainDeath".Translate();

        public override void RunOnPawn(Pawn pawn)
        {
            var brain = pawn.health.hediffSet.GetBrain();
            var hediff = HediffMaker.MakeHediff(VFEA_DefOf.ChemicalBurn, pawn, brain);
            hediff.Severity = pawn.health.hediffSet.GetPartHealth(brain) - 1f;
            hediff.TryGetComp<HediffComp_GetsPermanent>().IsPermanent = true;
            pawn.health.AddHediff(hediff, brain);
            SendLetter("VFEAncients.ExperimentFailed.BrainDeath".Translate(), pawn.Corpse);
        }
    }
}