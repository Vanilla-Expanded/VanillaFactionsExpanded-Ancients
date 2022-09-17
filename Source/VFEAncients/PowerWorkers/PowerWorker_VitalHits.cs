using System.Linq;
using HarmonyLib;
using Verse;
using VFEAncients.HarmonyPatches;

namespace VFEAncients
{
    public class PowerWorker_VitalHits : PowerWorker
    {
        public PowerWorker_VitalHits(PowerDef def) : base(def)
        {
        }

        public override void DoPatches(Harmony harm)
        {
            base.DoPatches(harm);
            foreach (var info in typeof(DamageWorker_AddInjury).AllSubclassesNonAbstract().Select(type => AccessTools.Method(type, "ChooseHitPart"))
                .Append(AccessTools.Method(typeof(DamageWorker_AddInjury), "ChooseHitPart")).Where(info => info != null && info.IsDeclaredMember()))
                harm.Patch(info, new HarmonyMethod(GetType(), nameof(ChooseHitPart_Prefix)));
            if (VFEAncientsMod.YayosCombat)
                harm.Patch(AccessTools.Method(AccessTools.TypeByName("yayoCombat.patch_DamageWorker_AddInjury"), "ChooseHitPart"),
                    new HarmonyMethod(GetType(), nameof(ChooseHitPart_Prefix)));
        }

        public static bool ChooseHitPart_Prefix(DamageInfo dinfo, Pawn pawn, ref BodyPartRecord __result)
        {
            if (dinfo.Instigator.HasPower<PowerWorker_VitalHits>())
            {
                var vitalParts = pawn.health.hediffSet.GetNotMissingParts(dinfo.Height, BodyPartDepth.Inside).Where(part => part.def.tags.Any(tag => tag.vital)).ToList();
                if (vitalParts.TryRandomElementByWeight(x => x.coverageAbs * x.def.GetHitChanceFactorFor(dinfo.Def), out __result)) return false;
                if (vitalParts.TryRandomElementByWeight(x => x.coverageAbs, out __result)) return false;
            }

            return true;
        }
    }
}