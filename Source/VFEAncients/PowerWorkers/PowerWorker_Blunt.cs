using HarmonyLib;
using RimWorld;
using Verse;
using VFEAncients.HarmonyPatches;

namespace VFEAncients
{
    public class PowerWorker_Blunt : PowerWorker
    {
        public PowerWorker_Blunt(PowerDef def) : base(def)
        {
        }

        public override void DoPatches(Harmony harm)
        {
            base.DoPatches(harm);
            harm.Patch(AccessTools.Method(typeof(Pawn), nameof(Pawn.PreApplyDamage)), postfix: new HarmonyMethod(GetType(), nameof(SurfaceOnly)));
            harm.Patch(AccessTools.Method(typeof(ArmorUtility), nameof(ArmorUtility.GetPostArmorDamage)), postfix: new HarmonyMethod(GetType(), nameof(ChangeType)));
        }

        public static void SurfaceOnly(Pawn __instance, ref DamageInfo dinfo)
        {
            if (dinfo.Def != DamageDefOf.Blunt && dinfo.Def.armorCategory == DamageArmorCategoryDefOf.Sharp && __instance.HasPower<PowerWorker_Blunt>())
            {
                dinfo.SetBodyRegion(depth: BodyPartDepth.Outside);
                dinfo.SetAllowDamagePropagation(false);
            }
        }

        public static void ChangeType(Pawn pawn, ref DamageDef damageDef)
        {
            if (damageDef != DamageDefOf.Blunt && damageDef.armorCategory == DamageArmorCategoryDefOf.Sharp && pawn.HasPower<PowerWorker_Blunt>()) damageDef = DamageDefOf.Blunt;
        }
    }
}