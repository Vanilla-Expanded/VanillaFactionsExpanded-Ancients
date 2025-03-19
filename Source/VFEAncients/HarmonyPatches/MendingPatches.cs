using System;
using System.Collections.Generic;
using System.Linq;
using HarmonyLib;
using RimWorld;
using UnityEngine;
using Verse;

namespace VFEAncients.HarmonyPatches;

public static class MendingPatches
{
    public static void Do(Harmony harm)
    {
        harm.Patch(AccessTools.Method(typeof(WorkGiver_DoBill), "IsUsableIngredient"),
            new HarmonyMethod(typeof(MendingPatches), nameof(IsUsableIngredient_Prefix)));
        harm.Patch(AccessTools.Method(typeof(WorkGiver_DoBill), "TryFindBestBillIngredientsInSet"),
            postfix: new HarmonyMethod(typeof(MendingPatches), nameof(TryFindStuffIngredients)));
        harm.Patch(AccessTools.Method(typeof(GenRecipe), nameof(GenRecipe.MakeRecipeProducts)), new HarmonyMethod(typeof(MendingPatches), nameof(RepairItem)));
        harm.Patch(AccessTools.Method(typeof(Thing), nameof(Thing.SpecialDisplayStats)),
            postfix: new HarmonyMethod(typeof(MendingPatches), nameof(MendingDisplayStats)));
    }


    private static bool IsUsableIngredient_Prefix(Bill bill, Thing t, ref bool __result)
    {
        if (bill.recipe is null || !bill.recipe.HasModExtension<RecipeExtension_Mend>()) return true;
        if (!t.def.useHitPoints || !(t.def.IsWeapon || t.def.IsApparel))
            __result = bill.recipe.ingredients[0].filter.AllowedThingDefs.Any(IsStuffIngredient(t));
        else __result = bill.IsFixedOrAllowedIngredient(t) && t.HitPoints < t.MaxHitPoints;
        return false;
    }

    public static bool RepairItem(RecipeDef recipeDef, List<Thing> ingredients, ref IEnumerable<Thing> __result)
    {
        if (recipeDef.HasModExtension<RecipeExtension_Mend>())
        {
            var item = ingredients.FirstOrDefault(t =>
                t.stackCount == 1 && (t.def.IsWeapon || t.def.IsApparel) && t.def.useHitPoints && t.HitPoints < t.MaxHitPoints);
            if (item != null)
            {
                item.HitPoints = item.MaxHitPoints;
                __result = Gen.YieldSingle(item);
                return false;
            }
        }

        return true;
    }

    public static Func<ThingDef, bool> IsStuffIngredient(Thing t)
    {
        return def => def.MadeFromStuff
            ? t.def.stuffProps?.CanMake(def) ?? false
            : NonStuffStuff(def) == t.def;
    }

    public static ThingDef NonStuffStuff(ThingDef def)
    {
        return def.CostList != null && def.CostList.Count > 0
            ? def.CostListAdjusted(null).MaxBy(tdcc => tdcc.count).thingDef
            : StuffFromTech(def.techLevel);
    }

    public static ThingDef StuffFromTech(TechLevel level)
    {
        switch (level)
        {
            case TechLevel.Animal:
            case TechLevel.Neolithic:
            case TechLevel.Medieval:
                return ThingDefOf.WoodLog;
            case TechLevel.Undefined:
            case TechLevel.Industrial:
                return ThingDefOf.Steel;
            case TechLevel.Spacer:
            case TechLevel.Ultra:
            case TechLevel.Archotech:
                return ThingDefOf.Plasteel;
            default:
                throw new ArgumentOutOfRangeException(nameof(level), level, null);
        }
    }

    public static void TryFindStuffIngredients(List<Thing> availableThings, Bill bill, List<ThingCount> chosen, ref bool __result)
    {
        if (__result && bill.recipe.HasModExtension<RecipeExtension_Mend>())
        {
            var tc = chosen[0];
            if (tc.Count == 1)
            {
                var item = tc.Thing;
                var chosenDef = item.Stuff ?? NonStuffStuff(item.def);
                var countWanted = Mathf.RoundToInt(Mathf.Clamp((item.Stuff != null
                                                                   ? item.def.CostStuffCount
                                                                   : item.def.CostList != null && item.def.CostList.Any()
                                                                       ? item.CostListAdjusted().First(tdcc => tdcc.thingDef == chosenDef).count
                                                                       : item.GetStatValue(StatDefOf.MarketValueIgnoreHp) / 100) *
                                                               bill.recipe.GetModExtension<RecipeExtension_Mend>().Fraction, 1f, 9999f));

                foreach (var thing in availableThings.Where(thing => thing.def == chosenDef))
                {
                    chosen.Add(new ThingCount(thing, countWanted));
                    countWanted -= thing.stackCount;
                    if (countWanted <= 0) return;
                }

                __result = false;
            }
        }
    }

    public static IEnumerable<StatDrawEntry> MendingDisplayStats(IEnumerable<StatDrawEntry> stats, Thing __instance)
    {
        foreach (var stat in stats) yield return stat;

        var t = __instance;
        if (t.stackCount == 1 && (t.def.IsWeapon || t.def.IsApparel) && t.def.useHitPoints)
        {
            var repair = t.def.MadeFromStuff ? t.Stuff : NonStuffStuff(t.def);
            yield return new StatDrawEntry(StatCategoryDefOf.BasicsImportant, "VFEAncients.RepairMat".Translate(), repair.LabelCap,
                "VFEAncients.MatForRepair".Translate(),
                1000,
                hyperlinks: Gen.YieldSingle(new Dialog_InfoCard.Hyperlink(repair)));
        }
    }
}