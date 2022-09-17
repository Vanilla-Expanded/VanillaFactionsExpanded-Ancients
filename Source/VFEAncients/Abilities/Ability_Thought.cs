using RimWorld;
using RimWorld.Planet;
using Verse;

namespace VFEAncients
{
    public class Ability_Thought : Ability
    {
        public override void Cast(params GlobalTargetInfo[] targets)
        {
            base.Cast(targets);
            if (def.TryGetModExtension<AbilityExtension_Thought>(out var ext))
            {
                if (ext.casterThought != null) pawn.needs.mood?.thoughts.memories.TryGainMemoryFast(ext.casterThought);
                if (ext.targetThought != null)
                    foreach (var target in targets)
                        (target.Thing as Pawn)?.needs.mood?.thoughts.memories.TryGainMemoryFast(ext.targetThought);
            }
        }
    }

    public class AbilityExtension_Thought : DefModExtension
    {
        public ThoughtDef casterThought;
        public ThoughtDef targetThought;
    }
}