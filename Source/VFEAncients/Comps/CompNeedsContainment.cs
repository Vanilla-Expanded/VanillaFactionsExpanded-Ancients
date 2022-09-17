using System.Collections.Generic;
using RimWorld;
using Verse;

namespace VFEAncients
{
    public class CompNeedsContainment : ThingComp
    {
        public bool ShouldDeteriorate =>
            !(parent.StoringThing() is Thing t && Props.validContainers.Contains(t.def) && (!t.TryGetComp<CompPowerTrader>(out var comp) || comp.PowerOn));

        public CompProperties_NeedsContainment Props => props as CompProperties_NeedsContainment;
    }

    public class CompProperties_NeedsContainment : CompProperties
    {
        public List<ThingDef> validContainers;

        public CompProperties_NeedsContainment()
        {
            compClass = typeof(CompNeedsContainment);
        }
    }
}