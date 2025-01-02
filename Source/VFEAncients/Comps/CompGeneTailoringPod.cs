using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using RimWorld;
using UnityEngine;
using Verse;
using Verse.AI;
using VFECore.OptionalFeatures;

namespace VFEAncients
{
    [StaticConstructorOnStartup]
    public class CompGeneTailoringPod : ThingComp, IThingHolder, ISuspendableThingHolder, IThingHolderWithDrawnPawn
    {
        public static Material BackgroundMat = SolidColorMaterials.SimpleSolidColorMaterial(new Color(0.082f, 0.078f, 0.063f));
        public static Texture2D StartOperationTex = ContentFinder<Texture2D>.Get("UI/Gizmos/GeneTailoringInitiate");
        private readonly List<Operation> possibleOperations;
        private Operation currentOperation;
        private ThingOwner innerContainer;

        private static List<ThingDef> cachedPods;

        private int ticksTillDone = -1;

        public CompGeneTailoringPod()
        {
            innerContainer = new ThingOwner<Thing>(this);
            possibleOperations = typeof(Operation).AllSubclassesNonAbstract().Select(opType => (Operation) Activator.CreateInstance(opType, this)).ToList();
        }

        public bool PowerOn => !parent.TryGetComp<CompPowerTrader>(out var comp) || comp.PowerOn;
        public bool HasFuel => !parent.TryGetComp<CompRefuelable>(out var comp) || comp.HasFuel;

        public Pawn Occupant => innerContainer.OfType<Pawn>().FirstOrDefault();

        public string TimeExplain =>
            VFEA_DefOf.VFEA_InjectingTimeFactor.Worker.GetExplanationFull(new StatRequest {thingInt = parent, defInt = parent.def}, ToStringNumberSense.Absolute,
                parent.GetStatValue(VFEA_DefOf.VFEA_InjectingTimeFactor)).TrimEmptyLines().TrimEndNewlines().TrimLines(1, 1);

        public bool IsContentsSuspended => true;

        public void GetChildHolders(List<IThingHolder> outChildren)
        {
            ThingOwnerUtility.AppendThingHoldersFromThings(outChildren, GetDirectlyHeldThings());
        }

        public ThingOwner GetDirectlyHeldThings() => innerContainer;

        public float HeldPawnDrawPos_Y => parent.DrawPos.y - Altitudes.AltInc;
        public float HeldPawnBodyAngle => parent.Rotation.Opposite.AsAngle;

        public PawnPosture HeldPawnPosture => PawnPosture.LayingOnGroundFaceUp;

        public string FailChanceExplain(Operation op)
        {
            if (Occupant.GetPowerTracker()?.HasPower(VFEA_DefOf.PromisingCandidate) ?? false) return $"\n{VFEA_DefOf.PromisingCandidate.LabelCap}: 0%";
            var builder = new StringBuilder();
            builder.AppendLine();
            builder.AppendLine(VFEA_DefOf.VFEA_FailChance.Worker.GetExplanationFull(new StatRequest {thingInt = parent, defInt = parent.def}, ToStringNumberSense.Absolute,
                parent.GetStatValue(VFEA_DefOf.VFEA_FailChance)).TrimEndNewlines().TrimLines(0, op is not null ? 1 : 0));
            if (op is not null)
            {
                builder.AppendLine(op.FailChanceExplainOnPawn(Occupant));
                builder.Append("StatsReport_FinalValue".Translate() + ": " + op.FailChanceOnPawn(Occupant).ToStringByStyle(ToStringStyle.PercentZero));
            }

            return builder.ToString().TrimEmptyLines().TrimEndNewlines();
        }

        public virtual void StartOperation(Operation op)
        {
            // Check if operation still valid - handle situations where a machine was turned off with
            // the start operation float menu open, allowing to still select the start operation option.
            if (!op.CanRunOnPawn(Occupant))
            {
                Messages.Message("CannotUseReason".Translate("VFEAncient.AvailableOperationsCannotBeApplied".Translate()), parent, MessageTypeDefOf.RejectInput, false);
                return;
            }

            currentOperation = op;
            parent.GetComp<CompRefuelable>()?.ConsumeFuel(1f);
            var allPods = parent.Map.listerThings.ThingsOfDef(parent.def);

            ticksTillDone = currentOperation.StartOnPawnGetDuration() + allPods.IndexOf(parent);
        }

