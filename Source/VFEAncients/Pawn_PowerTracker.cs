using System.Collections.Generic;
using System.Linq;
using RimWorld;
using Verse;

namespace VFEAncients
{
    public class Pawn_PowerTracker : IExposable
    {
        private static readonly Dictionary<Pawn, Pawn_PowerTracker> TRACKERS = new();
        public HashSet<ThoughtDef> AllNullifiedThoughts = new();
        private HashSet<PowerDef> powers = new();

        public Pawn_PowerTracker(Pawn pawn) => Pawn = pawn;

        public Pawn Pawn { get; }
        public IEnumerable<PowerDef> AllPowers => powers;

        public void ExposeData()
        {
            Scribe_Collections.Look(ref powers, "powers", LookMode.Def);
            if (!powers.EnumerableNullOrEmpty() && Scribe.mode == LoadSaveMode.PostLoadInit)
            {
                var tempPowers = powers.ToArray();
                powers.Clear();
                foreach (var power in tempPowers) AddPower(power);
            }
        }

        public bool HasPower(PowerDef power) => powers.Contains(power);

        public void AddPower(PowerDef power)
        {
            if (powers.Contains(power)) Log.Warning($"Attempted to add power that {Pawn} already has: {power}");
            powers.Add(power);
            power.Worker.Notify_Added(this);
            Pawn?.needs?.mood?.thoughts?.memories?.TryGainMemory(VFEA_DefOf.VFEA_AcquiredSuperpower);
        }

        public void RemovePower(PowerDef power)
        {
            if (!powers.Contains(power)) return;
            powers.Remove(power);
            power.Worker.Notify_Removed(this);
        }

        public static void Save(Pawn __instance)
        {
            var tracker = Get(__instance);
            Scribe_Deep.Look(ref tracker, "vfea_powerTracker", __instance);
            TRACKERS[__instance] = tracker;
        }

        public static Pawn_PowerTracker Get(Pawn pawn)
        {
            if (pawn is null) return null;
            if (TRACKERS.TryGetValue(pawn, out var tracker) && tracker != null) return tracker;
            if (CanGetPowers(pawn))
            {
                tracker = new Pawn_PowerTracker(pawn);
                TRACKERS.SetOrAdd(pawn, tracker);
                return tracker;
            }

            return null;
        }

        public static bool CanGetPowers(Pawn pawn) => pawn.RaceProps.intelligence >= Intelligence.Humanlike;
    }
}