using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using System.Text;
using HarmonyLib;
using RimWorld;
using Verse;

namespace VFEAncients.HarmonyPatches;

public static class PowerPatches
{
    private static readonly FieldInfo permanentOnly =
        AccessTools.Field(AccessTools.FirstInner(typeof(Pawn), type => AccessTools.Field(type, "permanentOnly") is not null), "permanentOnly");

    public new static Type GetType() => typeof(PowerPatches);

    public static void Do(Harmony harm)
    {
        harm.Patch(AccessTools.Method(typeof(Pawn), nameof(Pawn.ExposeData)),
            postfix: new HarmonyMethod(typeof(Pawn_PowerTracker), nameof(Pawn_PowerTracker.Save)));
        harm.Patch(AccessTools.Method(typeof(StatWorker), nameof(StatWorker.GetValueUnfinalized)),
            transpiler: new HarmonyMethod(GetType(), nameof(StatGetValueTranspile)));
        harm.Patch(AccessTools.Method(typeof(StatWorker), nameof(StatWorker.GetExplanationUnfinalized)),
            transpiler: new HarmonyMethod(GetType(), nameof(StatExplanationTranspile)));
        harm.Patch(AccessTools.Method(typeof(Pawn_InteractionsTracker), "TryInteractRandomly"),
            transpiler: new HarmonyMethod(typeof(PowerPatches), nameof(ForceInteraction)));
        harm.Patch(AccessTools.Method(typeof(VerbProperties), nameof(VerbProperties.AdjustedCooldown), new[] { typeof(Tool), typeof(Pawn), typeof(Thing) }),
            postfix: new HarmonyMethod(GetType(), nameof(ApplyStat)));
        harm.Patch(
            AccessTools.Method(typeof(VerbProperties), nameof(VerbProperties.AdjustedCooldown),
                new[] { typeof(Tool), typeof(Pawn), typeof(ThingDef), typeof(ThingDef) }),
            postfix: new HarmonyMethod(GetType(), nameof(ApplyStat)));
        harm.Patch(AccessTools.Method(typeof(PawnGenerator), "TryGenerateNewPawnInternal"),
            postfix: new HarmonyMethod(typeof(PowerPatches), nameof(AddPowers)));
        harm.Patch(AccessTools.Method(typeof(StatWorker), nameof(StatWorker.FinalizeValue)),
            postfix: new HarmonyMethod(GetType(), nameof(SetStat)));
        harm.Patch(AccessTools.Method(typeof(StatWorker), nameof(StatWorker.GetExplanationFull)),
            new HarmonyMethod(GetType(), nameof(SetStatExplain)));
        harm.Patch(AccessTools.Method(typeof(ThoughtUtility), nameof(ThoughtUtility.ThoughtNullified)),
            postfix: new HarmonyMethod(GetType(), nameof(ThoughtNullified_Postfix)));
        harm.Patch(AccessTools.Method(typeof(ThoughtUtility), nameof(ThoughtUtility.ThoughtNullifiedMessage)),
            postfix: new HarmonyMethod(GetType(), nameof(ThoughtNullifiedMessage_Postfix)));
        harm.Patch(AccessTools.PropertyGetter(typeof(Pawn), nameof(Pawn.CombinedDisabledWorkTags)),
            postfix: new HarmonyMethod(GetType(), nameof(DisableWork)));
        harm.Patch(AccessTools.Method(typeof(CharacterCardUtility), "GetWorkTypeDisableCauses"),
            postfix: new HarmonyMethod(GetType(), nameof(AddCauseDisable)));
        harm.Patch(AccessTools.Method(typeof(CharacterCardUtility), "GetWorkTypeDisabledCausedBy"),
            transpiler: new HarmonyMethod(GetType(), nameof(AddCauseDisableExplain)));
        harm.Patch(AccessTools.GetDeclaredMethods(typeof(Pawn)).First(info => info.Name.Contains("GetDisabledWorkTypes") && info.Name.Contains("FillList")),
            postfix: new HarmonyMethod(GetType(), nameof(AddDisabledWorkTypes)));
        harm.Patch(AccessTools.Method(typeof(MoveColonyUtility), "RemoveHediffs"), postfix: new HarmonyMethod(typeof(PowerPatches), nameof(RemovePowers)));
    }

