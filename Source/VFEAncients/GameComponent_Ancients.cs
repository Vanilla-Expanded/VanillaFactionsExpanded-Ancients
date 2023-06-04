using System;
using System.Collections.Generic;
using System.Linq;
using RimWorld;
using Verse;

namespace VFEAncients;

public class GameComponent_Ancients : GameComponent
{
    public List<SlingshotInfo> SlingshotQueue = new();

    public Dictionary<TickerType, List<(Pawn_PowerTracker, PowerDef)>> TickLists = new()
    {
        { TickerType.Normal, new List<(Pawn_PowerTracker, PowerDef)>() },
        { TickerType.Rare, new List<(Pawn_PowerTracker, PowerDef)>() },
        { TickerType.Long, new List<(Pawn_PowerTracker, PowerDef)>() }
    };

    public GameComponent_Ancients(Game game) { }

    public override void GameComponentTick()
    {
        base.GameComponentTick();

        if (Find.TickManager.TicksGame % 2000 == 0)
            foreach (var (tracker, power) in TickLists[TickerType.Long])
                try { power.Worker.TickLong(tracker); }
                catch (Exception e) { Log.Error($"Exception ticking power {power}: {e}"); }

        if (Find.TickManager.TicksGame % 250 == 0)
            foreach (var (tracker, power) in TickLists[TickerType.Rare])
                try { power.Worker.TickRare(tracker); }
                catch (Exception e) { Log.Error($"Exception ticking power {power}: {e}"); }

        foreach (var (tracker, power) in TickLists[TickerType.Normal])
            try { power.Worker.Tick(tracker); }
            catch (Exception e) { Log.Error($"Exception ticking power {power}: {e}"); }

        if (Find.TickManager.TicksGame % 500 == 0)
            SlingshotQueue.RemoveAll(info =>
            {
                if (info.ReturnTick > Find.TickManager.TicksGame) return false;

                if (info.Map == null || info.Map.Index < 0) info.Map = Find.AnyPlayerHomeMap;

                info.Cell = DropCellFinder.TryFindDropSpotNear(info.Cell, info.Map, out var cell, false, false, false, VFEA_DefOf.VFEA_SupplyCrateIncoming.size)
                    ? cell
                    : DropCellFinder.TradeDropSpot(info.Map);
                var things = ThingSetMakerDefOf.Reward_ItemsStandard.root.Generate(new ThingSetMakerParams
                    { podContentsType = PodContentsType.AncientFriendly, totalMarketValueRange = new FloatRange(info.Wealth, info.Wealth) });
                if (info.ForcedItems != null && info.ForcedItems.Any()) things.AddRange(info.ForcedItems.Select(t => t.MakeThing()));
                var skyfaller = SkyfallerMaker.SpawnSkyfaller(VFEA_DefOf.VFEA_SupplyCrateIncoming, things, info.Cell, info.Map);
                Messages.Message("VFEAncients.SupplyCrateArrived".Translate(), skyfaller, MessageTypeDefOf.PositiveEvent);
                return true;
            });
    }

    public override void ExposeData()
    {
        base.ExposeData();
        Scribe_Collections.Look(ref SlingshotQueue, "slingshotQueue", LookMode.Deep);
    }

    public class SlingshotInfo : IExposable
    {
        public IntVec3 Cell;
        public List<ThingDefStuffCount> ForcedItems;
        public Map Map;
        public int ReturnTick;
        public float Wealth;

        public void ExposeData()
        {
            Scribe_Values.Look(ref ReturnTick, "returnTick");
            Scribe_Values.Look(ref Wealth, "wealth");
            Scribe_References.Look(ref Map, "map");
            Scribe_Values.Look(ref Cell, "cell");
            Scribe_Collections.Look(ref ForcedItems, "forcedItems", LookMode.Deep);
        }
    }
}

public struct ThingDefStuffCount : IExposable
{
    private ThingDef thingDef;
    private ThingDef stuffDef;
    private int count;

    public ThingDef ThingDef => thingDef;
    public ThingDef StuffDef => stuffDef;
    public int Count => count;

    public ThingDefStuffCount(ThingDef thingDef, int count) : this(thingDef, null, count) { }

    public ThingDefStuffCount(ThingDef thingDef, ThingDef stuffDef, int count)
    {
        if (count < 0)
        {
            Log.Warning($"Tried to set {nameof(ThingDefStuffCount)} count to {count}. thingDef={thingDef}, stuffDef={stuffDef}");
            count = 0;
        }

        this.thingDef = thingDef;
        this.count = count;
        this.stuffDef = stuffDef;
    }

    public Thing MakeThing()
    {
        var thing = ThingMaker.MakeThing(ThingDef, StuffDef);
        if (Count >= 1)
            thing.stackCount = Count;
        return thing;
    }

    public void ExposeData()
    {
        Scribe_Defs.Look(ref thingDef, "thingDef");
        Scribe_Defs.Look(ref stuffDef, "stuffDef");
        Scribe_Values.Look(ref count, "count", 1);
    }
}
