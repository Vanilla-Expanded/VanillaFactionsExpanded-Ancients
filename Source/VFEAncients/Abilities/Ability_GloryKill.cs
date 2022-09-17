using System.Collections.Generic;
using System.Linq;
using RimWorld;
using RimWorld.Planet;
using Verse;
using Verse.Sound;

namespace VFEAncients
{
    public class Ability_GloryKill : Ability
    {
        public IEnumerable<Pawn> PossibleTargets =>
            GenAdj.CellsAdjacent8Way(pawn).SelectMany(c => c.GetThingList(pawn.Map)).Where(t => t.HostileTo(pawn)).OfType<Pawn>();

        public override void Cast(params GlobalTargetInfo[] targets)
        {
            base.Cast(targets);

            if (VFEAncientsMod.settings.enableGloryKillMusic) VFEA_DefOf.VFEA_GloryKill_Music.PlayOneShot(pawn);

            foreach (var target in targets)
            {
                if (target.Thing is not Pawn p) continue;
                var limbs = AllLimbs(p).ToList();
                foreach (var part in limbs.InRandomOrder().Take(new IntRange(1, limbs.Count - 1).RandomInRange))
                    p.health.AddHediff(HediffDefOf.MissingBodyPart, part);

                var blood = new IntRange(25, 50).RandomInRange;
                for (var i = 0; i < blood; i++)
                    FilthMaker.TryMakeFilth(GenRadial.RadialCellsAround(p.PositionHeld, 2.9f, true).RandomElement(), p.MapHeld, p.RaceProps.BloodDef,
                        p.LabelIndefinite());

                p.Kill(null);
            }
        }

        public override void ModifyTargets(ref GlobalTargetInfo[] targets)
        {
            base.ModifyTargets(ref targets);
            targets = PossibleTargets.Select(p => (GlobalTargetInfo) p).ToArray();
        }

        public override bool IsEnabledForPawn(out string reason)
        {
            if (!base.IsEnabledForPawn(out reason)) return false;
            if (PossibleTargets.Any()) return true;
            reason = "VFEAncients.NoTargets".Translate();
            return false;
        }

        public static IEnumerable<BodyPartRecord> AllLimbs(Pawn pawn) =>
            pawn.health.hediffSet.GetNotMissingParts(tag: BodyPartTagDefOf.MovingLimbCore)
                .Concat(pawn.health.hediffSet.GetNotMissingParts(tag: BodyPartTagDefOf.ManipulationLimbCore));
    }
}