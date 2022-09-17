using RimWorld;
using UnityEngine;
using Verse;

namespace VFEAncients
{
    public class Building_Cooler : Building_TempControl
    {
        public override void SpawnSetup(Map map, bool respawningAfterLoad)
        {
            base.SpawnSetup(map, respawningAfterLoad);
            if (compTempControl is null)
            {
                Log.Error(this + " is missing CompTempControl. Perhaps another mod removed it. The building won't work.");
            }
        }
        public override void TickRare()
        {
            if (compPowerTrader.PowerOn)
            {
                var pos = Position + (IntVec3.North * 2).RotatedBy(Rotation);
                var flag = false;
                if (!pos.Impassable(Map))
                {
                    var ambientTemperature = AmbientTemperature;
                    float num;
                    if (ambientTemperature > 20f)
                        num = 1f;
                    else if (ambientTemperature < -120f)
                        num = 0f;
                    else
                        num = Mathf.InverseLerp(-120f, 20f, ambientTemperature);

                    var energyLimit = compTempControl.Props.energyPerSecond * num * 4.16666651f;
                    var change = GenTemperature.ControlTemperatureTempChange(pos, Map, energyLimit, compTempControl.targetTemperature);
                    flag = !Mathf.Approximately(change, 0f);
                    if (flag) pos.GetRoom(Map).Temperature += change;
                }

                var props = compPowerTrader.Props;
                if (flag)
                    compPowerTrader.PowerOutput = -props.basePowerConsumption;
                else
                    compPowerTrader.PowerOutput = -props.basePowerConsumption * compTempControl.Props.lowPowerConsumptionFactor;

                compTempControl.operatingAtHighPower = flag;
            }
        }
    }
}