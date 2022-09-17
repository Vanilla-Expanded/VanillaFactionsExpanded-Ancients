using System.Collections.Generic;
using RimWorld;
using Verse;

namespace VFEAncients
{
    public class ThingSetMaker_Fixed : ThingSetMaker
    {
        public int count = 1;
        public IntRange countRange = IntRange.zero;

        public ThingDef def;

        public override void Generate(ThingSetMakerParams parms, List<Thing> outThings)
        {
            var t = ThingMaker.MakeThing(def, GenStuff.RandomStuffByCommonalityFor(def));
            t.stackCount = countRange.min != 0 && countRange.max != 0 ? countRange.RandomInRange : count;
            outThings.Add(t);
        }

        public override IEnumerable<ThingDef> AllGeneratableThingsDebugSub(ThingSetMakerParams parms) => Gen.YieldSingle(def);
    }
}