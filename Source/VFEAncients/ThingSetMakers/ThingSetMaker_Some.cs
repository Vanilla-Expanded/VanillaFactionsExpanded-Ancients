using System.Collections.Generic;
using System.Linq;
using RimWorld;
using Verse;

namespace VFEAncients
{
    public class ThingSetMaker_Some : ThingSetMaker
    {
        public List<ThingSetMaker> choices;
        public int count;

        public override void ResolveReferences()
        {
            base.ResolveReferences();
            foreach (var choice in choices) choice.ResolveReferences();
        }

        public override void Generate(ThingSetMakerParams parms, List<Thing> outThings)
        {
            outThings.AddRange(choices.InRandomOrder().Take(count).SelectMany(choice => choice.Generate(parms)));
        }

        public override IEnumerable<ThingDef> AllGeneratableThingsDebugSub(ThingSetMakerParams parms)
        {
            return choices.SelectMany(choice => choice.AllGeneratableThingsDebug(parms));
        }
    }
}