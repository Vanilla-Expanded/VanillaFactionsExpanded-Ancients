using System.Collections.Generic;
using System.Linq;
using System.Reflection.Emit;
using HarmonyLib;
using RimWorld;
using UnityEngine;
using Verse;

namespace VFEAncients.HarmonyPatches
{
    public static class StorytellerPatches
    {
        public static void Do(Harmony harm)
        {
            harm.Patch(AccessTools.Method(typeof(InteractionWorker_RecruitAttempt), nameof(InteractionWorker_RecruitAttempt.Interacted)),
                transpiler: new HarmonyMethod(typeof(StorytellerPatches), nameof(IncreaseRecruitDifficulty)));
            harm.Patch(AccessTools.Method(typeof(SkillRecord), nameof(SkillRecord.Interval)), new HarmonyMethod(typeof(StorytellerPatches), nameof(NoSkillDecay)));
            harm.Patch(AccessTools.Method(typeof(IncidentWorker), nameof(IncidentWorker.CanFireNow)),
                new HarmonyMethod(typeof(StorytellerPatches), nameof(AdditionalIncidentReqs)));
            harm.Patch(AccessTools.Method(typeof(IncidentWorker_PawnsArrive), nameof(IncidentWorker_PawnsArrive.FactionCanBeGroupSource)),
                null, new HarmonyMethod(typeof(StorytellerPatches), nameof(AncientsShouldNotArrive)));
        }

        public static IEnumerable<CodeInstruction> IncreaseRecruitDifficulty(IEnumerable<CodeInstruction> instructions, ILGenerator generator)
        {
            var instructionsList = instructions.ToList();

            var fGuest = AccessTools.Field(
                typeof(Pawn),
                nameof(Pawn.guest)
            );
            var fResistance = AccessTools.Field(
                typeof(Pawn_GuestTracker),
                nameof(Pawn_GuestTracker.resistance)
            );

            var mMin = AccessTools.Method(
                typeof(Mathf),
                nameof(Mathf.Min),
                new[] {typeof(float), typeof(float)}
            );

            var pStoryTeller = AccessTools.PropertyGetter(
                typeof(Find),
                nameof(Find.Storyteller)
            );

            var mTryGetStorytellerComp = AccessTools.Method(
                typeof(Utils),
                nameof(Utils.TryGetComp),
                new[] {typeof(Storyteller)}, new[] {typeof(StorytellerComp_IncreaseRecruitDifficulty)}
            );

            for (var i = 0; i < instructionsList.Count; i++)
            {
                var curInstr = instructionsList[i];

                // Apply a factor of 0.2 to the calculated resistance reduction if the current
                // storyteller has the increased recruitment difficulty tenet.
                // NB.: Interacted() will limit this calculated reduction to not exceed the remaining
                // resistance of the prisoner, so ensure this factor is applied before that,
                // to avoid a situation where it becomes impossible to reduce the resistance to 0.
                if (
                    curInstr.IsLdloc() &&
                    instructionsList[i + 1].IsLdarg() &&
                    instructionsList[i + 2].LoadsField(fGuest) &&
                    instructionsList[i + 3].LoadsField(fResistance) &&
                    instructionsList[i + 4].Calls(mMin)
                )
                {
                    var doesNotHaveIncreasedRecruitDifficulty = generator.DefineLabel();
                    curInstr.labels.Add(doesNotHaveIncreasedRecruitDifficulty);

                    yield return new CodeInstruction(
                        OpCodes.Call,
                        pStoryTeller
                    );
                    yield return new CodeInstruction(
                        OpCodes.Call,
                        mTryGetStorytellerComp
                    );
                    yield return new CodeInstruction(
                        OpCodes.Brfalse_S,
                        doesNotHaveIncreasedRecruitDifficulty
                    );
                    yield return new CodeInstruction(
                        OpCodes.Ldloc,
                        curInstr.operand
                    );
                    yield return new CodeInstruction(
                        OpCodes.Ldc_R4,
                        5f
                    );
                    yield return new CodeInstruction(OpCodes.Div);
                    yield return new CodeInstruction(
                        OpCodes.Stloc,
                        curInstr.operand
                    );
                }

                yield return curInstr;
            }
        }

        public static bool NoSkillDecay() => Find.Storyteller.TryGetComp<StorytellerComp_NoSkilLDecay>() == null;

        public static bool AdditionalIncidentReqs(IncidentWorker __instance, IncidentParms parms, ref bool __result)
        {
            if (parms.forced) return true;
            if (Faction.OfPlayer.ideos.PrimaryIdeo is not { } ideo) return true;
            if(__instance.def != IncidentDefOf.WandererJoin && __instance.def != VFEA_DefOf.WandererJoinAbasia) return true;
            if (ideo.precepts.SelectMany(precept => precept.def.comps).OfType<PreceptComp_DisableIncident>()
                .Any(disableIncident => disableIncident.Incident == __instance.def)) return __result = false;

            return true;
        }

        public static void AncientsShouldNotArrive(ref bool __result, Faction f, Map map, bool desperate = false)
        {
            if (f != null && f.def == VFEA_DefOf.VFEA_AncientSoldiers) __result = false;
        }
    }
}