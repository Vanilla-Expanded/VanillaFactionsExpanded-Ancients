using System;
using System.Collections.Generic;
using System.Linq;
using RimWorld;
using Verse;

namespace VFEAncients
{
    public class CompSupplySlingshot : ThingComp
    {
        private static ValueTuple<Map, Designator_Build> designator;
        private CompTransporter cachedCompTransporter;

        public CompTransporter Transporter => cachedCompTransporter ??= parent.GetComp<CompTransporter>();

        public virtual int TicksToReturn => 60000 * 7;

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
                Current.Game.GetComponent<GameComponent_Ancients>().SlingshotQueue.Enqueue(new GameComponent_Ancients.SlingshotInfo
                {
                    Cell = parent.Map.listerThings
                        .ThingsOfDef(VFEA_DefOf.VFEA_SlingshotDropOffSpot)?
                        .OrderBy(t => t.Position.DistanceTo(parent.Position))
                        .FirstOrDefault()?.Position ?? parent.Position,
                    Map = parent.Map,
                    ReturnTick = Find.TickManager.TicksGame + TicksToReturn,
                    Wealth = compTransporter.innerContainer.Sum(item => item.MarketValue * item.stackCount)
                });
                compTransporter.innerContainer.ClearAndDestroyContents();
                compTransporter.CancelLoad(parent.Map);
                SkyfallerMaker.SpawnSkyfaller(VFEA_DefOf.VFEA_SupplyCrateLeaving, compTransporter.parent.Position, parent.Map);
            }
        }
    }
}