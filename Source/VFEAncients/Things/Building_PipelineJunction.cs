using RimWorld;
using Verse;

namespace VFEAncients
{
    public class Building_PipelineJunction : Building_Casket
    {
        private int ticksTillRefill;
        public virtual int RefillTime => 7 * 60000;
        public virtual int Count => 250;
        public virtual ThingDef Def => ThingDefOf.Chemfuel;
        public override bool CanOpen => base.CanOpen && (GetComp<CompHackable>()?.IsHacked ?? true);

        public override void Open()
        {
            base.Open();
            ticksTillRefill = RefillTime;
        }

        public override void SpawnSetup(Map map, bool respawningAfterLoad)
        {
            base.SpawnSetup(map, respawningAfterLoad);
            if (!respawningAfterLoad) Refill();
        }

        public override void Tick()
        {
            base.Tick();
            if (ticksTillRefill > 0) ticksTillRefill--;
            if (ticksTillRefill == 0) Refill();
            var empty = GetComp<CompEmptyStateGraphic>();
        }

        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Values.Look(ref ticksTillRefill, "ticksTillRefill");
        }

        public virtual void Refill()
        {
            // var count = Count;
            // while (count > 0)
            // {
            //     var t = ThingMaker.MakeThing(Def);
            //     t.stackCount = Mathf.Clamp(count, 0, Def.stackLimit);
            //     count -= t.stackCount;
            //     TryAcceptThing(t, false);
            // }
            var thing = ThingMaker.MakeThing(Def);
            thing.stackCount = Count;
            TryAcceptThing(thing);
            var hackable = GetComp<CompHackable>();
            hackable?.Hack(-hackable.defence);
            ticksTillRefill = -1;
        }

        public override string GetInspectString()
        {
            return base.GetInspectString() + (ticksTillRefill > 0 ? "\n" + (string) "VFEAncients.RefillsIn".Translate(ticksTillRefill.ToStringTicksToPeriodVerbose()) : "");
        }
    }
}