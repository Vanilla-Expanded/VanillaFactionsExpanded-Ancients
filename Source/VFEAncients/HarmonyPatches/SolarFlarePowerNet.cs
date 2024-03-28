using System;
using System.Collections.Generic;
using System.Linq;
using RimWorld;
using UnityEngine;
using Verse;

namespace VFEAncients.HarmonyPatches;

public static class SolarFlarePowerNet
{
    public static IEnumerable<CompPowerTrader> PowerTradersSolarFlare(this PowerNet net) => net.powerComps.Where(CompSolarPowerUp.PowerUpActive);

    public static float EnergyGainRateSolarFlare(this PowerNet net) =>
        DebugSettings.unlimitedPower ? 100000f : net.PowerTradersSolarFlare().Where(pc => pc.PowerOn).Sum(pc => pc.EnergyOutputPerTick);

    public static float CurrentStoredEnergySolarFlare(this PowerNet net) => net.BatteriesSolarFlare().Sum(batt => batt.StoredEnergy);
    public static IEnumerable<CompPowerBattery> BatteriesSolarFlare(this PowerNet net) => net.batteryComps.Where(CompSolarPowerUp.PowerUpActive);

    public static void PowerNetTickSolarFlare(this PowerNet net)
    {
        if (net.Map.gameConditionManager.ElectricityDisabled(net.Map))
        {
            var num = net.EnergyGainRateSolarFlare();
            var num2 = net.CurrentStoredEnergySolarFlare();
            if (num2 + num >= -1E-07f) net.ChangeStoredEnergySolarFlare(num);

            PowerNet.partsWantingPowerOn.Clear();
            foreach (var t in net.PowerTradersSolarFlare())
                if (!t.PowerOn && FlickUtility.WantsToBeOn(t.parent) && !t.parent.IsBrokenDown())
                    PowerNet.partsWantingPowerOn.Add(t);

            if (PowerNet.partsWantingPowerOn.Count > 0)
            {
                var num4 = 200 / PowerNet.partsWantingPowerOn.Count;
                if (num4 < 30) num4 = 30;
                if (Find.TickManager.TicksGame % num4 == 0)
                {
                    var num5 = Mathf.Max(1, Mathf.RoundToInt(PowerNet.partsWantingPowerOn.Count * 0.05f));
                    for (var j = 0; j < num5; j++)
                    {
                        var compPowerTrader = PowerNet.partsWantingPowerOn.RandomElement();
                        if (!compPowerTrader.PowerOn)
                        {
                            compPowerTrader.PowerOn = true;
                            num += compPowerTrader.EnergyOutputPerTick;
                        }
                    }
                }
            }
        }
    }

    public static void ChangeStoredEnergySolarFlare(this PowerNet net, float extra)
    {
        if (extra > 0f)
        {
            net.DistributeEnergyAmongBatteriesSolarFlare(extra);
            return;
        }

        var num = -extra;
        net.givingBats.Clear();
        net.givingBats.AddRange(net.BatteriesSolarFlare().Where(t => t.StoredEnergy > 1E-07f));

        var a = num / net.givingBats.Count;
        var num2 = 0;
        while (num > 1E-07f)
        {
            foreach (var bat in net.givingBats)
            {
                var num3 = Mathf.Min(a, bat.StoredEnergy);
                bat.DrawPower(num3);
                num -= num3;
                if (num < 1E-07f) return;
            }

            num2++;
            if (num2 > 10) break;
        }

        if (num > 1E-07f) Log.Warning("Drew energy from a PowerNet that didn't have it.");
    }

    public static void DistributeEnergyAmongBatteriesSolarFlare(this PowerNet net, float energy)
    {
        if (energy <= 0f || !net.BatteriesSolarFlare().Any()) return;

        PowerNet.batteriesShuffled.Clear();
        PowerNet.batteriesShuffled.AddRange(net.BatteriesSolarFlare());
        PowerNet.batteriesShuffled.Shuffle();
        var iter = 0;
        for (;;)
        {
            iter++;
            if (iter > 10000) break;

            var minCanAccept = PowerNet.batteriesShuffled.Aggregate(float.MaxValue, (current, t) => Mathf.Min(current, t.AmountCanAccept));

            if (energy < minCanAccept * PowerNet.batteriesShuffled.Count) goto IL_100;

            for (var i = PowerNet.batteriesShuffled.Count - 1; i >= 0; i--)
            {
                var amountCanAccept = PowerNet.batteriesShuffled[i].AmountCanAccept;
                var flag = amountCanAccept <= 0f || Math.Abs(amountCanAccept - minCanAccept) < 1E-07f;
                if (minCanAccept > 0f)
                {
                    PowerNet.batteriesShuffled[i].AddEnergy(minCanAccept);
                    energy -= minCanAccept;
                }

                if (flag) PowerNet.batteriesShuffled.RemoveAt(i);
            }

            if (energy < 0.0005f || !PowerNet.batteriesShuffled.Any()) goto IL_15B;
        }

        Log.Error("Too many iterations.");
        goto IL_15B;
    IL_100:
        var amount = energy / PowerNet.batteriesShuffled.Count;
        foreach (var t in PowerNet.batteriesShuffled) t.AddEnergy(amount);
    IL_15B:
        PowerNet.batteriesShuffled.Clear();
    }
}
