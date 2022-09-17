using RimWorld;
using RimWorld.Planet;
using Verse;

namespace VFEAncients
{
    public class Ability_Animal : Ability
    {
        public override bool ValidateTarget(LocalTargetInfo target, bool showMessages = true)
        {
            if (!base.ValidateTarget(target, showMessages)) return false;
            if (target.Pawn == null) return false;
            if (!target.Pawn.AnimalOrWildMan())
            {
                if (showMessages) Messages.Message("VFEAncients.NotAnimal".Translate(), MessageTypeDefOf.RejectInput);
                return false;
            }

            if (target.Pawn.Faction == pawn.Faction)
            {
                if (showMessages) Messages.Message("VFEAncients.Tame".Translate(), MessageTypeDefOf.RejectInput);
                return false;
            }

            return true;
        }


        public override void Cast(params GlobalTargetInfo[] targets)
        {
            base.Cast(targets);
            foreach (var target in targets)
            {
                if (target.Thing is not Pawn p) continue;
                if (!p.AnimalOrWildMan()) continue;
                if (p.MentalState != null &&
                    (p.MentalState.def == MentalStateDefOf.Manhunter || p.MentalState.def == MentalStateDefOf.ManhunterPermanent))
                    p.MentalState.RecoverFromState();
                else
                    InteractionWorker_RecruitAttempt.DoRecruit(pawn, p);
            }
        }
    }
}