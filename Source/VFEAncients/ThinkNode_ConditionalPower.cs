using Verse;
using Verse.AI;

namespace VFEAncients
{
    public class ThinkNode_ConditionalPower : ThinkNode_Conditional
    {
        public PowerDef Power;

        public override bool Satisfied(Pawn pawn) => pawn?.GetPowerTracker()?.HasPower(Power) ?? false;
    }
}