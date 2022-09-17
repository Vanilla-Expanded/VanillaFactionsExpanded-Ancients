using System.Collections.Generic;
using RimWorld;
using UnityEngine;
using Verse;

namespace VFEAncients
{
    [StaticConstructorOnStartup]
    public class Building_TurretPD : Building_TurretGun
    {
        public static Texture2D FireAtPodsTex = ContentFinder<Texture2D>.Get("UI/Gizmos/PDTarget_DropPods");
        public static Texture2D FireAtPawnsTex = ContentFinder<Texture2D>.Get("UI/Gizmos/PDTarget_Pawns");
        public static Texture2D FireAtProjectilesTex = ContentFinder<Texture2D>.Get("UI/Gizmos/PDTarget_Projectiles");

        public PDOpts Opts = new()
        {
            AtPawns = true,
            AtPods = true,
            AtProjectiles = true
        };

        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Deep.Look(ref Opts, "pdOpts");
        }

        public override IEnumerable<Gizmo> GetGizmos()
        {
            foreach (var gizmo in base.GetGizmos()) yield return gizmo;
            if (Faction == Faction.OfPlayer)
            {
                yield return new Command_Toggle
                {
                    defaultLabel = "VFEAncients.FireAtPods".Translate(),
                    defaultDesc = "VFEAncients.FireAtPodsDesc".Translate(),
                    icon = FireAtPodsTex,
                    isActive = () => Opts.AtPods,
                    toggleAction = () => Opts.AtPods = !Opts.AtPods
                };
                yield return new Command_Toggle
                {
                    defaultLabel = "VFEAncients.FireAtPawns".Translate(),
                    defaultDesc = "VFEAncients.FireAtPawnsDesc".Translate(),
                    icon = FireAtPawnsTex,
                    isActive = () => Opts.AtPawns,
                    toggleAction = () => Opts.AtPawns = !Opts.AtPawns
                };
                yield return new Command_Toggle
                {
                    defaultLabel = "VFEAncients.FireAtProjectiles".Translate(),
                    defaultDesc = "VFEAncients.FireAtProjectilesDesc".Translate(),
                    icon = FireAtProjectilesTex,
                    isActive = () => Opts.AtProjectiles,
                    toggleAction = () => Opts.AtProjectiles = !Opts.AtProjectiles
                };
            }
        }

        public struct PDOpts : IExposable
        {
            public bool AtPawns;
            public bool AtProjectiles;
            public bool AtPods;

            public void ExposeData()
            {
                Scribe_Values.Look(ref AtPawns, "atPawns", true);
                Scribe_Values.Look(ref AtPods, "atPods", true);
                Scribe_Values.Look(ref AtProjectiles, "atProjectiles", true);
            }
        }
    }
}