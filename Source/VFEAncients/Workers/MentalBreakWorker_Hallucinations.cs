using Verse;
using Verse.AI;

namespace VFEAncients;

public class MentalBreakWorker_Hallucinations : MentalBreakWorker
{
    public override bool TryStart(Pawn pawn, string reason, bool causedByMood) =>
        pawn?.mindState?.mentalStateHandler?.TryStartMentalState(def.mentalState, reason, true, causedByMood, false, null, true) ?? false;
}
