using System.Linq;
using RimWorld;
using Verse;

namespace VFEAncients
{
    public class CompSupplyCrate : ThingComp
    {
        private int ticksTillGone;
        public CompProperties_SupplyCrate Props => props as CompProperties_SupplyCrate;

        public override void ReceiveCompSignal(string signal)
        {
            base.ReceiveCompSignal(signal);
            if (signal == Building_Crate.CrateContentsChanged && parent is Building_Crate crate && !crate.HasAnyContents)
            {
                if (Rand.Chance(Props.raidChance))
                    IncidentDefOf.RaidEnemy.Worker.TryExecute(Find.Storyteller.storytellerComps
                        .First(x => x is StorytellerComp_OnOffCycle || x is StorytellerComp_RandomMain).GenerateParms(IncidentDefOf.RaidEnemy.category, parent.Map));
                parent.Destroy();
            }
        }

        public override void PostSpawnSetup(bool respawningAfterLoad)
        {
            base.PostSpawnSetup(respawningAfterLoad);
            ticksTillGone = Props.expiryTicks;
        }

        public override void CompTick()
        {
            base.CompTick();
            if (!parent.Spawned) return;
            ticksTillGone--;
            if (ticksTillGone > 0) return;
            GenExplosion.DoExplosion(parent.Position, parent.Map, parent.RotatedSize.x * parent.RotatedSize.z, DamageDefOf.Bomb, parent, parent.MaxHitPoints);
            parent.Destroy();
        }

        public override string CompInspectStringExtra()
        {
            return base.CompInspectStringExtra() + "VFEAncients.ExpiryIn".Translate(ticksTillGone.ToStringTicksToPeriodVerbose().Colorize(ColoredText.DateTimeColor));
        }

        public override void PostExposeData()
        {
            base.PostExposeData();
            Scribe_Values.Look(ref ticksTillGone, "ticksTillGone");
        }
    }

    public class CompProperties_SupplyCrate : CompProperties
    {
        public int expiryTicks;
        public float raidChance;

        public CompProperties_SupplyCrate()
        {
            compClass = typeof(CompSupplyCrate);
        }
    }
}