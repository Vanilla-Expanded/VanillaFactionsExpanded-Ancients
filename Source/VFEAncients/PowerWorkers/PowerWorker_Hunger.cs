using System;
using HarmonyLib;
using RimWorld;
using Verse;
using VFEAncients.HarmonyPatches;

namespace VFEAncients
{
    public class PowerWorker_Hunger : PowerWorker
    {
        public PowerWorker_Hunger(PowerDef def) : base(def)
        {
        }

        public override void DoPatches(Harmony harm)
        {
            base.DoPatches(harm);
            harm.Patch(AccessTools.PropertyGetter(typeof(Need_Food), nameof(Need_Food.GUIChangeArrow)), new HarmonyMethod(GetType(), nameof(HungerArrow)));
            harm.Patch(AccessTools.Method(typeof(Need_Food), nameof(Need_Food.NeedInterval)), new HarmonyMethod(GetType(), nameof(Interval)));
        }

        public static bool HungerArrow(Need_Food __instance, ref int __result, Pawn ___pawn)
        {
            if (___pawn.HasPower<PowerWorker_Hunger>())
            {
                __result = Math.Abs(__instance.CurLevel - __instance.MaxLevel) < 0.009f ? 0 : 1;
                return false;
            }

            return true;
        }

        public static void Interval(Need_Food __instance, Pawn ___pawn)
        {
            if (___pawn.HasPower<PowerWorker_Hunger>()) __instance.CurLevel += __instance.FoodFallPerTick * 300f;
        }
    }
}