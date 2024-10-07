using RimWorld;
using Verse;

namespace VFEAncients;

[DefOf]
public class VFEA_DefOf
{
    public static HediffDef VFEA_PlasteelClaw;
    public static HediffDef VFEA_RegrowingPart;
    public static PowerDef Lustful;
    public static InteractionDef VFEA_RomanceAttempt_Lustful;
    public static JobDef VFEA_ZealotExecution;
    public static PowerDef PromisingCandidate;
    public static PowerDef Paranoid;
    public static InteractionDef KindWords;
    public static PowerDef Celebrity;
    public static ThingDef VFEA_LaserEyeBeamDraw;
    public static ThingDef VFEA_ElectricBlast;
    public static StatDef VFEAncients_MeleeCooldownFactor;
    public static JobDef VFEA_EnterGeneTailoringPod;
    public static JobDef VFEA_CarryToGeneTailoringPod;
    public static StatDef VFEA_InjectingTimeFactor;
    public static StatDef VFEA_FailChance;
    public static MentalBreakDef Berserk;
    public static HediffDef ChemicalBurn;
    public static ThingDef VFEA_NanotechRetractor;
    public static ThingDef VFEA_SupplyCrateIncoming;
    public static ThingDef VFEA_SupplyCrateLeaving;
    public static ThingDef VFEA_AncientSupplyCrate;
    public static ThingSetMakerDef VFEA_Contents_SuuplyDrop;
    public static JobDef VFEA_PrisonerInterrogate;
    public static JobDef VFEA_CarryToBioBatteryTank;
    public static PrisonerInteractionModeDef VFEA_Interrogate;
    public static InteractionDef VFEA_Intimidate;
    public static InteractionDef VFEA_InterrogatePrisoner;
    public static RulePackDef VFEA_InterrogationRefused;
    public static RulePackDef VFEA_InterrogationSuccess;
    public static ThingDef VFEA_NaniteSampler;
    public static HediffDef VFEA_MetaMorph;
    public static FactionDef VFEA_NewVault;
    public static FactionDef VFEA_AncientSoldiers;
    public static ThingDef VFEA_SuperJumpingPawn;
    public static SoundDef VFEA_GloryKill_Music;
    public static PowerDef PsychologicalParalysis;
    public static ThingDef VFEA_SlingshotDropOffSpot;
    public static PawnRenderTreeDef VFEA_Metamorphed;
    public static ThoughtDef VFEA_AcquiredSuperpower;
    [MayRequireRoyalty]
    public static IncidentDef WandererJoinAbasia;

    static VFEA_DefOf()
    {
        DefOfHelper.EnsureInitializedInCtor(typeof(VFEA_DefOf));
    }
}