        public override void CompTick()
        {
            base.CompTick();
            if (ticksTillDone > 0 && currentOperation != null)
            {
                ticksTillDone--;
                if (!PowerOn)
                {
                    currentOperation.Failure();
                    currentOperation = null;
                    ticksTillDone = -1;
                }

                if (ticksTillDone == 0) CompleteOperation();
            }
        }

        public virtual void CompleteOperation()
        {
            if (!(Occupant?.GetPowerTracker()?.HasPower(VFEA_DefOf.PromisingCandidate) ?? false) && Rand.Chance(currentOperation.FailChanceOnPawn(Occupant)))
                currentOperation.Failure();
            else currentOperation.Success();
            currentOperation = null;
            ticksTillDone = -1;
        }

        public override void PostDestroy(DestroyMode mode, Map previousMap)
        {
            currentOperation?.Failure();
            if (mode is DestroyMode.Deconstruct or DestroyMode.KillFinalize) EjectContents(previousMap);
            innerContainer.ClearAndDestroyContents();
            base.PostDestroy(mode, previousMap);
        }

        public override IEnumerable<Gizmo> CompGetGizmosExtra()
        {
            if (innerContainer.Any)
            {
                foreach (var thing in innerContainer)
                {
                    yield return Building.SelectContainedItemGizmo(parent, thing);
                }
            }

            if (Occupant != null && HasFuel && PowerOn && ticksTillDone <= 0 && currentOperation == null)
            {
                yield return new Command_Action
                {
                    action = () =>
                    {
                        var options = possibleOperations.Where(op => op.CanRunOnPawn(Occupant))
                            .Select(op => new FloatMenuOption(op.Label,
                                () => Find.WindowStack.Add(Dialog_MessageBox.CreateConfirmation("VFEAncients.ExperimentFailureWarning".Translate(
                                        (Occupant?.GetPowerTracker()?.HasPower(VFEA_DefOf.PromisingCandidate) ?? false ? 0f : op.FailChanceOnPawn(Occupant)).ToStringPercent()
                                        .Colorize(ColoredText.ThreatColor),
                                        parent.def.GetCompProperties<CompProperties_AffectedByFacilities>().linkableFacilities.Where(def =>
                                                def.GetCompProperties<CompProperties_Facility>()?.statOffsets?.Any(statMod => statMod.stat == VFEA_DefOf.VFEA_FailChance) ?? false)
                                            .Select(def => def.label).ToLineList("  - "), op.TicksRequired.ToStringTicksToPeriodVerbose().Colorize(ColoredText.DateTimeColor),
                                        TimeExplain, FailChanceExplain(op)),
                                    () => StartOperation(op), true)))).ToList();
                        // Handle situations where one of the machines was turned off after
                        // a pawn entered the pod, ending in situation with no valid operations.
                        if (!options.Any())
                            options.Add(new FloatMenuOption("CannotUseReason".Translate("VFEAncient.AvailableOperationsCannotBeApplied".Translate()), null));
                        Find.WindowStack.Add(new FloatMenu(options));
                    },
                    defaultLabel = "VFEAncients.StartOperation".Translate(),
                    icon = StartOperationTex
                };
                yield return new Command_Action
                {
                    icon = CompBiosculpterPod.InterruptCycleIcon,
                    action = () => EjectContents(),
                    defaultLabel = "VFEAncients.CancelOp".Translate(),
                    defaultDesc = "VFEAncients.CancelOpDesc".Translate(Occupant.Named("PAWN"))
                };
            }

            if (currentOperation != null && DebugSettings.ShowDevGizmos)
            {
                yield return new Command_Action
                {
                    action = CompleteOperation,
                    defaultLabel = "DEV: Complete Instantly",
                    defaultDesc = "Instantly advance to complete, and roll for failure"
                };
                yield return new Command_Action
                {
                    action = () =>
                    {
                        currentOperation.Success();
                        currentOperation = null;
                        ticksTillDone = -1;
                    },
                    defaultLabel = "DEV: Succeed Instantly",
                    defaultDesc = "Instantly succeed, ignoring success chance"
                };
                yield return new Command_Action
                {
                    action = () =>
                    {
                        currentOperation.Failure();
                        currentOperation = null;
                        ticksTillDone = -1;
                    },
                    defaultLabel = "DEV: Fail Instantly",
                    defaultDesc = "Instantly fail, ignoring success chance"
                };
            }
        }

