using System.Collections.Generic;
using RimWorld;
using UnityEngine;
using Verse;

namespace VFEAncients;

public class CompSolarPowerUp : ThingComp
{
    public static HashSet<CompPower> SolarPoweredUp = new();
    private bool oldElectricityDisabled;
    private CompPowerTrader powerComp;

    public CompProperties_SolarPowerUp Props => props as CompProperties_SolarPowerUp;

    public static bool PowerUpActive(Thing parent) => parent.Spawned && parent.Map.GameConditionManager.ElectricityDisabled(parent.Map);
    public static bool PowerUpActive(CompPower powerComp) => PowerUpActive(powerComp.parent) && SolarPoweredUp.Contains(powerComp);

    public override void PostSpawnSetup(bool respawningAfterLoad)
    {
        base.PostSpawnSetup(respawningAfterLoad);
        powerComp = parent.TryGetComp<CompPowerTrader>();
        if (powerComp is not null)
            SolarPoweredUp.Add(powerComp);
        if (parent.TryGetComp<CompPowerBattery>(out var battery)) SolarPoweredUp.Add(battery);
    }

    public override void CompTick()
    {
        base.CompTick();
        if (powerComp is null) return;
        if (PowerUpActive(parent))
        {
            if (powerComp is CompPowerPlant plant)
            {
                plant.UpdateDesiredPowerOutput();
                plant.PowerOutput *= Props.PowerPlantOutputMult;
            }
            else if (Mathf.Approximately(powerComp.PowerOutput, -powerComp.Props.basePowerConsumption))
            {
                if (powerComp.PowerOutput > 0) powerComp.PowerOutput *= Props.PowerPlantOutputMult;
                else powerComp.PowerOutput = 0;
            }
        }
        else
        {
            if (powerComp is CompPowerPlant plant) plant.UpdateDesiredPowerOutput();
            else powerComp.SetUpPowerVars();
        }
    }

    public override void PostExposeData()
    {
        base.PostExposeData();
        Scribe_Values.Look(ref oldElectricityDisabled, "oldElectricityDisabled");
    }

    public override void PostDeSpawn(Map map)
    {
        SolarPoweredUp.Remove(powerComp);
        base.PostDeSpawn(map);
    }
}

public class CompProperties_SolarPowerUp : CompProperties
{
    public float PowerPlantOutputMult = 1f;
    public float WorkSpeedMult = 2f;

    public CompProperties_SolarPowerUp() => compClass = typeof(CompSolarPowerUp);
}

public class StatPart_SolarPowerUp : StatPart
{
    public override void TransformValue(StatRequest req, ref float val)
    {
        if (req.HasThing && req.Thing.MapHeld.GameConditionManager.ElectricityDisabled(req.Thing.Map)
                         && req.Thing.TryGetComp<CompSolarPowerUp>(out var solarPowerUp))
            val *= solarPowerUp.Props.WorkSpeedMult;
    }

    public override string ExplanationPart(StatRequest req)
    {
        if (req.HasThing && req.Thing.MapHeld.GameConditionManager.ElectricityDisabled(req.Thing.Map)
                         && req.Thing.TryGetComp<CompSolarPowerUp>(out var solarPowerUp))
            return "VFEAncients.SolarPowerUp".Translate() + ": x" + solarPowerUp.Props.WorkSpeedMult.ToStringPercent();
        return "";
    }
}
