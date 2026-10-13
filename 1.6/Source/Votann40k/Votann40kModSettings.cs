using Verse;

namespace Votann40k;

public class Votann40kModSettings : ModSettings
{
    public int minimumItemValueToTrade = 500;

    public int contractRefreshDays = 5;

    public int grudgeTier2Kills = 10;

    public int grudgeTier3Kills = 25;

    public float grudgeTier1Factor = 1.15f;

    public float grudgeTier2Factor = 1.3f;

    public float grudgeTier3Factor = 1.5f;

    public int grudgeReforgeKills = 20;

    public int grudgeReforgeDays = 15;

    public int grudgeSwearGoodwillCost = 20;

    public int grudgeBreakDebtMax = 1000;

    public int kinGrudgeDebtPerKill = 20;

    public float kinGrudgeKinKillerFactor = 1.75f;

    public float kinGrudgeAlignedFactor = 2f;

    public int kinGrudgeDebtFloor = -5000;

    public int kinGrudgeRerollDays = 15;

    public override void ExposeData()
    {
        Scribe_Values.Look(ref minimumItemValueToTrade, "minimumItemValueToTrade", 500);
        Scribe_Values.Look(ref contractRefreshDays, "contractRefreshDays", 5);
        Scribe_Values.Look(ref grudgeTier2Kills, "grudgeTier2Kills", 10);
        Scribe_Values.Look(ref grudgeTier3Kills, "grudgeTier3Kills", 25);
        Scribe_Values.Look(ref grudgeTier1Factor, "grudgeTier1Factor", 1.15f);
        Scribe_Values.Look(ref grudgeTier2Factor, "grudgeTier2Factor", 1.3f);
        Scribe_Values.Look(ref grudgeTier3Factor, "grudgeTier3Factor", 1.5f);
        Scribe_Values.Look(ref grudgeReforgeKills, "grudgeReforgeKills", 20);
        Scribe_Values.Look(ref grudgeReforgeDays, "grudgeReforgeDays", 15);
        Scribe_Values.Look(ref grudgeSwearGoodwillCost, "grudgeSwearGoodwillCost", 20);
        Scribe_Values.Look(ref grudgeBreakDebtMax, "grudgeBreakDebtMax", 1000);
        Scribe_Values.Look(ref kinGrudgeDebtPerKill, "kinGrudgeDebtPerKill", 20);
        Scribe_Values.Look(ref kinGrudgeKinKillerFactor, "kinGrudgeKinKillerFactor", 1.75f);
        Scribe_Values.Look(ref kinGrudgeAlignedFactor, "kinGrudgeAlignedFactor", 2f);
        Scribe_Values.Look(ref kinGrudgeDebtFloor, "kinGrudgeDebtFloor", -5000);
        Scribe_Values.Look(ref kinGrudgeRerollDays, "kinGrudgeRerollDays", 15);
    }
}
