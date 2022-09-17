using Verse;
using Verse.AI;
using VFEAncients.HarmonyPatches;

namespace VFEAncients
{
    public class PowerWorker_Tantrum : PowerWorker
    {
        public PowerWorker_Tantrum(PowerDef def) : base(def)
        {
        }

        public static void CommonalityIncrease(MentalBreakWorker __instance, Pawn pawn, ref float __result)
        {
            if (__instance.def.defName.Contains("Tantrum") && pawn.HasPower<PowerWorker_Tantrum>()) __result *= 5f;
        }
    }
}