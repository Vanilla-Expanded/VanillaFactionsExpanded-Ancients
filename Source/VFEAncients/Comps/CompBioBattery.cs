using System;
using System.Collections.Generic;
using System.Linq;
using RimWorld;
using UnityEngine;
using Verse;
using Verse.AI;

namespace VFEAncients
{
    [StaticConstructorOnStartup]
    public class CompBioBattery : CompPowerPlant, IThingHolderWithDrawnPawn, ISuspendableThingHolder
    {
        public static Material TopMat = MaterialPool.MatFrom("Things/Building/Power/AncientBioBattery/BioBattery_Top");
        private ThingOwner innerContainer;
        private float massLeft = -1;
        private int ticksTillConsume = -1;
        private static List<ThingDef> cachedBatteries;

        public CompBioBattery() => innerContainer = new ThingOwner<Thing>(this);

        public Pawn Occupant => innerContainer.OfType<Pawn>().FirstOrDefault();
        public override float DesiredPowerOutput => Occupant is null ? 0 : base.DesiredPowerOutput;

        public bool IsContentsSuspended => true;

        public void GetChildHolders(List<IThingHolder> outChildren)
        {
            ThingOwnerUtility.AppendThingHoldersFromThings(outChildren, GetDirectlyHeldThings());
        }

        public ThingOwner GetDirectlyHeldThings() => innerContainer;

        public float HeldPawnDrawPos_Y => parent.def.altitudeLayer.AltitudeFor(Altitudes.AltInc);
        public float HeldPawnBodyAngle => Rot4.North.AsAngle;
        public PawnPosture HeldPawnPosture => PawnPosture.LayingOnGroundFaceUp;

        public override void CompTick()
        {
            base.CompTick();
            ticksTillConsume--;
            if (ticksTillConsume == 0)
            {
                massLeft--;
                ticksTillConsume = 2500;
            }

            if (massLeft <= 0f)
            {
                innerContainer.ClearAndDestroyContents();
                ticksTillConsume = -1;
                massLeft = -1;
            }
        }

        public void InsertPawn(Pawn pawn)
        {
            innerContainer.TryAddOrTransfer(pawn, false);
            massLeft = pawn.GetStatValue(StatDefOf.Mass);
            ticksTillConsume = 2500;
            while (HealthUtility.FixWorstHealthCondition(pawn) != null);
            if (pawn.Downed)
            {
                var hediffs = pawn.health?.hediffSet?.hediffs;
                if (hediffs != null)
                {
                    for (var i = hediffs.Count - 1; i >= 0; i--)
                    {
                        var capMod = hediffs[i].CapMods?.FirstOrDefault(x => x.capacity == PawnCapacityDefOf.Consciousness);
                        if (capMod != null)
                        {
                            if (capMod.setMax < 1 || capMod.offset < 0)
                            {
                                pawn.health.RemoveHediff(hediffs[i]);
                            }
                        }
                    }
                }
            }
        }

        public virtual bool CanAcceptPawn(Pawn pawn) => Occupant is null;

        public static void AddCarryToBatteryJobs(List<FloatMenuOption> opts, Pawn pawn, Pawn target)
        {
            if (!pawn.CanReserveAndReach(target, PathEndMode.OnCell, Danger.Deadly, 1, -1, null, true)) return;
            var label = "";
            Action action = null;
            foreach (var thing in BatteriesFor(pawn, target))
            {
                label = "VFEAncients.CarryToBioBattery".Translate(target);
                if (!thing.TryGetComp<CompBioBattery>(out var comp)) continue;
                if (target.IsQuestLodger())
                    label += " (" + "CryptosleepCasketGuestsNotAllowed".Translate() + ")";
                else if (target.GetExtraHostFaction() != null)
                    label += " (" + "CryptosleepCasketGuestPrisonersNotAllowed".Translate() + ")";
                else if (!comp.CanAcceptPawn(target))
                    label += " (" + "CryptosleepCasketOccupied".Translate() + ")";
                else
                {
                    var pod = thing;
                    action = () =>
                    {
                        var job = JobMaker.MakeJob(VFEA_DefOf.VFEA_CarryToBioBatteryTank, target, pod);
                        job.count = 1;
                        pawn.jobs.TryTakeOrderedJob(job, JobTag.Misc);
                    };
                    break;
                }
            }

            if (!label.NullOrEmpty()) opts.Add(FloatMenuUtility.DecoratePrioritizedTask(new FloatMenuOption(label, action), pawn, target));
        }

        public override void PostExposeData()
        {
            base.PostExposeData();
            Scribe_Values.Look(ref ticksTillConsume, "ticksTillConsume");
            Scribe_Values.Look(ref massLeft, "massLeft");
            Scribe_Deep.Look(ref innerContainer, "innerContainer", this);
        }

        public override string CompInspectStringExtra() => base.CompInspectStringExtra() + (Occupant is not null
            ? "\n" + "VFEAncients.MassLeft".Translate(massLeft.ToStringMass(), Occupant.GetStatValue(StatDefOf.Mass).ToStringMass(), (
                massLeft / Occupant.GetStatValue(StatDefOf.Mass)).ToStringPercent()) + "\n" + "Contains".Translate() + ": " + Occupant.NameShortColored.Resolve()
            : "");

        public static IEnumerable<Thing> BatteriesFor(Pawn pawn, Pawn target)
        {
            if (cachedBatteries == null)
            {
                cachedBatteries = DefDatabase<ThingDef>.AllDefs.Where(def => def.comps.Any(comp => comp.compClass == typeof(CompBioBattery))).ToList();
            }

            List<Thing> availableBatteries = new List<Thing>();
            for (int i = 0; i < cachedBatteries.Count; i++)
            {
                ThingDef def = cachedBatteries[i];

                var batteries = pawn.Map.listerThings.ThingsOfDef(def);
                int batteryCount = batteries.Count;
                if (batteryCount == 0)
                    continue;

                List<Thing> battery = pawn.Map.listerThings.ThingsOfDef(def).Where(thing => thing is not null && pawn.CanReach(thing, PathEndMode.InteractionCell, Danger.Some)).ToList();
                if (battery != null)
                {
                    availableBatteries.AddRange(battery);
                }

            }

            return availableBatteries;



         
                
        }

        public override void PostDraw()
        {
            base.PostDraw();
            var s = new Vector3(parent.def.graphicData.drawSize.x, 1f, parent.def.graphicData.drawSize.y);
            var drawPos = parent.DrawPos;
            drawPos.y += Altitudes.AltInc * 2;
            Graphics.DrawMesh(MeshPool.plane10, Matrix4x4.TRS(drawPos, parent.Rotation.AsQuat, s), TopMat, 0);
            if (Occupant is null) return;
            var drawLoc = parent.DrawPos;
            Occupant.Drawer.renderer.RenderPawnAt(drawLoc, Rot4.South, true);
        }
    }
}