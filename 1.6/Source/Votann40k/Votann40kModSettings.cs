using Verse;

namespace Votann40k;

public class Votann40kModSettings : ModSettings
{
    public int minimumItemValueToTrade = 500;

    public int contractRefreshDays = 5;

    public override void ExposeData()
    {
        Scribe_Values.Look(ref minimumItemValueToTrade, "minimumItemValueToTrade", 500);
        Scribe_Values.Look(ref contractRefreshDays, "contractRefreshDays", 5);
    }
}
