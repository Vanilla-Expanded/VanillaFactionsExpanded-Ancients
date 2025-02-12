using HarmonyLib;
using RimWorld;
using RimWorld.Planet;
using System;
using RimWorld.QuestGen;
using Verse;

namespace VFEAncients.HarmonyPatches
{
    public static class QuestPatches
    {
        public new static Type GetType() => typeof(QuestPatches);

        public static void Do(Harmony harm)
        {
            harm.Patch(AccessTools.Method(typeof(SiteMakerHelper), nameof(SiteMakerHelper.FactionCanOwn),
            [
                typeof(SitePartDef), typeof(Faction), typeof(bool), typeof(Predicate<Faction>)
            ]),
            prefix: new HarmonyMethod(GetType(), nameof(FactionCanOwnPrefix)));
            harm.Patch(AccessTools.Method(typeof(QuestNode_Root_DistressCall), nameof(QuestNode_Root_DistressCall.FactionUsable)),
                prefix: new HarmonyMethod(FactionUsableForDistressCallPrefix));
        }

        public static bool FactionCanOwnPrefix(SitePartDef sitePart, Faction faction, ref bool __result)
        {
            if (faction != null && faction.def == VFEA_DefOf.VFEA_AncientSoldiers && sitePart.defName.StartsWith("VFEA_"))
            {
                __result = true;
                return false;
            }
            return true;
        }

        public static bool FactionUsableForDistressCallPrefix(Faction f, ref bool __result)
        {
            if (f != null && f.def == VFEA_DefOf.VFEA_AncientSoldiers)
            {
                __result = false;
                return false;
            }
            return true;
        }
    }
}