using System;
using System.Collections.Generic;
using System.Linq;
using LudeonTK;
using Verse;

namespace VFEAncients;

public static class DebugActionsPowers
{
    [DebugAction("Pawns", actionType = DebugActionType.Action, allowedGameStates = AllowedGameStates.PlayingOnMap)]
    public static List<DebugActionNode> GivePower()
    {
        var list = new List<DebugActionNode>
        {
            new("*Fill", DebugActionType.ToolMap, AddPowers(FillPowers(PowerType.Superpower).And(FillPowers(PowerType.Weakness)))),
            new("*Fill Superpowers", DebugActionType.ToolMap, AddPowers(FillPowers(PowerType.Superpower))),
            new("*Fill Weaknesses", DebugActionType.ToolMap, AddPowers(FillPowers(PowerType.Weakness)))
        };
        list.AddRange(DefDatabase<PowerDef>.AllDefs.OrderBy(def => def.label)
           .Select(def =>
                new DebugActionNode(def.label, DebugActionType.ToolMap, AddPowers(tracker => tracker.AddPower(def)))));
        return list;
    }

    private static Action AddPowers(Action<Pawn_PowerTracker> add)
    {
        return () =>
        {
            foreach (var tracker in from p in Find.CurrentMap.thingGrid.ThingsAt(UI.MouseCell()).OfType<Pawn>()
                     let tracker = p.GetPowerTracker()
                     where tracker != null
                     select tracker) add(tracker);
        };
    }

    private static Action<Pawn_PowerTracker> FillPowers(PowerType type)
    {
        return tracker =>
        {
            var count = ITab_Pawn_Powers.MaxPowers - tracker.AllPowers.Count(p => p.powerType == type);
            for (var i = 0; i < count; i++)
                if (DefDatabase<PowerDef>.AllDefs.Where(power => power.powerType == type && !tracker.HasPower(power))
                   .TryRandomElement(out var weak))
                    tracker.AddPower(weak);
        };
    }

    private static Action<T> And<T>(this Action<T> action1, Action<T> action2)
    {
        return obj =>
        {
            action1(obj);
            action2(obj);
        };
    }
}
