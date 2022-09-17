using HarmonyLib;
using RimWorld;
using Verse;
using VFEAncients.HarmonyPatches;

namespace VFEAncients
{
    public class PowerWorker_MeleeSkill : PowerWorker
    {
        public PowerWorker_MeleeSkill(PowerDef def) : base(def)
        {
        }

        public override void DoPatches(Harmony harm)
        {
            base.DoPatches(harm);
            harm.Patch(AccessTools.Method(typeof(Verb_MeleeAttack), "GetNonMissChance"), new HarmonyMethod(GetType(), nameof(ForceHit)));
            harm.Patch(AccessTools.Method(typeof(Verb_MeleeAttack), "GetDodgeChance"), new HarmonyMethod(GetType(), nameof(ForceDodge)));
        }

        public static bool ForceHit(Verb_MeleeAttack __instance, ref float __result)
        {
            if (__instance.Caster.HasPower<PowerWorker_MeleeSkill>())
            {
                __result = 1f;
                return false;
            }

            return true;
        }

        public static bool ForceDodge(LocalTargetInfo target, Verb_MeleeAttack __instance, ref float __result)
        {
            if (target.Thing.HasPower<PowerWorker_MeleeSkill>())
            {
                __result = 1f;
                return false;
            }

            if (__instance.Caster.HasPower<PowerWorker_MeleeSkill>())
            {
                __result = 0f;
                return false;
            }

            return true;
        }
    }
}