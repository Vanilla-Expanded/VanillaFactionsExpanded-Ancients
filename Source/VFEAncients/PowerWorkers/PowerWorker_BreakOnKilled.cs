using System.Collections.Generic;
using System.Linq;
using System.Reflection.Emit;
using HarmonyLib;
using RimWorld;
using Verse;
using VFEAncients.HarmonyPatches;

namespace VFEAncients
{
    public class PowerWorker_BreakOnKilled : PowerWorker
    {
        public PowerWorker_BreakOnKilled(PowerDef def) : base(def)
        {
        }

        public override void DoPatches(Harmony harm)
        {
            base.DoPatches(harm);
            harm.Patch(AccessTools.Method(typeof(PawnDiedOrDownedThoughtsUtility), "AppendThoughts_ForHumanlike"),
                transpiler: new HarmonyMethod(GetType(), nameof(InjectOnKilled)));
        }

        public static IEnumerable<CodeInstruction> InjectOnKilled(IEnumerable<CodeInstruction> instructions)
        {
            var list = instructions.ToList();
            var idx1 = list.FindIndex(ins => ins.opcode == OpCodes.Ldloc_2);
            list.InsertRange(idx1 + 1, new[]
            {
                new CodeInstruction(OpCodes.Ldloc_2),
                new CodeInstruction(OpCodes.Ldarg_0),
                new CodeInstruction(OpCodes.Call, AccessTools.Method(typeof(PowerWorker_BreakOnKilled), nameof(OnKilled)))
            });
            return list;
        }

        public static void OnKilled(Pawn killer, Pawn killed)
        {
            if (killer.HasPower<PowerWorker_BreakOnKilled>())
            {
                var data = killer.GetData<WorkerData_Break>();
                if (data != null && Rand.Chance(data.BreakChance)) data.Break.Worker.TryStart(killer, "VFEAncients.Reason.KilledSomeone".Translate(), false);
            }
        }
    }
}