using Verse;
using Verse.Sound;

namespace VFEAncients
{
    public class HediffComp_SoundOnRemove : HediffComp
    {
        public override void CompPostPostRemoved()
        {
            base.CompPostPostRemoved();
            if (props is HediffCompProperties_SoundOnRemove {sound: var sound}) sound.PlayOneShot(parent.pawn);
        }
    }

    public class HediffCompProperties_SoundOnRemove : HediffCompProperties
    {
        public SoundDef sound;

        public HediffCompProperties_SoundOnRemove() => compClass = typeof(HediffComp_SoundOnRemove);
    }
}