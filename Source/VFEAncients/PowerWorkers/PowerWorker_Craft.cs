using HarmonyLib;
using RimWorld;
using UnityEngine;
using Verse;
using VFEAncients.HarmonyPatches;

namespace VFEAncients
{
    public class PowerWorker_Craft : PowerWorker
    {
        public PowerWorker_Craft(PowerDef def) : base(def)
        {
        }

        public override void DoPatches(Harmony harm)
        {
            base.DoPatches(harm);
            harm.Patch(AccessTools.Method(typeof(QualityUtility), nameof(QualityUtility.GenerateQualityCreatedByPawn), new[] {typeof(Pawn), typeof(SkillDef)}),
                postfix: new HarmonyMethod(GetType(), nameof(AddLevels)));
        }

        public static void AddLevels(ref QualityCategory __result, Pawn pawn, SkillDef relevantSkill)
        {
            if (relevantSkill == SkillDefOf.Crafting && pawn.HasPower<PowerWorker_Craft>() && pawn.GetData<WorkerData_Craft>() is WorkerData_Craft data)
                __result = (QualityCategory) Mathf.Min((int) (__result + (byte) data.LevelBonus), 6);
        }
    }

    public class WorkerData_Craft : WorkerData
    {
        public int LevelBonus;
    }
}