    public static void AddPowers(Pawn __result, PawnGenerationRequest request)
    {
        if (request.KindDef != null && !request.AllowedDevelopmentalStages.Newborn()
                                    && request.KindDef.TryGetModExtension<PawnKindExtension_Powers>(out var ext) &&
                                       __result?.GetPowerTracker() is { } tracker)
        {
            if (ext.forcePowers != null)
                foreach (var power in ext.forcePowers)
                    tracker.AddPower(power);

            if (ext.numRandomSuperpowers > 0)
                for (var i = 0; i < ext.numRandomSuperpowers; i++)
                    tracker.AddPower(DefDatabase<PowerDef>.AllDefs.Where(power => power.powerType == PowerType.Superpower).RandomElement());

            if (ext.numRandomWeaknesses > 0)
                for (var i = 0; i < ext.numRandomWeaknesses; i++)
                    tracker.AddPower(DefDatabase<PowerDef>.AllDefs.Where(power => power.powerType == PowerType.Weakness).RandomElement());
        }
    }

    public static void ApplyStat(ref float __result, Pawn attacker, VerbProperties __instance)
    {
        if (attacker != null && __instance.IsMeleeAttack) __result *= attacker.GetStatValue(VFEA_DefOf.VFEAncients_MeleeCooldownFactor,cacheStaleAfterTicks:250);
    }

    public static IEnumerable<CodeInstruction> ForceInteraction(IEnumerable<CodeInstruction> instructions, ILGenerator generator)
    {
        var list = instructions.ToList();
        var idx1 = list.FindIndex(ins => ins.opcode == OpCodes.Call && ins.operand is MethodInfo { Name: "TryRandomElementByWeight" }) + 2;
        var label1 = generator.DefineLabel();
        var label2 = generator.DefineLabel();
        list[idx1].labels.Add(label1);
        list.InsertRange(idx1, new[]
        {
            new CodeInstruction(OpCodes.Ldarg_0),
            new CodeInstruction(OpCodes.Ldfld, AccessTools.Field(typeof(Pawn_InteractionsTracker), "pawn")),
            new CodeInstruction(OpCodes.Ldsfld, AccessTools.Field(typeof(VFEA_DefOf), nameof(VFEA_DefOf.Lustful))),
            new CodeInstruction(OpCodes.Call, Helpers.HasPowerDef),
            new CodeInstruction(OpCodes.Brfalse, label2),
            new CodeInstruction(OpCodes.Ldsfld, AccessTools.Field(typeof(VFEA_DefOf), nameof(VFEA_DefOf.VFEA_RomanceAttempt_Lustful))),
            new CodeInstruction(OpCodes.Stloc, 4),
            new CodeInstruction(OpCodes.Ldarg_0).WithLabels(label2),
            new CodeInstruction(OpCodes.Ldfld, AccessTools.Field(typeof(Pawn_InteractionsTracker), "pawn")),
            new CodeInstruction(OpCodes.Ldsfld, AccessTools.Field(typeof(VFEA_DefOf), nameof(VFEA_DefOf.Celebrity))),
            new CodeInstruction(OpCodes.Call, Helpers.HasPowerDef),
            new CodeInstruction(OpCodes.Brfalse, label1),
            new CodeInstruction(OpCodes.Ldsfld, AccessTools.Field(typeof(VFEA_DefOf), nameof(VFEA_DefOf.KindWords))),
            new CodeInstruction(OpCodes.Stloc, 4)
        });
        return list;
    }

