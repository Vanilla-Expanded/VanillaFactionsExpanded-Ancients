using System.Collections.Generic;
using System.Linq;
using System.Reflection.Emit;
using HarmonyLib;
using RimWorld;
using Verse;
using Verse.AI;

namespace VFEAncients.HarmonyPatches
{
    public static class PreceptPatches
    {
        public static DefMap<PrisonerInteractionModeDef, HistoryEventDef> PrisonerHistory;

        static PreceptPatches()
        {
            LongEventHandler.ExecuteWhenFinished(() =>
            {
                PrisonerHistory = new DefMap<PrisonerInteractionModeDef, HistoryEventDef>();
                foreach (var def in DefDatabase<HistoryEventDef>.AllDefs)
                    if (def.TryGetModExtension<RelatedInteractionMode>(out var ext) && ext.related != null)
                        PrisonerHistory[ext.related] = def;
            });
        }

        public static void Do(Harmony harm)
        {
            harm.Patch(AccessTools.Method(typeof(Ideo), nameof(Ideo.MemberWillingToDo)), new HarmonyMethod(typeof(PreceptPatches), nameof(MemberWillingToDo_Prefix)));
            harm.Patch(AccessTools.Method(typeof(WorkGiver_Warden), "ShouldTakeCareOfPrisoner"),
                postfix: new HarmonyMethod(typeof(PreceptPatches), nameof(ShouldTakeCareOfPrisoner_Postfix)));
            harm.Patch(AccessTools.Method(typeof(ITab_Pawn_Visitor), "FillTab"), transpiler: new HarmonyMethod(typeof(PreceptPatches), nameof(FixInteractionList)));
            harm.Patch(AccessTools.Method(typeof(JobGiver_ReactToCloseMeleeThreat), "TryGiveJob"),
                postfix: new HarmonyMethod(typeof(PreceptPatches), nameof(NoFightingInterrogator)));
        }

        public static void NoFightingInterrogator(Pawn pawn, ref Job __result)
        {
            var threat = pawn?.mindState?.meleeThreat;
            if (__result != null && threat != null && pawn.IsPrisoner && pawn.HostFaction == threat.Faction && pawn.guest.interactionMode == VFEA_DefOf.VFEA_Interrogate &&
                threat.CurJobDef == VFEA_DefOf.VFEA_PrisonerInterrogate && RestraintsUtility.InRestraints(pawn) &&
                pawn.GetRoom() is {IsPrisonCell: true} room) __result = JobMaker.MakeJob(JobDefOf.FleeAndCower, room.Cells?.RandomElement() ?? pawn.Position, threat);
        }

        public static void ShouldTakeCareOfPrisoner_Postfix(Pawn warden, Thing prisoner, ref bool __result)
        {
            if (__result && prisoner is Pawn pawn && pawn.guest?.interactionMode != null && warden.Ideo != null)
            {
                if (PrisonerHistory[pawn.guest.interactionMode] is { } eventDef)
                {
                    var ev = new HistoryEvent(eventDef, warden.Named(HistoryEventArgsNames.Doer), pawn.Named(HistoryEventArgsNames.Victim),
                        pawn.Faction.Named(HistoryEventArgsNames.AffectedFaction));
                    if (!ev.DoerWillingToDo()) __result = false;
                    if (!ev.VictimWillingToDo()) __result = false;
                }

                if (pawn.guest.interactionMode.GetModExtension<RequirePrecept>() is {precept: var precept})
                    if (!warden.Ideo.HasPrecept(precept))
                        __result = false;
            }
        }

        public static bool MemberWillingToDo_Prefix(HistoryEvent ev, Ideo __instance, ref bool __result)
        {
            if (ev.def.TryGetModExtension<RequirePrecept>(out var ext) && ext.precept != null && !__instance.HasPrecept(ext.precept))
            {
                __result = false;
                return false;
            }

            return true;
        }

        public static bool InteractionValidOn(PrisonerInteractionModeDef def, Pawn pawn) =>
            new HistoryEvent(PrisonerHistory[def], pawn.Named(HistoryEventArgsNames.Victim)).VictimWillingToDo();

