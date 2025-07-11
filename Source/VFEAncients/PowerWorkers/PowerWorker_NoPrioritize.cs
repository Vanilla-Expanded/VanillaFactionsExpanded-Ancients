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
            harm.Patch(
                AccessTools.Method(typeof(FloatMenuMakerMap), nameof(FloatMenuMakerMap.ShouldGenerateFloatMenuForPawn), new[] { typeof(Pawn) }),
                postfix: new HarmonyMethod(GetType(), nameof(DisableOpts))
            );
        }

        public static void DisableOpts(Pawn pawn, ref AcceptanceReport __result)
        {
            if (pawn.HasPower<PowerWorker_NoPrioritize>())
                __result = new AcceptanceReport("VFEAncients.CannotPrioritizeAudacious".Translate(pawn.LabelShortCap));
        }
    }
}