    public static IEnumerable<CodeInstruction> StatGetValueTranspile(IEnumerable<CodeInstruction> instructions, ILGenerator generator)
    {
        var list = instructions.ToList();
        var info1 = AccessTools.Field(typeof(Pawn), nameof(Pawn.ageTracker));
        var idx1 = list.FindIndex(ins => ins.LoadsField(info1));
        var idx2 = list.FindIndex(idx1, ins => ins.opcode == OpCodes.Stloc_0);
        list.InsertRange(idx2 + 1, new[]
        {
            new CodeInstruction(OpCodes.Ldloc_1),
            new CodeInstruction(OpCodes.Ldloca_S, 0),
            new CodeInstruction(OpCodes.Ldarg_0),
            new CodeInstruction(OpCodes.Ldfld, AccessTools.Field(typeof(StatWorker), "stat")),
            new CodeInstruction(OpCodes.Call, AccessTools.Method(typeof(PowerPatches), nameof(PowerModifyStat)))
        });
        return list;
    }

    public static void PowerModifyStat(Pawn pawn, ref float val, StatDef stat)
    {
        if (PowerWorker.TouchStat(stat) && pawn.GetPowerTracker() is { } tracker)
            foreach (var power in tracker.AllPowers)
            {
                if (power.statFactors != null) val *= power.statFactors.GetStatFactorFromList(stat);
                if (power.statOffsets != null) val += power.statOffsets.GetStatOffsetFromList(stat);
            }
    }

    public static IEnumerable<CodeInstruction> StatExplanationTranspile(IEnumerable<CodeInstruction> instructions, ILGenerator generator)
    {
        var list = instructions.ToList();
        var info1 = AccessTools.Field(typeof(Pawn), nameof(Pawn.ageTracker));
        var idx1 = list.FindIndex(ins => ins.LoadsField(info1));
        var label1 = (Label)list[list.FindIndex(idx1, ins => ins.opcode == OpCodes.Beq_S)].operand;
        var idx2 = list.FindIndex(idx1, ins => ins.opcode == OpCodes.Pop);
        list[idx2 + 1].labels.Remove(label1);
        list.InsertRange(idx2 + 1, new[]
        {
            new CodeInstruction(OpCodes.Ldloc_2).WithLabels(label1),
            new CodeInstruction(OpCodes.Ldloc_0),
            new CodeInstruction(OpCodes.Ldarg_0),
            new CodeInstruction(OpCodes.Ldfld, AccessTools.Field(typeof(StatWorker), "stat")),
            new CodeInstruction(OpCodes.Ldarg_0),
            new CodeInstruction(OpCodes.Call, AccessTools.Method(typeof(PowerPatches), nameof(PowerModifyExplanation)))
        });
        return list;
    }

    public static void PowerModifyExplanation(Pawn pawn, StringBuilder builder, StatDef stat, StatWorker worker)
    {
        if (PowerWorker.TouchStat(stat) && pawn.GetPowerTracker() is { } tracker)
            foreach (var power in tracker.AllPowers)
            {
                if (power.statFactors != null)
                {
                    var factor = power.statFactors.GetStatFactorFromList(stat);
                    if (Math.Abs(factor - 1f) > 0.0001f)
                        builder.AppendLine(power.LabelCap + ": " + worker.ValueToString(factor, false, ToStringNumberSense.Factor));
                }

                if (power.statOffsets != null)
                {
                    var offset = power.statOffsets.GetStatOffsetFromList(stat);
                    if (offset != 0f)
                        builder.AppendLine(power.LabelCap + ": " + worker.ValueToString(offset, false, ToStringNumberSense.Offset));
                }
            }
    }

    public static void SetStat(StatRequest req, ref float val, StatWorker __instance)
    {
        if (req.Thing is Pawn pawn && PowerWorker.TouchStat(__instance.stat) && pawn.GetPowerTracker() is { } tracker)
            foreach (var power in tracker.AllPowers)
            {
                var value = power.setStats.GetStatValueFromList(__instance.stat, float.NaN);
                if (power.setStats != null && !float.IsNaN(value))
                    val = power.setStats.GetStatValueFromList(__instance.stat, __instance.stat.defaultBaseValue);
            }
    }