        public static IEnumerable<CodeInstruction> FixInteractionList(IEnumerable<CodeInstruction> instructions, ILGenerator generator)
        {
            var list = instructions.ToList();
            var info1 = AccessTools.Field(typeof(PrisonerInteractionModeDef), nameof(PrisonerInteractionModeDef.allowOnWildMan));
            var idx1 = list.FindIndex(ins => ins.LoadsField(info1));
            var labels1 = list[idx1 + 2].ExtractLabels();
            var label1 = (Label) list[idx1 + 1].operand;
            var label3 = generator.DefineLabel();
            list[idx1 + 2].labels.Add(label3);
            var selPawn = AccessTools.Method(typeof(ITab), "get_SelPawn");
            list.InsertRange(idx1 + 2, new[]
            {
                new CodeInstruction(OpCodes.Ldloc, 34).WithLabels(labels1),
                new CodeInstruction(OpCodes.Ldarg_0),
                new CodeInstruction(OpCodes.Call, selPawn),
                new CodeInstruction(OpCodes.Call, AccessTools.Method(typeof(PreceptPatches), nameof(InteractionValidOn))),
                new CodeInstruction(OpCodes.Brfalse, label1),
                new CodeInstruction(OpCodes.Ldarg_0),
                new CodeInstruction(OpCodes.Call, selPawn),
                new CodeInstruction(OpCodes.Callvirt, AccessTools.PropertyGetter(typeof(Thing), nameof(Thing.MapHeld))),
                new CodeInstruction(OpCodes.Ldloc, 34),
                new CodeInstruction(OpCodes.Call, AccessTools.Method(typeof(PreceptPatches), nameof(ColonyHasAnyWardenWithRequiredPrecept))),
                new CodeInstruction(OpCodes.Brfalse, label1)
            });
            var info2 = AccessTools.Field(typeof(Def), nameof(Def.description));
            var idx2 = list.FindIndex(ins => ins.LoadsField(info2));
            var label2 = generator.DefineLabel();
            list[idx2 - 1].labels.Add(label2);
            var label4 = generator.DefineLabel();
            var idx3 = list.FindLastIndex(idx2, ins => ins.Branches(out _));
            list[idx3].operand = label4;
            idx3 = list.FindLastIndex(idx3 - 1, ins => ins.Branches(out _));
            list[idx3].operand = label4;
            list.InsertRange(idx2 - 1, new[]
            {
                new CodeInstruction(OpCodes.Ldloc, 34).WithLabels(label4),
                new CodeInstruction(OpCodes.Ldsfld, AccessTools.Field(typeof(PrisonerInteractionModeDefOf), nameof(PrisonerInteractionModeDefOf.AttemptRecruit))),
                new CodeInstruction(OpCodes.Bne_Un, label2),
                new CodeInstruction(OpCodes.Ldarg_0),
                new CodeInstruction(OpCodes.Call, selPawn),
                new CodeInstruction(OpCodes.Callvirt, AccessTools.PropertyGetter(typeof(Thing), nameof(Thing.MapHeld))),
                new CodeInstruction(OpCodes.Call, AccessTools.Method(typeof(PreceptPatches), nameof(ColonyHasAnyWardenCapableOfRecruitment))),
                new CodeInstruction(OpCodes.Brtrue, label2),
                new CodeInstruction(OpCodes.Ldstr, "VFEAncients.MessageNoWardenCapableOfRecruitment"),
                new CodeInstruction(OpCodes.Call, AccessTools.Method(typeof(Translator), nameof(Translator.Translate), new[] {typeof(string)})),
                new CodeInstruction(OpCodes.Call, AccessTools.Method(typeof(TaggedString), "op_Implicit", new[] {typeof(TaggedString)})),
                new CodeInstruction(OpCodes.Ldarg_0),
                new CodeInstruction(OpCodes.Call, selPawn),
                new CodeInstruction(OpCodes.Newobj, AccessTools.Constructor(typeof(LookTargets), new[] {typeof(Thing)})),
                new CodeInstruction(OpCodes.Ldsfld, AccessTools.Field(typeof(MessageTypeDefOf), nameof(MessageTypeDefOf.CautionInput))),
                new CodeInstruction(OpCodes.Ldc_I4_0),
                new CodeInstruction(OpCodes.Call,
                    AccessTools.Method(typeof(Messages), nameof(Messages.Message), new[] {typeof(string), typeof(LookTargets), typeof(MessageTypeDef), typeof(bool)}))
            });
            return list;
        }

        private static bool ColonyHasAnyWardenCapableOfRecruitment(Map map)
        {
            return Enumerable.Any(map.mapPawns.FreeColonistsSpawned,
                pawn => pawn.workSettings.WorkIsActive(WorkTypeDefOf.Warden) &&
                        new HistoryEvent(PrisonerHistory[PrisonerInteractionModeDefOf.AttemptRecruit], pawn.Named(HistoryEventArgsNames.Doer)).DoerWillingToDo());
        }

        private static bool ColonyHasAnyWardenWithRequiredPrecept(Map map, PrisonerInteractionModeDef intDef)
        {
            var precept = PrisonerHistory[intDef]?.GetModExtension<RequirePrecept>()?.precept;
            return precept == null || Enumerable.Any(map.mapPawns.FreeColonistsSpawned,
                pawn => pawn.workSettings.WorkIsActive(WorkTypeDefOf.Warden) && pawn.Ideo != null && pawn.Ideo.HasPrecept(precept));
        }

        public static bool VictimWillingToDo(this HistoryEvent ev)
        {
            var pawn = ev.args.GetArg<Pawn>(HistoryEventArgsNames.Victim);
            return pawn?.Ideo == null || pawn.Ideo.PreceptsListForReading.SelectMany(precept => precept.def.comps).OfType<IVictimPreceptComp>().All(pc => pc.VictimWillingToDo(ev));
        }
    }
}