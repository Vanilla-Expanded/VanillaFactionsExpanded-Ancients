using HarmonyLib;
using RimWorld;
using Verse;
using VFEAncients.HarmonyPatches;

namespace VFEAncients;

public class PowerWorker_Resurrect : PowerWorker
{
    public const int TicksToResurrect = 180000;

    public PowerWorker_Resurrect(PowerDef def) : base(def) { }

    public override void DoPatches(Harmony harm)
    {
        base.DoPatches(harm);
        harm.Patch(AccessTools.Method(typeof(Corpse), nameof(Corpse.TickRare)), postfix: new(GetType(), nameof(CorpseTick)));
        harm.Patch(AccessTools.Method(typeof(Corpse), nameof(Corpse.GetInspectString)), postfix: new(GetType(), nameof(AddResInfo)));
    }

    public static void AddResInfo(Corpse __instance, ref string __result)
    {
        if (__instance.InnerPawn.HasPower<PowerWorker_Resurrect>())
            __result +=
                $"\n{"VFEAncients.ResurrectsIn".Translate()} {(TicksToResurrect - (Find.TickManager.TicksGame - __instance.timeOfDeath)).ToStringTicksToPeriodVerbose().Colorize(ColoredText.DateTimeColor)}";
    }

    public static void CorpseTick(Corpse __instance)
    {
        var pawn = __instance.InnerPawn;
        if (pawn.HasPower<PowerWorker_Resurrect>() && Find.TickManager.TicksGame - __instance.timeOfDeath >= TicksToResurrect
                                                   && ResurrectionUtility.TryResurrect(pawn)
                                                   && PawnUtility.ShouldSendNotificationAbout(pawn))
            Messages.Message("MessagePawnResurrected".Translate(pawn), pawn, MessageTypeDefOf.PositiveEvent);
    }
}
