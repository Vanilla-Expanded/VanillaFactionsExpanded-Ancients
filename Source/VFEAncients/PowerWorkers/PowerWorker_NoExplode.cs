using HarmonyLib;
using Verse;
using VFEAncients.HarmonyPatches;

namespace VFEAncients
{
    public class PowerWorker_NoExplode : PowerWorker
    {
        public PowerWorker_NoExplode(PowerDef def) : base(def)
        {
        }

        public override void DoPatches(Harmony harm)
        {
            base.DoPatches(harm);
            harm.Patch(AccessTools.Method(typeof(DamageWorker), "ExplosionDamageThing"), new HarmonyMethod(GetType(), nameof(Immunity)));
        }

        public static bool Immunity(Thing t) => !t.HasPower<PowerWorker_NoExplode>();
    }
}