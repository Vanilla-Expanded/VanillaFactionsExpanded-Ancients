using System.Collections.Generic;
using System.Linq;
using HarmonyLib;
using RimWorld;
using Verse;
using VFEAncients.HarmonyPatches;

namespace VFEAncients
{
    public class PowerWorker_Loner : PowerWorker
    {
        private static readonly Dictionary<Pawn, bool> aloneInRoom = new();

        public PowerWorker_Loner(PowerDef def) : base(def)
        {
        }

        public override void TickRare(Pawn_PowerTracker parent)
        {
            base.TickRare(parent);
            var alone = !parent.Pawn.GetRoom()?.ContainedAndAdjacentThings.Except(parent.Pawn).Any(t => t is Pawn p && p.RaceProps.Humanlike) ?? false;
            aloneInRoom[parent.Pawn] = alone;
            if (alone && !(parent.Pawn.needs?.joy?.IsFrozen ?? true)) parent.Pawn.needs.joy.CurLevel += 0.0015f * 3f;
        }

        public override void DoPatches(Harmony harm)
        {
            base.DoPatches(harm);
            harm.Patch(AccessTools.PropertyGetter(typeof(Need_Joy), "GainingJoy"), postfix: new HarmonyMethod(GetType(), nameof(GainingAlone)));
        }

        public static void GainingAlone(Pawn ___pawn, ref bool __result)
        {
            if (___pawn.HasPower<PowerWorker_Loner>() && aloneInRoom.TryGetValue(___pawn, out var alone) && alone) __result = true;
        }
    }
}