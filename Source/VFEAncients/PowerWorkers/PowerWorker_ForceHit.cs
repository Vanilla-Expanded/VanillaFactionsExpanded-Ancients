// ReSharper disable InconsistentNaming

using System.Collections.Generic;
using System.Linq;
using System.Reflection.Emit;
using HarmonyLib;
using Verse;
using VFEAncients.HarmonyPatches;

namespace VFEAncients
{
    public class PowerWorker_ForceHit : PowerWorker
    {
        public PowerWorker_ForceHit(PowerDef def) : base(def)
        {
        }

        public override void DoPatches(Harmony harm)
        {
            base.DoPatches(harm);
            harm.Patch(AccessTools.Method(typeof(Verb_LaunchProjectile), "TryCastShot"),
                transpiler: new HarmonyMethod(GetType(), nameof(TryCastShot_Transpile)));
            harm.Patch(AccessTools.Method(typeof(TooltipUtility), nameof(TooltipUtility.ShotCalculationTipString)),
                transpiler: new HarmonyMethod(GetType(), nameof(ShotCalculationTipString_Transpile)) {after = new[] {"drumad.rimworld.nightvision"}});
            if (VFEAncientsMod.YayosCombat)
            {
                var info = AccessTools.Method(AccessTools.Inner(AccessTools.TypeByName("yayoCombat.yyShotReport"), "yayoTryCastShot"), "Prefix");
                harm.Patch(info, transpiler: new HarmonyMethod(GetType(), nameof(TryCastShot_Transpile_Yayos)));
            }
        }

        public static IEnumerable<CodeInstruction> ShotCalculationTipString_Transpile(IEnumerable<CodeInstruction> instructions, ILGenerator generator)
        {
            var list = instructions.ToList();
            var info1 = AccessTools.Method(typeof(ShotReport), nameof(ShotReport.GetTextReadout));
            var idx1 = list.FindIndex(ins => ins.Calls(info1));
            var label1 = generator.DefineLabel();
            list[idx1 + 1].labels.Add(label1);
            list.InsertRange(idx1 + 1, new[]
            {
                new CodeInstruction(OpCodes.Ldloc_2),
                new CodeInstruction(OpCodes.Ldfld, AccessTools.Field(typeof(Verb), "caster")),
                new CodeInstruction(OpCodes.Call, typeof(PowerWorker_ForceHit).HasPowerType()),
                new CodeInstruction(OpCodes.Brfalse, label1),
                new CodeInstruction(OpCodes.Pop),
                new CodeInstruction(OpCodes.Ldstr, "VFEAncients.MarksmanshipTooltip"),
                new CodeInstruction(OpCodes.Call, AccessTools.Method(typeof(Translator), nameof(Translator.Translate), new[] {typeof(string)})),
                new CodeInstruction(OpCodes.Call, AccessTools.Method(typeof(TaggedString), "op_Implicit", new[] {typeof(TaggedString)}))
            });

            return list;
        }

        public static IEnumerable<CodeInstruction> TryCastShot_Transpile(IEnumerable<CodeInstruction> instructions)
        {
            var list = instructions.ToList();

            var infos = new[]
            {
                AccessTools.PropertyGetter(typeof(ShotReport), nameof(ShotReport.AimOnTargetChance_IgnoringPosture)),
                AccessTools.PropertyGetter(typeof(ShotReport), nameof(ShotReport.PassCoverChance))
            };
            foreach (var info1 in infos)
            {
                var idx1 = list.FindIndex(ins => ins.Calls(info1));
                AddForceHitLogic(list, idx1);
            }

            return list;
        }

        public static IEnumerable<CodeInstruction> TryCastShot_Transpile_Yayos(IEnumerable<CodeInstruction> instructions, ILGenerator generator)
        {
            var list = instructions.ToList();
            var idx1 = list.FindLastIndex(ins => ins.opcode == OpCodes.Ldloc_S && ins.operand is LocalBuilder {LocalIndex: 15});
            var label1 = generator.DefineLabel();
            list[idx1 + 1].labels.Add(label1);
            list.InsertRange(idx1 + 1, new[]
            {
                new CodeInstruction(OpCodes.Ldarg_1),
                new CodeInstruction(OpCodes.Callvirt, AccessTools.PropertyGetter(typeof(Verb), nameof(Verb.Caster))),
                new CodeInstruction(OpCodes.Call, typeof(PowerWorker_ForceHit).HasPowerType()),
                new CodeInstruction(OpCodes.Brfalse, label1),
                new CodeInstruction(OpCodes.Pop),
                new CodeInstruction(OpCodes.Ldc_R4, 0f)
            });
            var info = AccessTools.PropertyGetter(typeof(ShotReport), nameof(ShotReport.PassCoverChance));
            var idx2 = list.FindIndex(ins => ins.Calls(info));
            var label2 = generator.DefineLabel();
            list[idx2 + 1].labels.Add(label2);
            list.InsertRange(idx2 + 1, new[]
            {
                new CodeInstruction(OpCodes.Ldarg_1),
                new CodeInstruction(OpCodes.Callvirt, AccessTools.PropertyGetter(typeof(Verb), nameof(Verb.Caster))),
                new CodeInstruction(OpCodes.Call, typeof(PowerWorker_ForceHit).HasPowerType()),
                new CodeInstruction(OpCodes.Brfalse, label2),
                new CodeInstruction(OpCodes.Pop),
                new CodeInstruction(OpCodes.Ldc_R4, 1f)
            });
            return list;
        }

        public static void AddForceHitLogic(List<CodeInstruction> list, int idx)
        {
            var label = (Label) list[idx + 2].operand;
            list.InsertRange(idx - 1, new[]
            {
                new CodeInstruction(OpCodes.Ldarg_0),
                new CodeInstruction(OpCodes.Callvirt, AccessTools.PropertyGetter(typeof(Verb), nameof(Verb.Caster))),
                new CodeInstruction(OpCodes.Call, typeof(PowerWorker_ForceHit).HasPowerType()),
                new CodeInstruction(OpCodes.Brtrue, label)
            });
        }
    }
}