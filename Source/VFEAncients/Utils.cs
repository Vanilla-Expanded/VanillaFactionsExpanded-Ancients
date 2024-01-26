using System;
using System.Collections.Generic;
using System.Linq;
using HarmonyLib;
using RimWorld;
using Verse;

namespace VFEAncients
{
    public static class Utils
    {
        public static Pawn_PowerTracker GetPowerTracker(this Pawn pawn) => Pawn_PowerTracker.Get(pawn);

        public static void Split<T>(this IEnumerable<T> source, out List<T> truthy, out List<T> falsy, Func<T, bool> func)
        {
            truthy = new List<T>();
            falsy = new List<T>();
            foreach (var t in source)
                if (func(t))
                    truthy.Add(t);
                else falsy.Add(t);
        }

        public static bool TryPeek<T>(this Queue<T> queue, out T res)
        {
            try
            {
                res = queue.Peek();
                return true;
            }
            catch (InvalidOperationException)
            {
                res = default;
                return false;
            }
        }

        public static bool TryGetComp<T>(this Thing t, out T comp) where T : ThingComp
        {
            comp = t.TryGetComp<T>();
            return comp != null;
        }

        public static bool TryGetModExtension<T>(this Def def, out T ext) where T : DefModExtension
        {
            ext = def.HasModExtension<T>() ? def.GetModExtension<T>() : null;
            return ext != null;
        }

        public static IEnumerable<T> DoPassThrough<T>(this IEnumerable<T> source, Action<T> action)
        {
            foreach (var item in source)
            {
                action(item);
                yield return item;
            }
        }

        public static T TryGetComp<T>(this Storyteller storyteller) where T : StorytellerComp
        {
            return storyteller.storytellerComps.FirstOrFallback(comp => comp is T) as T;
        }

        public static string TrimLines(this string input, int start = 0, int end = 0)
        {
            var arr = input.Split('\n');
            var c = arr.Length;
            return arr.Skip(start).Take(c - end - start).Join(delimiter: "\n");
        }

        public static string TrimEmptyLines(this string input)
        {
            return input.Split('\n').Where(str => !str.NullOrEmpty()).Join(delimiter: "\n");
        }

        public static bool IsPowered(this Thing thing)
        {
            return !thing.TryGetComp<CompPowerTrader>(out var comp) || comp.PowerOn;
        }
    }
}