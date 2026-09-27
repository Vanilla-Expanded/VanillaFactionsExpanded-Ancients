using System;
using System.Collections.Generic;
using System.Linq;
using HarmonyLib;
using RimWorld;
using UnityEngine;
using Verse;
using Verse.AI;

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
        harm.Patch(AccessTools.Method(typeof(Toils_Recipe), nameof(Toils_Recipe.DoRecipeWork)),
            postfix: new HarmonyMethod(typeof(MendingPatches), nameof(ScaleWorkToDamage)));
    }

    // A mending bill costs the recipe's flat workAmount however little is wrong with the item.
    // RecipeExtension_Mend has carried a WorkPerHP field since 1.3, marked TODO and never read: a
    // weapon one scratch from mint costs the same bill as one hit from breaking. This charges it -
    // work is the missing hit points times WorkPerHP, so the old flat workAmount becomes the price
    // of a fully ruined item. A recipe that leaves WorkPerHP at its default of 0 is untouched.
    //
    // The work is set on the toil rather than on the recipe because only here is the actual item
    // known. DoRecipeWork builds its Toil fresh per job and hands it back, so wrapping initAction
    // is enough here - no transpiler, and nothing that depends on the compiler's closure shape.
    public static void ScaleWorkToDamage(Toil __result)
    {
        var toil = __result;
        var inner = toil.initAction;
        toil.initAction = delegate
        {
            inner?.Invoke();

            var job = toil.actor?.jobs?.curJob;
            var ext = job?.bill?.recipe?.GetModExtension<RecipeExtension_Mend>();
            // Every other recipe in the game reaches this delegate too, and leaves it here.
            if (ext == null || ext.WorkPerHP <= 0f) return;
            if (!(toil.actor.jobs.curDriver is JobDriver_DoBill driver)) return;
            if (job.placedThings == null) return;

            foreach (var placed in job.placedThings)
            {
                var item = placed?.thing;
                if (item == null || item.stackCount != 1) continue;
                if (!item.def.useHitPoints || !(item.def.IsWeapon || item.def.IsApparel)) continue;
                if (item.HitPoints >= item.MaxHitPoints) continue;

                driver.workLeft = Mathf.Max(1f, (item.MaxHitPoints - item.HitPoints) * ext.WorkPerHP);
                return;
            }
        };
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