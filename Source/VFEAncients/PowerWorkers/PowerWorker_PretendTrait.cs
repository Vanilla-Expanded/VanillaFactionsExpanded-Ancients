using System.Collections.Generic;
using HarmonyLib;
using RimWorld;
using Verse;

namespace VFEAncients
{
    public class PowerWorker_PretendTrait : PowerWorker
    {
        private static readonly Dictionary<TraitDef, HashSet<Pawn>> pretending = new();

        public PowerWorker_PretendTrait(PowerDef def) : base(def)
        {
        }

        public override void DoPatches(Harmony harm)
        {
            base.DoPatches(harm);
            harm.Patch(AccessTools.Method(typeof(TraitSet), nameof(TraitSet.HasTrait), new[] {typeof(TraitDef)}), postfix: new HarmonyMethod(GetType(), nameof(HasTrait_Postfix)));
            harm.Patch(AccessTools.Method(typeof(TraitSet), nameof(TraitSet.HasTrait), new[] {typeof(TraitDef), typeof(int)}),
                postfix: new HarmonyMethod(GetType(), nameof(HasTrait_Postfix)));
        }

        public override void Notify_Added(Pawn_PowerTracker parent)
        {
            base.Notify_Added(parent);
            var trait = GetData<WorkerData_Trait>();
            if (trait == null)
            {
                Log.Warning("[VFEA] Using PowerWorker_PretendTrait without WorkerData_Trait");
                return;
            }

            if (pretending.TryGetValue(trait.Trait, out var pawns)) pawns.Add(parent.Pawn);
            else pretending.Add(trait.Trait, new HashSet<Pawn> {parent.Pawn});
        }

        public override void Notify_Removed(Pawn_PowerTracker parent)
        {
            base.Notify_Removed(parent);
            pretending[GetData<WorkerData_Trait>().Trait].Remove(parent.Pawn);
        }

        public static void HasTrait_Postfix(TraitDef tDef, Pawn ___pawn, ref bool __result)
        {
            if (!__result && pretending.TryGetValue(tDef, out var pawns) && pawns.Contains(___pawn)) __result = true;
        }
    }

    public class WorkerData_Trait : WorkerData
    {
        public TraitDef Trait;
    }
}