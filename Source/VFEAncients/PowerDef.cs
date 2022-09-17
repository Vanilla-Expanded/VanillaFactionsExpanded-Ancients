using System;
using System.Collections.Generic;
using RimWorld;
using UnityEngine;
using Verse;
using AbilityDef = VFECore.Abilities.AbilityDef;

// ReSharper disable InconsistentNaming

namespace VFEAncients
{
    public class PowerDef : Def
    {
        private static readonly HashSet<Type> appliedPatches = new();
        public static HashSet<ThoughtDef> GlobalNullifiedThoughts = new();
        public List<AbilityDef> abilities;
        public WorkTags disabledWorkTags = WorkTags.None;
        public string effectDescription;
        public List<HediffDef> hediffs;
        public List<ThoughtDef> nullifiedThoughts;

        public PowerType powerType;
        public List<StatModifier> setStats = new();
        public List<StatModifier> statFactors = new();
        public List<StatModifier> statOffsets = new();
        public string texPath;
        public TickerType tickerType = TickerType.Never;
        public Type workerClass = typeof(PowerWorker);
        public WorkerData workerData;

        public Texture2D Icon { get; private set; }

        public PowerWorker Worker { get; private set; }

        public override IEnumerable<string> ConfigErrors()
        {
            foreach (var error in base.ConfigErrors()) yield return error;
            if (workerClass == null) yield return "Worker class cannot be null";
            if (!typeof(PowerWorker).IsAssignableFrom(workerClass)) yield return "Worker class must be subclass of PowerWorker";
        }

        public override void PostLoad()
        {
            base.PostLoad();
            Worker = (PowerWorker) Activator.CreateInstance(workerClass, this);
            if (!appliedPatches.Contains(workerClass))
            {
                Worker.DoPatches(VFEAncientsMod.Harm);
                appliedPatches.Add(workerClass);
            }

            LongEventHandler.ExecuteWhenFinished(() =>
            {
                Icon = ContentFinder<Texture2D>.Get(texPath);
                if (nullifiedThoughts != null)
                    foreach (var def in nullifiedThoughts)
                        GlobalNullifiedThoughts.Add(def);
            });
        }
    }

    public class WorkerData
    {
    }

    public enum PowerType
    {
        Superpower,
        Weakness
    }
}