using System.Linq;
using RimWorld;
using Verse;

namespace VFEAncients;

public class PawnRenderNode_Metamorphed : PawnRenderNode_AnimalPart
{
    public PawnRenderNode_Metamorphed(Pawn pawn, PawnRenderNodeProperties props, PawnRenderTree tree) : base(pawn, props, tree) { }

    public override Graphic GraphicFor(Pawn pawn)
    {
        var metaMorphComp = pawn.health.hediffSet.GetAllComps().OfType<HediffComp_MetaMorph>().First();
        var curKindLifeStage = metaMorphComp.Target.lifeStages.Last();
        Graphic graphic;
        if (pawn.gender != Gender.Female || curKindLifeStage.femaleGraphicData == null)
            graphic = curKindLifeStage.bodyGraphicData.Graphic;
        else
            graphic = curKindLifeStage.femaleGraphicData.Graphic;
        if ((pawn.Dead || (pawn.IsMutant && pawn.mutant.Def.useCorpseGraphics)) && curKindLifeStage.corpseGraphicData != null)
        {
            if (pawn.gender != Gender.Female || curKindLifeStage.femaleCorpseGraphicData == null)
                graphic = curKindLifeStage.corpseGraphicData.Graphic.GetColoredVersion(curKindLifeStage.corpseGraphicData.Graphic.Shader, graphic.Color,
                    graphic.ColorTwo);
            else
                graphic = curKindLifeStage.femaleCorpseGraphicData.Graphic.GetColoredVersion(curKindLifeStage.femaleCorpseGraphicData.Graphic.Shader,
                    graphic.Color, graphic.ColorTwo);
        }

        switch (pawn.Drawer.renderer.CurRotDrawMode)
        {
            case RotDrawMode.Fresh:
                if (ModsConfig.AnomalyActive && pawn.IsMutant && pawn.mutant.HasTurned)
                    return graphic.GetColoredVersion(ShaderDatabase.Cutout, MutantUtility.GetSkinColor(pawn, graphic.Color).Value,
                        MutantUtility.GetSkinColor(pawn, graphic.ColorTwo).Value);
                return graphic;
            case RotDrawMode.Rotting:
                return graphic.GetColoredVersion(ShaderDatabase.Cutout, PawnRenderUtility.GetRottenColor(graphic.Color),
                    PawnRenderUtility.GetRottenColor(graphic.ColorTwo));
            case RotDrawMode.Dessicated:
                if (curKindLifeStage.dessicatedBodyGraphicData != null)
                {
                    Graphic graphic2;
                    if (metaMorphComp.Target.RaceProps.FleshType == FleshTypeDefOf.Insectoid)
                    {
                        var dessicatedColorInsect = PawnRenderUtility.DessicatedColorInsect;
                        if (pawn.gender != Gender.Female || curKindLifeStage.femaleDessicatedBodyGraphicData == null)
                            graphic2 = curKindLifeStage.dessicatedBodyGraphicData.Graphic.GetColoredVersion(ShaderDatabase.Cutout, dessicatedColorInsect,
                                dessicatedColorInsect);
                        else
                            graphic2 = curKindLifeStage.femaleDessicatedBodyGraphicData.Graphic.GetColoredVersion(ShaderDatabase.Cutout, dessicatedColorInsect,
                                dessicatedColorInsect);
                    }
                    else if (pawn.gender != Gender.Female || curKindLifeStage.femaleDessicatedBodyGraphicData == null)
                        graphic2 = curKindLifeStage.dessicatedBodyGraphicData.GraphicColoredFor(pawn);
                    else
                        graphic2 = curKindLifeStage.femaleDessicatedBodyGraphicData.GraphicColoredFor(pawn);

                    if (pawn.IsMutant) graphic2.ShadowGraphic = graphic.ShadowGraphic;
                    return graphic2;
                }

                break;
        }

        return null;
    }
}
