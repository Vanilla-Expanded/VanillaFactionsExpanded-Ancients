using System.Collections.Generic;
using System.Linq;
using System.Reflection.Emit;
using HarmonyLib;
using Verse;

namespace VFEAncients.HarmonyPatches;

public static class MetaMorphPatches
{
    public static void Do(Harmony harm)
    {
        harm.Patch(AccessTools.Method(typeof(Pawn), nameof(Pawn.ExposeData)), postfix: new(typeof(MetaMorphPatches), nameof(SaveMetamorphed)));
        harm.Patch(AccessTools.PropertyGetter(typeof(Pawn), nameof(Pawn.HealthScale)), new(typeof(MetaMorphPatches), nameof(MetaMorphHealth)));
        harm.Patch(AccessTools.PropertyGetter(typeof(Pawn), nameof(Pawn.VerbProperties)), new(typeof(MetaMorphPatches), nameof(MetaMorphAttacks)));
        harm.Patch(AccessTools.Method(typeof(PawnRenderTree), "TrySetupGraphIfNeeded"), transpiler: new(typeof(MetaMorphPatches), nameof(MetaMorphRenderTree)));
        harm.Patch(AccessTools.Method(typeof(PawnRenderTree), "SetupDynamicNodes"), transpiler: new(typeof(MetaMorphPatches), nameof(MetaMorphDynamicNodes)));
    }

    public static void SaveMetamorphed(Pawn __instance)
    {
        var metamorped = HediffComp_MetaMorph.MetamorphedPawns.Contains(__instance);
        Scribe_Values.Look(ref metamorped, "metamorphed");
        if (Scribe.mode == LoadSaveMode.LoadingVars && metamorped) HediffComp_MetaMorph.MetamorphedPawns.Add(__instance);
    }

    public static bool MetaMorphHealth(Pawn __instance, ref float __result)
    {
        if (!HediffComp_MetaMorph.MetamorphedPawns.Contains(__instance)) return true;
        var comp = __instance.health.hediffSet.GetAllComps().OfType<HediffComp_MetaMorph>().First();
        __result = comp.Target.RaceProps.lifeStageAges.Last().def.healthScaleFactor * comp.Target.RaceProps.baseHealthScale;
        return false;
    }


    public static bool MetaMorphAttacks(Pawn __instance, ref List<VerbProperties> __result)
    {
        if (!HediffComp_MetaMorph.MetamorphedPawns.Contains(__instance)) return true;
        var comp = __instance.health.hediffSet.GetAllComps().OfType<HediffComp_MetaMorph>().First();
        __result = comp.Target.race.Verbs;
        return false;
    }

    public static IEnumerable<CodeInstruction> MetaMorphRenderTree(IEnumerable<CodeInstruction> instructions, ILGenerator generator)
    {
        var info = AccessTools.Field(typeof(RaceProperties), nameof(RaceProperties.renderTree));
        foreach (var instruction in instructions)
        {
            yield return instruction;
            if (instruction.LoadsField(info))
            {
                yield return new(OpCodes.Ldsfld, AccessTools.Field(typeof(HediffComp_MetaMorph), nameof(HediffComp_MetaMorph.MetamorphedPawns)));
                yield return new(OpCodes.Ldarg_0);
                yield return new(OpCodes.Ldfld, AccessTools.Field(typeof(PawnRenderTree), nameof(PawnRenderTree.pawn)));
                yield return new(OpCodes.Call, AccessTools.Method(typeof(HashSet<Pawn>), nameof(HashSet<Pawn>.Contains)));
                var label = generator.DefineLabel();
                yield return new(OpCodes.Brfalse, label);
                yield return new(OpCodes.Pop);
                yield return new(OpCodes.Ldsfld, AccessTools.Field(typeof(VFEA_DefOf), nameof(VFEA_DefOf.VFEA_Metamorphed)));
                yield return new CodeInstruction(OpCodes.Nop).WithLabels(label);
            }
        }
    }

    public static IEnumerable<CodeInstruction> MetaMorphDynamicNodes(IEnumerable<CodeInstruction> instructions)
    {
        var info = AccessTools.PropertyGetter(typeof(RaceProperties), nameof(RaceProperties.Humanlike));
        foreach (var instruction in instructions)
        {
            yield return instruction;
            if (instruction.Calls(info))
            {
                yield return new(OpCodes.Ldsfld, AccessTools.Field(typeof(HediffComp_MetaMorph), nameof(HediffComp_MetaMorph.MetamorphedPawns)));
                yield return new(OpCodes.Ldarg_0);
                yield return new(OpCodes.Ldfld, AccessTools.Field(typeof(PawnRenderTree), nameof(PawnRenderTree.pawn)));
                yield return new(OpCodes.Call, AccessTools.Method(typeof(HashSet<Pawn>), nameof(HashSet<Pawn>.Contains)));
                yield return new(OpCodes.Not);
                yield return new(OpCodes.And);
            }
        }
    }
}
