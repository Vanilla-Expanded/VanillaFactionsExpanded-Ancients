using System.Collections.Generic;
using System.Linq;
using RimWorld;
using RimWorld.Planet;
using Verse;
using VFECore.Abilities;

namespace VFEAncients;

public class Ability_MetaMorph : Ability
{
    public PawnKindDef Target => AbilityModExtensions.OfType<AbilityExtension_MetaMorph>().FirstOrDefault()?.Target;

    public override void Cast(params GlobalTargetInfo[] targets)
    {
        base.Cast(targets);

        var hediff = HediffMaker.MakeHediff(VFEA_DefOf.VFEA_MetaMorph, pawn);
        if (hediff.TryGetComp<HediffComp_MetaMorph>() is { } comp) comp.Target = Target;
        pawn.health.AddHediff(hediff);

        for (var i = 0; i < 13; i++) pawn.health.DropBloodFilth();

        var filthNum = Target?.race.GetStatValueAbstract(StatDefOf.FilthRate) * 3.5 ?? 5;
        for (var i = 0; i < filthNum; i++)
            FilthMaker.TryMakeFilth(pawn.Position + GenRadial.RadialPattern[Rand.Range(1, 12)], pawn.Map, ThingDefOf.Filth_AnimalFilth, 1,
                FilthSourceFlags.Natural | FilthSourceFlags.Pawn);
    }

    public override bool IsEnabledForPawn(out string reason)
    {
        if (!base.IsEnabledForPawn(out reason)) return false;
        if (HediffComp_MetaMorph.MetamorphedPawns.Contains(pawn))
        {
            reason = "VFEAncients.AlreadyMetaMorph".Translate();
            return false;
        }

        return true;
    }
}

public class AbilityExtension_MetaMorph : AbilityExtension_AbilityMod
{
    public PawnKindDef Target;
}

[StaticConstructorOnStartup]
public class HediffComp_MetaMorph : HediffComp
{
    public static HashSet<Pawn> MetamorphedPawns = new();
    public PawnKindDef Target;

    public override void CompPostPostAdd(DamageInfo? dinfo)
    {
        base.CompPostPostAdd(dinfo);
        Setup();
    }

    public void Setup()
    {
        MetamorphedPawns.Add(Pawn);

        Pawn.Drawer.renderer.SetAllGraphicsDirty();

        Pawn.meleeVerbs.Notify_PawnDespawned();

        Pawn.verbTracker.InitVerbsFromZero();
    }

    public override void CompPostPostRemoved()
    {
        base.CompPostPostRemoved();

        MetamorphedPawns.Remove(Pawn);

        Pawn.Drawer.renderer.SetAllGraphicsDirty();

        Pawn.verbTracker.InitVerbsFromZero();

        Pawn.meleeVerbs.Notify_PawnDespawned();
    }

    public override void CompExposeData()
    {
        base.CompExposeData();
        Scribe_Defs.Look(ref Target, "target");
        if (Scribe.mode == LoadSaveMode.PostLoadInit) Setup();
    }
}