        public override IEnumerable<FloatMenuOption> CompFloatMenuOptions(Pawn selPawn)
        {
            if (selPawn.IsQuestLodger())
                yield return new FloatMenuOption("CannotUseReason".Translate("CryptosleepCasketGuestsNotAllowed".Translate()), null);
            else if (!selPawn.CanReach(parent, PathEndMode.InteractionCell, Danger.Deadly))
                yield return new FloatMenuOption("CannotUseNoPath".Translate(), null);
            else
            {
                if (!PowerOn)
                {
                    yield return new FloatMenuOption("CannotUseNoPower".Translate(), null);
                }
                else if (!HasFuel)
                {
                    yield return new FloatMenuOption("CannotUseReason".Translate("VFEAncient.GeneTailorNoFuel".Translate()), null);
                }
                else if (!Pawn_PowerTracker.CanGetPowers(selPawn))
                {
                    yield return new FloatMenuOption("CannotUseReason".Translate("VFEAncient.MustBeHumanlike".Translate()), null);
                }
                else if (Occupant != null)
                {
                    yield return new FloatMenuOption("CannotUseReason".Translate("VFEAncient.GeneTailorOccupied".Translate()), null);
                }
                else if (!possibleOperations.Any(op => op.CanRunOnPawn(selPawn)))
                {
                    yield return new FloatMenuOption("CannotUseReason".Translate("VFEAncient.AvailableOperationsCannotBeApplied".Translate()), null);
                }
                else
                {
                    yield return FloatMenuUtility.DecoratePrioritizedTask(
                        new FloatMenuOption("VFEAncients.EnterGeneTailoringPod".Translate(),
                            () => { selPawn.jobs.TryTakeOrderedJob(JobMaker.MakeJob(VFEA_DefOf.VFEA_EnterGeneTailoringPod, parent), JobTag.Misc); }), selPawn, parent);
                }
            }
        }

        public static void AddCarryToPodJobs(List<FloatMenuOption> opts, Pawn pawn, Pawn target)
        {
            if (!pawn.CanReserveAndReach(target, PathEndMode.OnCell, Danger.Deadly, 1, -1, null, true)) return;
            foreach (var thing in FindPodsFor(pawn, target))
            {
                string text = "VFEAncients.CarryToGeneTailoringPod".Translate(target);
                if (target.IsQuestLodger())
                {
                    text += " (" + "CryptosleepCasketGuestsNotAllowed".Translate() + ")";
                    opts.Add(FloatMenuUtility.DecoratePrioritizedTask(new FloatMenuOption(text, null), pawn, thing));
                }
                else if (target.GetExtraHostFaction() != null)
                {
                    text += " (" + "CryptosleepCasketGuestPrisonersNotAllowed".Translate() + ")";
                    opts.Add(FloatMenuUtility.DecoratePrioritizedTask(new FloatMenuOption(text, null), pawn, thing));
                }
                else if (thing.TryGetComp<CompGeneTailoringPod>().CanAccept(target))
                    opts.Add(FloatMenuUtility.DecoratePrioritizedTask(new FloatMenuOption(text, () =>
                    {
                        var job = JobMaker.MakeJob(VFEA_DefOf.VFEA_CarryToGeneTailoringPod, target, thing);
                        job.count = 1;
                        pawn.jobs.TryTakeOrderedJob(job, JobTag.Misc);
                    }), pawn, target));
            }
        }

