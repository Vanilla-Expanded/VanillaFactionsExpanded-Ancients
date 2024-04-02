using System.Collections.Generic;
using HarmonyLib;
using HeavyWeapons;
using RimWorld;
using Verse;
using VFEAncients.HarmonyPatches;

namespace VFEAncients;

public class PowerWorker_Strong : PowerWorker
{
    public PowerWorker_Strong(PowerDef def) : base(def) { }

    public override void DoPatches(Harmony harm)
    {
        base.DoPatches(harm);
        harm.Patch(AccessTools.Method(typeof(Verb_MeleeAttackDamage), "DamageInfosToApply"), postfix: new(GetType(), nameof(AddDamage)));
        harm.Patch(AccessTools.Method(typeof(Patch_FloatMenuMakerMap.AddHumanlikeOrders_Fix),
            nameof(Patch_FloatMenuMakerMap.AddHumanlikeOrders_Fix.CanEquip)), new(GetType(), nameof(ForceCanEquip)));
    }

    public static IEnumerable<DamageInfo> AddDamage(IEnumerable<DamageInfo> dinfos, Verb_MeleeAttackDamage __instance)
    {
        var isStrong = __instance.Caster.HasPower<PowerWorker_Strong>();
        foreach (var dinfo in dinfos)
        {
            if (isStrong) dinfo.SetAmount(dinfo.Amount * 2);
            yield return dinfo;
        }
    }

    public static bool ForceCanEquip(Pawn pawn, ref bool __result)
    {
        if (!pawn.HasPower<PowerWorker_Strong>()) return true;
        __result = true;
        return false;
    }
}
