using RimWorld;
using Verse;

namespace VFEAncients
{
    public class LightningSpot : ThingWithComps
    {
        public override void Tick()
        {
            base.Tick();
            if (this.IsHashIntervalTick(30)) Map.weatherManager.eventHandler.AddEvent(new WeatherEvent_LightningStrike(Map, Position));
        }
    }
}