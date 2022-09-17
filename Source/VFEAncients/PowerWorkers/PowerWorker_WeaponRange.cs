// PowerWorker_WeaponRange.cs by Joshua Bennett
// 
// Created 2021-07-25

using System;
using System.Collections.Generic;
using System.Reflection;
using HarmonyLib;
using Verse;

namespace VFEAncients
{
    public class PowerWorker_WeaponRange : PowerWorker
    {
        private static readonly Dictionary<Verb, VerbProperties> originalProps = new();

        public PowerWorker_WeaponRange(PowerDef def) : base(def)
        {
        }

        public override void DoPatches(Harmony harm)
        {
            base.DoPatches(harm);
            harm.Patch(AccessTools.Method(typeof(Pawn_EquipmentTracker), nameof(Pawn_EquipmentTracker.Notify_EquipmentAdded)),
                new HarmonyMethod(GetType(), nameof(ModifyRanges)));
            harm.Patch(AccessTools.Method(typeof(Pawn_EquipmentTracker), nameof(Pawn_EquipmentTracker.Notify_EquipmentRemoved)),
                postfix: new HarmonyMethod(GetType(), nameof(ResetRanges)));
        }

        public static void ModifyRanges(ThingWithComps eq, Pawn_EquipmentTracker __instance)
        {
            if (__instance?.pawn?.GetPowerTracker() == null) return;
            foreach (var power in __instance.pawn.GetPowerTracker().AllPowers)
                if (power.Worker is PowerWorker_WeaponRange wr)
                    foreach (var verb in eq.GetComp<CompEquippable>().AllVerbs)
                        wr.ModifyRange(verb);
        }

        public static void ResetRanges(ThingWithComps eq, Pawn_EquipmentTracker __instance)
        {
            if (__instance?.pawn?.GetPowerTracker() == null) return;
            foreach (var power in __instance.pawn.GetPowerTracker().AllPowers)
                if (power.Worker is PowerWorker_WeaponRange wr)
                    foreach (var verb in eq.GetComp<CompEquippable>().AllVerbs)
                        wr.ResetRange(verb);
        }

        public void ModifyRange(Verb verb)
        {
            var newProps = (VerbProperties) Activator.CreateInstance(verb.verbProps.GetType());
            foreach (var field in verb.verbProps.GetType().GetFields(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance))
                field.SetValue(newProps, field.GetValue(verb.verbProps));

            newProps.range *= GetData<WorkerData_WeaponRange>().rangeFactor;
            originalProps[verb] = verb.verbProps;
            verb.verbProps = newProps;
        }

        public void ResetRange(Verb verb)
        {
            if (originalProps.TryGetValue(verb, out var oldProps))
            {
                verb.verbProps = oldProps;
                originalProps.Remove(verb);
            }
        }

        public override void Notify_Added(Pawn_PowerTracker parent)
        {
            base.Notify_Added(parent);
            foreach (var verb in parent.Pawn.equipment.AllEquipmentVerbs) ModifyRange(verb);
        }

        public override void Notify_Removed(Pawn_PowerTracker parent)
        {
            base.Notify_Removed(parent);
            foreach (var verb in parent.Pawn.equipment.AllEquipmentVerbs) ResetRange(verb);
        }
    }

    public class WorkerData_WeaponRange : WorkerData
    {
        public float rangeFactor;
    }
}