using System.Collections.Generic;
using HarmonyLib;
using RimWorld;
using Verse;
using Verse.AI;
using VFEAncients.HarmonyPatches;

namespace VFEAncients
{
    public class PowerWorker_BreakOnReject : PowerWorker
    {
        private static readonly Dictionary<Pawn, Pawn> forcedTargets = new();

        public PowerWorker_BreakOnReject(PowerDef def) : base(def)
        {
        }

        public override void DoPatches(Harmony harm)
        {
            base.DoPatches(harm);
            harm.Patch(AccessTools.Method(typeof(MemoryThoughtHandler), nameof(MemoryThoughtHandler.TryGainMemory), new[] {typeof(ThoughtDef), typeof(Pawn), typeof(Precept)}),
                postfix: new HarmonyMethod(GetType(), nameof(PostMemoryAdd)));
            harm.Patch(AccessTools.Method(typeof(MentalState_MurderousRage), "TryFindNewTarget"), new HarmonyMethod(GetType(), nameof(ForceTarget)));
        }

        public static void PostMemoryAdd(MemoryThoughtHandler __instance, Pawn otherPawn, ThoughtDef def)
        {
            if (def == ThoughtDefOf.RebuffedMyRomanceAttempt && __instance.pawn.HasPower<PowerWorker_BreakOnReject>())
            {
                var data = __instance.pawn.GetData<WorkerData_Break>();
                if (data != null && Rand.Chance(data.BreakChance))
                {
                    forcedTargets[__instance.pawn] = otherPawn;
                    data.Break.Worker.TryStart(__instance.pawn, "VFEAncients.Reason.Rejected".Translate(otherPawn.LabelNoCountColored), false);
                }
            }
        }

        public static bool ForceTarget(MentalState_MurderousRage __instance, ref bool __result)
        {
            if (__instance.pawn.HasPower<PowerWorker_BreakOnReject>() && forcedTargets.TryGetValue(__instance.pawn, out var otherPawn))
            {
                __instance.target = otherPawn;
                __result = __instance.IsTargetStillValidAndReachable();
                return false;
            }

            return true;
        }
    }

    public class WorkerData_Break : WorkerData
    {
        public MentalBreakDef Break;
        public float BreakChance;
    }
}