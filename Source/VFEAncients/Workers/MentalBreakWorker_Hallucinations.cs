using Verse;
using Verse.AI;

namespace VFEAncients
{
    public class MentalBreakWorker_Hallucinations : MentalBreakWorker
    {
        public override bool TryStart(Pawn pawn, string reason, bool causedByMood)
        {
            return pawn?.mindState?.mentalStateHandler?.TryStartMentalState(def.mentalState, reason, true, causedByMood, null, true) ?? false;
        }
    }
}