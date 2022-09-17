using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using HarmonyLib;
using RimWorld;
using Verse;
using VFEAncients.HarmonyPatches;
using VFECore.Abilities;

namespace VFEAncients
{
    [StaticConstructorOnStartup]
    public class PowerWorker
    {
        private static byte[] cachedStatsToTouch;
        public PowerDef def;

        static PowerWorker() => LongEventHandler.ExecuteWhenFinished(() => cachedStatsToTouch = new byte[DefDatabase<StatDef>.DefCount]);

        public PowerWorker(PowerDef def) => this.def = def;

        public virtual IEnumerable<WorkTypeDef> DisabledWorkTypes => DefDatabase<WorkTypeDef>.AllDefs.Where(wtd => !AllowsWorkType(wtd));

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool TouchStat(StatDef stat) => stat.index > cachedStatsToTouch.Length || cachedStatsToTouch[stat.index] > 0;

        public T GetData<T>() where T : WorkerData => def.workerData as T;

        public virtual void Notify_Added(Pawn_PowerTracker parent)
        {
            if (def.abilities != null && parent.Pawn.TryGetComp<CompAbilities>(out var comp))
                foreach (var ability in def.abilities.Where(abs => !comp.HasAbility(abs)))
                    comp.GiveAbility(ability);
            if (def.hediffs != null)
                foreach (var hediff in def.hediffs)
                    parent.Pawn.health.AddHediff(hediff);
            if (def.tickerType != TickerType.Never) Current.Game.GetComponent<GameComponent_Ancients>().TickLists[def.tickerType].Add((parent, def));
            if (def.disabledWorkTags != WorkTags.None) parent.Pawn.cachedDisabledWorkTypes?.Clear();
            if (Helpers.workerTypesByPawn.TryGetValue(parent.Pawn, out var types)) types.Add(GetType());
            else Helpers.workerTypesByPawn.Add(parent.Pawn, new HashSet<Type> {GetType()});
            if (!def.nullifiedThoughts.NullOrEmpty()) parent.AllNullifiedThoughts.AddRange(def.nullifiedThoughts);
            foreach (var statDef in def.statFactors.Concat(def.statOffsets).Concat(def.setStats).Select(sm => sm.stat)) cachedStatsToTouch[statDef.index]++;
        }

        public virtual void Notify_Removed(Pawn_PowerTracker parent)
        {
            if (def.abilities != null && parent.Pawn.TryGetComp<CompAbilities>(out var comp))
                foreach (var ability in def.abilities)
                    comp.LearnedAbilities.RemoveAll(ab => ab.def == ability);
            if (def.hediffs != null)
                foreach (var hediff in parent.Pawn.health.hediffSet.hediffs.Where(hd => hd.Part == null && def.hediffs.Contains(hd.def)).ToList())
                    parent.Pawn.health.RemoveHediff(hediff);
            if (def.tickerType != TickerType.Never) Current.Game.GetComponent<GameComponent_Ancients>().TickLists[def.tickerType].Remove((parent, def));
            if (def.disabledWorkTags != WorkTags.None) parent.Pawn.cachedDisabledWorkTypes?.Clear();
            Helpers.workerTypesByPawn[parent.Pawn].Remove(GetType());
            if (!def.nullifiedThoughts.NullOrEmpty())
            {
                parent.AllNullifiedThoughts.Clear();
                foreach (var powerDef in parent.AllPowers.Except(def))
                    if (!powerDef.nullifiedThoughts.NullOrEmpty())
                        parent.AllNullifiedThoughts.AddRange(powerDef.nullifiedThoughts);
            }

            foreach (var statDef in def.statFactors.Concat(def.statOffsets).Concat(def.setStats).Select(sm => sm.stat)) cachedStatsToTouch[statDef.index]--;
        }

        public string EffectString()
        {
            var builder = new StringBuilder();
            if (def.abilities != null)
                foreach (var ability in def.abilities)
                {
                    builder.AppendLine("VFEAncients.Effect.AddAbility".Translate(ability.label));
                    if (!ability.description.NullOrEmpty()) builder.AppendLine(ability.description);
                }

            if (def.hediffs != null)
                foreach (var hediff in def.hediffs)
                    builder.AppendLine("VFEAncients.Effect.AddHediff".Translate(hediff.label));
            if (def.statFactors != null)
                foreach (var factor in def.statFactors)
                    builder.AppendLine($"{factor.stat.LabelForFullStatListCap}: {factor.ToStringAsFactor}");
            if (def.statOffsets != null)
                foreach (var factor in def.statOffsets)
                    builder.AppendLine($"{factor.stat.LabelForFullStatListCap}: {factor.ValueToStringAsOffset}");
            if (def.setStats != null)
                foreach (var factor in def.setStats)
                    builder.AppendLine($"{factor.stat.LabelForFullStatListCap}: {factor.stat.ValueToString(factor.value)}");
            if (def.nullifiedThoughts != null)
                foreach (var thoughtDef in def.nullifiedThoughts)
                    builder.AppendLine("VFEAncients.Effect.NullThought".Translate(thoughtDef.Label.Formatted("")));
            if (def.disabledWorkTags != WorkTags.None)
                foreach (var workTypeDef in DefDatabase<WorkTypeDef>.AllDefs.Where(work => (work.workTags & def.disabledWorkTags) != WorkTags.None))
                    builder.AppendLine(workTypeDef.gerundLabel.CapitalizeFirst() + " " + "DisabledLower".Translate());
            if (!AdditionalEffects().NullOrEmpty()) builder.AppendLine(AdditionalEffects());
            if (!def.effectDescription.NullOrEmpty()) builder.AppendLine(def.effectDescription);
            if (builder.Length > 0) builder.Insert(0, "\n" + "VFEAncients.Effects".Translate() + "\n");
            return builder.ToString();
        }

        private bool AllowsWorkType(WorkTypeDef workType) => (def.disabledWorkTags & workType.workTags) == WorkTags.None;

        private bool AllowsWorkGiver(WorkGiverDef workGiver) => (def.disabledWorkTags & workGiver.workTags) == WorkTags.None;

        public virtual void DoPatches(Harmony harm)
        {
        }

        public virtual string AdditionalEffects() => "";


        public virtual void Tick(Pawn_PowerTracker parent)
        {
        }

        public virtual void TickRare(Pawn_PowerTracker parent)
        {
        }

        public virtual void TickLong(Pawn_PowerTracker parent)
        {
        }
    }
}