using System.Collections.Generic;
using RimWorld;
using Verse;

namespace VFEAncients
{
    public class InteractionWorker_RomanceAttemptLustful : InteractionWorker
    {
        public override float RandomSelectionWeight(Pawn initiator, Pawn recipient)
        {
            return initiator.GetPowerTracker()?.HasPower(VFEA_DefOf.Lustful) ?? false ? 100f : 0f;
        }

        public override void Interacted(Pawn initiator, Pawn recipient, List<RulePackDef> extraSentencePacks, out string letterText, out string letterLabel,
            out LetterDef letterDef, out LookTargets lookTargets)
        {
            initiator.needs.mood?.thoughts.memories.TryGainMemory(ThoughtDefOf.RebuffedMyRomanceAttempt, recipient);

            recipient.needs.mood?.thoughts.memories.TryGainMemory(ThoughtDefOf.FailedRomanceAttemptOnMe, initiator);

            if (recipient.relations.OpinionOf(initiator) <= 0)
                recipient.needs.mood?.thoughts.memories.TryGainMemory(ThoughtDefOf.FailedRomanceAttemptOnMeLowOpinionMood, initiator);

            extraSentencePacks.Add(RulePackDefOf.Sentence_RomanceAttemptRejected);
            letterText = null;
            letterLabel = null;
            letterDef = null;
            lookTargets = null;
        }
    }
}