        public static IEnumerable<Thing> FindPodsFor(Pawn pawn, Pawn target)
        {
            if (cachedPods == null)
            {
                cachedPods = DefDatabase<ThingDef>.AllDefs.Where(def => def.comps.Any(comp => comp.compClass == typeof(CompGeneTailoringPod))).ToList();
            }
            List<Thing> availablePods = new List<Thing>();
            for (int i = 0; i < cachedPods.Count; i++)
            {
                ThingDef def = cachedPods[i];

                var pods = pawn.Map.listerThings.ThingsOfDef(def);
                int podsCount = pods.Count;
                if (podsCount == 0)
                    continue;

                Thing pod = GenClosest.ClosestThingReachable(target.Position, pawn.Map, ThingRequest.ForDef(def), PathEndMode.InteractionCell,
                    TraverseParms.For(pawn), 9999f, pod => pod.TryGetComp<CompGeneTailoringPod>().CanAccept(pawn));
                if (pod != null)
                {
                    availablePods.Add(pod);
                }
 
            }

        return availablePods;
          
        }

        public override void PostDraw()
        {
            base.PostDraw();
            var s = new Vector3(parent.def.graphicData.drawSize.x * 0.6f, 1f, parent.def.graphicData.drawSize.y * 0.6f);
            var drawPos = parent.DrawPos;
            drawPos.y -= Altitudes.AltInc * 2;
            Graphics.DrawMesh(MeshPool.plane10, Matrix4x4.TRS(drawPos, parent.Rotation.AsQuat, s), BackgroundMat, 0);
            if (Occupant != null)
            {
                var drawLoc = parent.DrawPos;
                drawLoc.y -= Altitudes.AltInc;
                if (parent.Rotation == Rot4.South) drawLoc.z -= 0.1f;
                if (parent.Rotation == Rot4.West || parent.Rotation == Rot4.East) drawLoc.z += 0.1f;
                if (parent.Rotation == Rot4.West) drawLoc.x -= 0.3f;
                if (parent.Rotation == Rot4.East) drawLoc.x += 0.3f;
                Occupant.Drawer.renderer.RenderPawnAt(drawLoc, parent.Rotation, true);
            }
        }

        public bool CanAccept(Pawn pawn)
        {
            return PowerOn && HasFuel && Pawn_PowerTracker.CanGetPowers(pawn) && Occupant == null && possibleOperations.Any(op => op.CanRunOnPawn(pawn));
        }

        public void TryAcceptPawn(Pawn pawn)
        {
            if (!CanAccept(pawn)) return;

            if (pawn.Spawned) pawn.DeSpawn();

            if (pawn.holdingOwner != null)
                pawn.holdingOwner.TryTransferToContainer(pawn, innerContainer);
            else
                innerContainer.TryAdd(pawn);
        }

        public void EjectContents(Map destMap = null)
        {
            if (destMap == null) destMap = parent.Map;
            innerContainer.TryDropAll(parent.InteractionCell, destMap ?? parent.Map, ThingPlaceMode.Near);
        }

        public override void PostExposeData()
        {
            base.PostExposeData();
            Scribe_Deep.Look(ref currentOperation, "currentOperation", this);
            Scribe_Values.Look(ref ticksTillDone, "ticksTillDone");
            Scribe_Deep.Look(ref innerContainer, "innerContainer", this);
        }

        public override string CompInspectStringExtra()
        {
            var stringBuilder = new StringBuilder();
            stringBuilder.AppendLineIfNotEmpty().Append(base.CompInspectStringExtra()).AppendLineIfNotEmpty();
            if (parent.Spawned)
            {
                if (Occupant != null) stringBuilder.AppendLineIfNotEmpty().Append("Contains".Translate()).Append(": ").Append(Occupant.NameShortColored.Resolve());

                if (currentOperation != null)
                    stringBuilder.AppendLineIfNotEmpty().Append("VFEAncients.CurrentOperation".Translate()).Append(": ").Append(currentOperation.Label);
                if (ticksTillDone > 0)
                    stringBuilder.AppendLineIfNotEmpty().Append("BiosculpterCycleTimeRemaining".Translate()).Append(": ")
                        .Append(ticksTillDone.ToStringTicksToPeriodVerbose().Colorize(ColoredText.DateTimeColor));
            }

            return stringBuilder.Length <= 0 ? null : stringBuilder.ToString();
        }
    }
}