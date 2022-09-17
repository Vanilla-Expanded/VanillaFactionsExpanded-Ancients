using RimWorld;
using RimWorld.Planet;
using Verse;
using VFECore.Abilities;

namespace VFEAncients;

public class Ability_Calm : Ability
{
    public override void Cast(params GlobalTargetInfo[] targets)
    {
        base.Cast(targets);
        foreach (var target in targets) (target.Thing as Pawn)?.MentalState?.RecoverFromState();
    }

    public override bool ShowGizmoOnPawn() => def.targetModes[0] == AbilityTargetingMode.Self ? pawn.InMentalState : base.ShowGizmoOnPawn();

    public override bool ValidateTarget(LocalTargetInfo target, bool showMessages = true)
    {
        if (!base.ValidateTarget(target, showMessages) || !target.HasThing || target.Thing is not Pawn p) return false;
        if (p.InMentalState) return true;
        if (showMessages) Messages.Message("VFEAncients.NotInMentalState".Translate(), MessageTypeDefOf.RejectInput);
        return false;
    }
}