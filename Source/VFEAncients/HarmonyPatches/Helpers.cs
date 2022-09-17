using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using HarmonyLib;
using Verse;

namespace VFEAncients.HarmonyPatches
{
    public static class Helpers
    {
        public static Dictionary<Pawn, HashSet<Type>> workerTypesByPawn = new();

        public static MethodInfo HasPowerDef => AccessTools.Method(typeof(Helpers), nameof(HasPower), new[] {typeof(Pawn), typeof(PowerDef)});
        public static MethodInfo HasPowerType(this Type generic) => AccessTools.Method(typeof(Helpers), nameof(HasPower), new[] {typeof(Thing)}, new[] {generic});

        public static bool HasPower<T>(this Thing caster) where T : PowerWorker =>
            caster is Pawn pawn && workerTypesByPawn.TryGetValue(pawn, out var types) && types.Contains(typeof(T));

        public static T GetData<T>(this Thing caster) where T : WorkerData
        {
            return (caster as Pawn)?.GetPowerTracker()?.AllPowers.FirstOrDefault(p => p.workerData is T)?.Worker.GetData<T>();
        }

        public static bool HasPower(this Pawn pawn, PowerDef power) => pawn.GetPowerTracker()?.HasPower(power) ?? false;
    }
}