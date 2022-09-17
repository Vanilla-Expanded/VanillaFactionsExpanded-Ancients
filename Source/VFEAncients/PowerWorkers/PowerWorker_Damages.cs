using System.Collections.Generic;
using System.Linq;
using HarmonyLib;
using Verse;
using VFEAncients.HarmonyPatches;

namespace VFEAncients
{
    public class PowerWorker_Damages : PowerWorker
    {
        public PowerWorker_Damages(PowerDef def) : base(def)
        {
        }

        public override void DoPatches(Harmony harm)
        {
            base.DoPatches(harm);
            harm.Patch(AccessTools.Method(typeof(Thing), nameof(Thing.TakeDamage)), new HarmonyMethod(GetType(), nameof(ChangeDamage)));
        }

        public static void ChangeDamage(ref DamageInfo dinfo, Thing __instance)
        {
            if (__instance.HasPower<PowerWorker_Damages>())
            {
                var def = dinfo.Def;
                var data = __instance.GetData<WorkerData_Damages>();
                if (data.Resist.Contains(def)) dinfo.SetAmount(0f);
                if (data.Multipliers.FirstOrDefault(m => m.damageDef == def) is {multiplier: var mult}) dinfo.SetAmount(dinfo.Amount * mult);
            }
        }
    }

    public class WorkerData_Damages : WorkerData
    {
        public List<DamageMultiplier> Multipliers = new();
        public List<DamageDef> Resist = new();
    }
}