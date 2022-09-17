using System.Collections.Generic;
using System.Linq;
using HarmonyLib;
using RimWorld;
using Verse;
using VFEAncients.HarmonyPatches;

namespace VFEAncients
{
    public class PowerWorker_NoPrioritize : PowerWorker
    {
        public PowerWorker_NoPrioritize(PowerDef def) : base(def)
        {
        }

        public override void DoPatches(Harmony harm)
        {
            base.DoPatches(harm);
            harm.Patch(AccessTools.Method(typeof(FloatMenuMakerMap), "AddJobGiverWorkOrders"), new HarmonyMethod(GetType(), nameof(StoreOpts)),
                new HarmonyMethod(GetType(), nameof(DisableOpts)));
        }

        public static void StoreOpts(List<FloatMenuOption> opts, ref List<FloatMenuOption> __state, Pawn pawn)
        {
            if (pawn.HasPower<PowerWorker_NoPrioritize>())
                __state = opts.ListFullCopy();
        }

        public static void DisableOpts(List<FloatMenuOption> opts, List<FloatMenuOption> __state, Pawn pawn)
        {
            if (pawn.HasPower<PowerWorker_NoPrioritize>())
                foreach (var option in opts.Except(__state).Where(opt => !opt.Disabled))
                {
                    option.action = null;
                    option.Label = "VFEAncients.CannotPrioritizeAudacious".Translate(pawn.LabelShortCap);
                }
        }
    }
}