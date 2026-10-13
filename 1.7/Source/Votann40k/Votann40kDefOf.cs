using RimWorld;
using Verse;
using Verse.AI;

namespace Votann40k;

[DefOf]
public static class Votann40kDefOf
{
    //JobDef
    public static JobDef BEWH_CarryKinToAncestorCore;
    public static JobDef BEWH_OpenContractWindow;
    public static JobDef BEWH_KinBreach;
    public static JobDef BEWH_KinEnterTunnel;
    public static JobDef BEWH_OpenGrudgeWindow;

    //GeneDef
    public static GeneDef BEWH_KinGrudgy;
    public static GeneDef BEWH_KinClanLoyalty;
    public static GeneDef BEWH_KinCloneskein;

    //ThingDef
    public static ThingDef BEWH_AncestorCore;
    public static ThingDef BEWH_KinCrucible;
    public static ThingDef BEWH_KinDrill;
    public static ThingDef BEWH_KinDrillSpawner;
    public static ThingDef BEWH_KinTunnelSpawner;
    public static ThingDef BEWH_KinTunnelHole;
    public static ThingDef BEWH_KinGrudgeStone;

    //ThoughtDef
    public static ThoughtDef BEWH_GrudgeStruckOut;
    public static ThoughtDef BEWH_GrudgeStruckOutHeard;

    //HediffDef
    public static HediffDef BEWH_FellInGrudge;

    //LetterDef
    public static LetterDef BEWH_KinGrudgeOffer;

    //XenotypeDef
    public static XenotypeDef BEWH_Kin;

    //ResearchProjectDef
    public static ResearchProjectDef TransportPod;

    //FactionDef
    public static FactionDef BEWH_OffworldKinFaction;

    //DutyDef
    public static DutyDef BEWH_KinReclaim;

    //PawnGroupKindDef
    public static PawnGroupKindDef BEWH_KinReclamation;

    //EffecterDef
    public static EffecterDef EmergencePointSustained2X2;
    public static EffecterDef EmergencePointComplete2X2;

    static Votann40kDefOf()
    {
        DefOfHelper.EnsureInitializedInCtor(typeof(Votann40kDefOf));
    }
}
