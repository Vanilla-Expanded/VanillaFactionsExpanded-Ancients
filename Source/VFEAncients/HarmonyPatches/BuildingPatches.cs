using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Emit;
using HarmonyLib;
using RimWorld;
using UnityEngine;
using Verse;
using Verse.AI;

namespace VFEAncients.HarmonyPatches;

public static class BuildingPatches
{
    public static void Do(Harmony harm)
    {
        harm.Patch(AccessTools.Method(typeof(SteadyEnvironmentEffects), nameof(SteadyEnvironmentEffects.FinalDeteriorationRate),
                new[] { typeof(Thing), typeof(bool), typeof(bool), typeof(TerrainDef), typeof(List<string>) }),
            postfix: new HarmonyMethod(typeof(BuildingPatches), nameof(AddDeterioration)));
        harm.Patch(AccessTools.Method(typeof(JobDriver_Hack), "MakeNewToils"), postfix: new HarmonyMethod(typeof(BuildingPatches), nameof(FixHacking)));
        harm.Patch(AccessTools.Method(typeof(PowerNet), nameof(PowerNet.PowerNetTick)),
            transpiler: new HarmonyMethod(typeof(BuildingPatches), nameof(PowerNetOnSolarFlareTranspiler)),
            postfix: new HarmonyMethod(typeof(BuildingPatches), nameof(PowerNetOnSolarFlarePostfix)));
    }

    public static IEnumerable<CodeInstruction> PowerNetOnSolarFlareTranspiler(IEnumerable<CodeInstruction> codeInstructions)
    {
        var get_PowerOn = AccessTools.Method(typeof(CompPowerTrader), "get_PowerOn");
        var powerCompsField = AccessTools.Field(typeof(PowerNet), "powerComps");
        var found = false;
        var codes = codeInstructions.ToList();
        for (var i = 0; i < codes.Count; i++)
        {
            var code = codes[i];
            yield return code;
            if (!found && i > 4 && code.opcode == OpCodes.Brfalse_S && codes[i - 1].Calls(get_PowerOn) && codes[i - 4].LoadsField(powerCompsField))
            {
                found = true;
                yield return new CodeInstruction(OpCodes.Ldarg_0);
                yield return new CodeInstruction(OpCodes.Ldfld, powerCompsField);
                yield return new CodeInstruction(OpCodes.Ldloc_S, 8);
                yield return new CodeInstruction(OpCodes.Callvirt, AccessTools.Method(typeof(List<CompPowerTrader>), "get_Item"));
                yield return new CodeInstruction(OpCodes.Call,
                    AccessTools.Method(typeof(CompSolarPowerUp), nameof(CompSolarPowerUp.PowerUpActive), new[] { typeof(CompPower) }));
                yield return new CodeInstruction(OpCodes.Brtrue_S, code.operand);
            }
        }
    }

    public static void PowerNetOnSolarFlarePostfix(PowerNet __instance)
    {
        __instance.PowerNetTickSolarFlare();
    }

    public static IEnumerable<Toil> FixHacking(IEnumerable<Toil> toils, JobDriver_Hack __instance)
    {
        var idx = 0;
        foreach (var toil in toils)
        {
            if (!__instance.job.targetA.Thing.def.hasInteractionCell)
                switch (idx)
                {
                    case 0:
                        toil.initAction = delegate { toil.actor.pather.StartPath(toil.actor.jobs.curJob.GetTarget(TargetIndex.A), PathEndMode.Touch); };
                        break;
                    case 1:
                        toil.endConditions = new List<Func<JobCondition>>
                        {
                            () => toil.actor.CanReachImmediate(toil.actor.jobs.curJob.GetTarget(TargetIndex.A), PathEndMode.Touch)
                                ? JobCondition.Ongoing
                                : JobCondition.Incompletable,
                            () => toil.actor.jobs.curJob.GetTarget(TargetIndex.A).Thing.TryGetComp<CompHackable>().IsHacked
                                ? JobCondition.Succeeded
                                : JobCondition.Ongoing
                        };
                        break;
                }

            idx++;
            yield return toil;
        }
    }

    public static void AddDeterioration(Thing t, List<string> reasons, ref float __result)
    {
        if (t.TryGetComp<CompNeedsContainment>(out var comp) && comp.ShouldDeteriorate)
        {
            __result += StatDefOf.DeteriorationRate.Worker.GetBaseValueFor(StatRequest.For(t)) * 0.5f;
            reasons?.Add("VFEAncients.DeterioratingUncontained".Translate());
        }
    }

    public class FloatMenuOptionProvider_CarryToPod : FloatMenuOptionProvider
    {
        public override bool Drafted => true;

        public override bool Undrafted => true;

        public override bool Multiselect => false;

        public override IEnumerable<FloatMenuOption> GetOptionsFor(Pawn target, FloatMenuContext context)
        {
            if ((target.IsColonist && target.Downed) || target.IsPrisonerOfColony)
            {
                foreach (FloatMenuOption item in CompGeneTailoringPod.GetCarryToPodJobs(context.FirstSelectedPawn, target)) { 
                    yield return item;
                }
            }
        }
    }
    public class FloatMenuOptionProvider_CarryToBattery : FloatMenuOptionProvider
    {
        public override bool Drafted => true;

        public override bool Undrafted => true;

        public override bool Multiselect => false;

        public override IEnumerable<FloatMenuOption> GetOptionsFor(Pawn target, FloatMenuContext context)
        {
            if (target.Downed)
            {
                foreach (FloatMenuOption item in CompBioBattery.GetCarryToBatteryJobs(context.FirstSelectedPawn, target)) { 
                    yield return item;
                }
            }
        }
    }
}