    public static bool SetStatExplain(StatRequest req, ToStringNumberSense numberSense, StatWorker __instance, ref string __result)
    {
        if (req.Thing is Pawn pawn && PowerWorker.TouchStat(__instance.stat) && pawn.GetPowerTracker() is { } tracker)
            foreach (var power in tracker.AllPowers)
                if (power.setStats != null)
                {
                    var set = power.setStats.GetStatValueFromList(__instance.stat, float.NaN);
                    if (!float.IsNaN(set))
                    {
                        __result = "StatsReport_FinalValue".Translate() + ": " + power.LabelCap + ": " +
                                   __instance.stat.ValueToString(set, numberSense);
                        return false;
                    }
                }

        return true;
    }

    public static void AddPowerExplain(StringBuilder builder, object power)
    {
        builder.AppendLine("VFEAncients.IncapableOfTooltipPower".Translate(((PowerDef)power).label));
    }

    public static void AddCauseDisable(Pawn pawn, WorkTags workTag, ref List<object> __result)
    {
        if (pawn.GetPowerTracker() is { } tracker) __result.AddRange(tracker.AllPowers.Where(power => (power.disabledWorkTags & workTag) != WorkTags.None));
    }

    public static void DisableWork(Pawn __instance, ref WorkTags __result)
    {
        if (__instance.GetPowerTracker() is { } tracker)
            __result = tracker.AllPowers.Aggregate(__result, (current, power) => current | power.disabledWorkTags);
    }

    public static void AddDisabledWorkTypes(Pawn __instance, List<WorkTypeDef> list, object __1)
    {
        if (__instance.GetPowerTracker() is { } tracker && !(bool)permanentOnly.GetValue(__1))
            foreach (var power in tracker.AllPowers)
            foreach (var workType in power.Worker.DisabledWorkTypes.Except(list))
                list.Add(workType);
    }

    public static void ThoughtNullified_Postfix(Pawn pawn, ThoughtDef def, ref bool __result)
    {
        if (!__result && pawn != null && PowerDef.GlobalNullifiedThoughts.Contains(def) &&
            (pawn.GetPowerTracker()?.AllNullifiedThoughts.Contains(def) ?? false)) __result = true;
    }

    public static void ThoughtNullifiedMessage_Postfix(Pawn pawn, ThoughtDef def, ref string __result)
    {
        if (__result.NullOrEmpty() && !ThoughtUtility.NeverNullified(def, pawn))
        {
            var power = NullifyingPower(def, pawn);
            if (power != null) __result = "ThoughtNullifiedBy".Translate().CapitalizeFirst() + ": " + power.LabelCap;
        }
    }

    public static PowerDef NullifyingPower(ThoughtDef def, Pawn pawn)
    {
        return pawn.GetPowerTracker()?.AllPowers.FirstOrDefault(power => power.nullifiedThoughts?.Contains(def) ?? false);
    }

    public static IEnumerable<CodeInstruction> AddCauseDisableExplain(IEnumerable<CodeInstruction> instructions, ILGenerator generator)
    {
        var list = instructions.ToList();
        var idx1 = list.FindIndex(ins => ins.opcode == OpCodes.Ldloca_S);
        var idx2 = list.FindIndex(idx1 + 1, ins => ins.opcode == OpCodes.Ldloca_S);
        var idx3 = list.FindLastIndex(ins => ins.opcode == OpCodes.Brfalse_S);
        var label1 = generator.DefineLabel();
        var label2 = list[idx3].operand;
        list[idx3].operand = label1;
        list.InsertRange(idx2, new[]
        {
            new CodeInstruction(OpCodes.Br, label2),
            new CodeInstruction(OpCodes.Ldloc_2).WithLabels(label1),
            new CodeInstruction(OpCodes.Isinst, typeof(PowerDef)),
            new CodeInstruction(OpCodes.Brfalse, label2),
            new CodeInstruction(OpCodes.Ldloc_0),
            new CodeInstruction(OpCodes.Ldloc_2),
            new CodeInstruction(OpCodes.Call, AccessTools.Method(typeof(PowerPatches), nameof(AddPowerExplain)))
        });
        return list;
    }

    public static void RemovePowers(List<Pawn> pawns)
    {
        foreach (var pawn in pawns)
            if (pawn.GetPowerTracker() is { } tracker)
            {
                var powers = tracker.AllPowers.ToList();
                foreach (var power in powers) tracker.RemovePower(power);
            }
    }
}
