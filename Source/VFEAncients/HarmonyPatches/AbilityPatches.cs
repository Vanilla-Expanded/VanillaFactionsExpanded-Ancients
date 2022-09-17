using System.Collections.Generic;
using System.Linq;
using HarmonyLib;
using RimWorld.Planet;
using Verse;
using VFECore.Abilities;

namespace VFEAncients.HarmonyPatches
{
    public static class AbilityPatches
    {
        public static void Do(Harmony harm)
        {
            harm.Patch(AccessTools.Method(typeof(Pawn), nameof(Pawn.GetGizmos)), postfix: new HarmonyMethod(typeof(AbilityPatches), nameof(AddGizmos)));
            harm.Patch(AccessTools.Method(typeof(Caravan_CarryTracker), nameof(Caravan_CarryTracker.WantsToBeCarried)),
                postfix: new HarmonyMethod(typeof(AbilityPatches), nameof(WantsToBeCarriedPostfix)));
        }

        public static IEnumerable<Gizmo> AddGizmos(IEnumerable<Gizmo> gizmos, Pawn __instance)
        {
            foreach (var gizmo in gizmos) yield return gizmo;

            if (__instance.IsColonist && !__instance.IsColonistPlayerControlled && __instance.TryGetComp<CompAbilities>(out var comp))
                foreach (var ability in comp.LearnedAbilities.Where(ability => ability.ShowGizmoOnPawn()))
                    yield return ability.GetGizmo();
        }

        public static void WantsToBeCarriedPostfix(Pawn p, ref bool __result)
        {
            if (p.HasPower(VFEA_DefOf.PsychologicalParalysis)) __result = true;
        }
    }
}