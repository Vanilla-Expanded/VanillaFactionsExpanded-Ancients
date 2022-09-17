using HarmonyLib;
using Verse;
using VFEAncients.HarmonyPatches;

namespace VFEAncients
{
    public class PowerWorker_Bones : PowerWorker
    {
        public PowerWorker_Bones(PowerDef def) : base(def)
        {
        }

        public override void DoPatches(Harmony harm)
        {
            base.DoPatches(harm);
            harm.Patch(
                AccessTools.Method(typeof(Pawn_HealthTracker), nameof(Pawn_HealthTracker.AddHediff),
                    new[] {typeof(Hediff), typeof(BodyPartRecord), typeof(DamageInfo), typeof(DamageWorker.DamageResult)}),
                new HarmonyMethod(GetType(), nameof(HandleBones)));
        }

        public static bool HandleBones(Pawn_HealthTracker __instance, Hediff hediff, BodyPartRecord part = null, DamageInfo? dinfo = null, DamageWorker.DamageResult result = null)
        {
            var pawn = __instance.pawn;
            part ??= hediff.Part;
            return hediff is not Hediff_Injury || dinfo is not {Def: {hediffSolid: var hediffDef}} || hediffDef != hediff.def || !pawn.HasPower<PowerWorker_Bones>() ||
                   !part.def.IsSolid(part, pawn.health.hediffSet.hediffs) || pawn.health.hediffSet.PartOrAnyAncestorHasDirectlyAddedParts(part);
        }
    }
}