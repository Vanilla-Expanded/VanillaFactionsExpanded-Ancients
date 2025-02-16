using System;
using System.Collections.Generic;
using System.Linq;
using RimWorld;
using UnityEngine;
using Verse;

namespace VFEAncients;

[StaticConstructorOnStartup]
public class CompSupplySlingshot : ThingComp
{
    private static ValueTuple<Map, Designator_Build> designator;
    private CompTransporter cachedCompTransporter;

    public CompTransporter Transporter => cachedCompTransporter ??= parent.GetComp<CompTransporter>();

    public virtual int TicksToReturn => GenDate.TicksPerDay * 7;

    // A dictionary that forces any item matching the key def to be returned back to the player,
    // multiplied the value (so it can return more, less, or nothing back).
    private static readonly Dictionary<ThingDef, float> ThingReturnCountMultiplier = new[]
        {
            // Return the same amount that was sent
            (DefDatabase<ThingDef>.GetNamedSilentFail("VRecyclingE_ReclaimedBiopack"), 1f),
            (DefDatabase<ThingDef>.GetNamedSilentFail("VRecyclingE_StabilizedAlloypack"), 1f),
            // Return 5% extra trash when sending it back
            (DefDatabase<ThingDef>.GetNamedSilentFail("VRecyclingE_Trash"), 1.05f),
            // Other packs will get handled automatically due to no market value.
        }
        .Where(x => x.Item1 != null)
        .ToDictionary(x => x.Item1, x => x.Item2);

    public override IEnumerable<Gizmo> CompGetGizmosExtra()
    {
        foreach (var gizmo in base.CompGetGizmosExtra()) yield return gizmo;

        if (Transporter.LoadingInProgressOrReadyToLaunch)
            yield return new Command_Action
            {
                defaultLabel = "VFEAncients.LaunchSupplies".Translate(),
                icon = CompLaunchable.LaunchCommandTex,
                alsoClickIfOtherInGroupClicked = false,
                action = delegate
                {
                    ConfirmIf(() => Transporter.innerContainer.Any(thing => thing is Pawn),
                        () => "VFEAncients.ConfirmSendPawn".Translate(Transporter.innerContainer.First(thing => thing is Pawn).Named("PAWN")), () =>
                            ConfirmIf(() => Transporter.AnyInGroupHasAnythingLeftToLoad,
                                () => "ConfirmSendNotCompletelyLoadedPods".Translate(Transporter.FirstThingLeftToLoadInGroup.LabelCapNoCount,
                                    Transporter.FirstThingLeftToLoadInGroup),
                                TryLaunch), true);
                }
            };

        if (designator.Item1 != Find.CurrentMap) designator = (Find.CurrentMap, new Designator_Build(VFEA_DefOf.VFEA_SlingshotDropOffSpot));
        yield return designator.Item2;
    }

    private static void ConfirmIf(Func<bool> predicate, Func<string> confirmStr, Action onConfirm, bool danger = false)
    {
        if (predicate())
            Find.WindowStack.Add(Dialog_MessageBox.CreateConfirmation(confirmStr(), onConfirm, danger));
        else
            onConfirm();
    }

    public void TryLaunch()
    {
        if (!parent.Spawned)
        {
            Log.Error("Tried to launch " + parent + ", but it's unspawned.");
            return;
        }

        var transportersInGroup = Transporter.TransportersInGroup(parent.Map);
        if (transportersInGroup == null)
        {
            Log.Error("Tried to launch " + parent + ", but it's not in any group.");
            return;
        }

        if (!Transporter.LoadingInProgressOrReadyToLaunch) return;
        Transporter.TryRemoveLord(parent.Map);
        foreach (var compTransporter in transportersInGroup.ListFullCopy())
        {
            var forcedItems = AddForcedItems(compTransporter.innerContainer);
            Current.Game.GetComponent<GameComponent_Ancients>().SlingshotQueue.Add(new GameComponent_Ancients.SlingshotInfo
            {
                Cell = parent.Map.listerThings
                   .ThingsOfDef(VFEA_DefOf.VFEA_SlingshotDropOffSpot)?
                   .OrderBy(t => t.Position.DistanceTo(parent.Position))
                   .FirstOrDefault()?.Position ?? parent.Position,
                Map = parent.Map,
                ReturnTick = Find.TickManager.TicksGame + TicksToReturn,
                Wealth = compTransporter.innerContainer.Sum(item => item.MarketValue * item.stackCount),
                ForcedItems = forcedItems
            });
            compTransporter.innerContainer.ClearAndDestroyContents();
            compTransporter.CancelLoad(parent.Map);
            SkyfallerMaker.SpawnSkyfaller(VFEA_DefOf.VFEA_SupplyCrateLeaving, compTransporter.parent.Position, parent.Map);
        }
    }

    // Make a method that will be easy to hook up into with harmony so other mods can go through and modify the contents,
    // as well as add forced rewards. If someone wants to, they could add a whole bartering system with this.
    private static List<ThingDefStuffCount> AddForcedItems(ThingOwner container)
    {
        var entries = new Dictionary<(ThingDef thing, ThingDef stuff), float>();

        for (var i = container.Count - 1; i >= 0; i--)
        {
            var item = container.GetAt(i);

            // Return predefined items with a specified multiplier
            if (ThingReturnCountMultiplier.TryGetValue(item.def, out var m))
                AddCurrentToReturned(m);
            // Return any item with no market value without modifying the stack count (minus rounding)
            else if (item.MarketValue <= 0)
                AddCurrentToReturned();

            void AddCurrentToReturned(float multiplier = 1.0f)
            {
                var key = (item.def, item.Stuff);
                var count = entries.TryGetValue(key) + item.stackCount * multiplier;
                entries[key] = count;

                container.RemoveAt(i);
            }
        }

        return entries
            .Select(x => new ThingDefStuffCount(x.Key.thing, x.Key.stuff, Mathf.Max(GenMath.RoundRandom(x.Value), 0)))
            .Where(x => x.Count > 0)
            .ToList();
    }
}