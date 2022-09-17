using HarmonyLib;
using RimWorld;
using Verse;
using VFEAncients.HarmonyPatches;

namespace VFEAncients
{
    public class PowerWorker_RecreationDrop : PowerWorker
    {
        public PowerWorker_RecreationDrop(PowerDef def) : base(def)
        {
        }

        public override void DoPatches(Harmony harm)
        {
            base.DoPatches(harm);
            harm.Patch(AccessTools.PropertyGetter(typeof(Need_Joy), "FallPerInterval"), postfix: new HarmonyMethod(GetType(), nameof(Double)));
        }

        public static void Double(ref float __result, Pawn ___pawn)
        {
            if (___pawn.HasPower<PowerWorker_RecreationDrop>()) __result *= 2f;
        }
    }
}