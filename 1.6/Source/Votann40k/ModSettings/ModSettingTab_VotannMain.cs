using Core40k;
using UnityEngine;
using Verse;

namespace Votann40k;

public class ModSettingTab_VotannMain : ModSettingTab
{
    public override void DrawTab(Rect inRect, ModSettings settings)
    {
        if (settings is not Votann40kModSettings votannSettings)
        {
            Log.Error("Settings not correct type");
            return;
        }

        var viewRect = new Rect(inRect.x, inRect.y, inRect.width - 16f, scrollViewHeight);
        scrollViewHeight = 0f;

        Widgets.BeginScrollView(inRect, ref scrollPos, viewRect);
        var listingStandard = new Listing_Standard();
        listingStandard.Begin(viewRect);
        listingStandard.Gap(36);
        scrollViewHeight += ListingHeightIncreaseGap;
        scrollViewHeight += ListingHeightIncrease;

        votannSettings.minimumItemValueToTrade = (int)listingStandard.SliderLabeled("BEWH.Votann.ModSettings.KinContractMinValue".Translate(votannSettings.minimumItemValueToTrade), votannSettings.minimumItemValueToTrade, 0, 1000, tooltip: "BEWH.Votann.ModSettings.KinContractMinValueTooltip".Translate());
        scrollViewHeight += ListingHeightIncrease;

        listingStandard.Gap(12);
        scrollViewHeight += 12f;

        votannSettings.contractRefreshDays = (int)listingStandard.SliderLabeled("BEWH.Votann.ModSettings.KinContractRefreshDays".Translate(votannSettings.contractRefreshDays), votannSettings.contractRefreshDays, 1, 30, tooltip: "BEWH.Votann.ModSettings.KinContractRefreshDaysTooltip".Translate());
        scrollViewHeight += ListingHeightIncrease;

        scrollViewHeight += ListingHeightIncrease;

        listingStandard.GapLine(24);
        scrollViewHeight += 24f;

        Text.Font = GameFont.Medium;
        listingStandard.Label("BEWH.Votann.ModSettings.GrudgeHeader".Translate());
        Text.Font = GameFont.Small;
        scrollViewHeight += ListingHeightIncrease;

        votannSettings.grudgeTier2Kills = (int)listingStandard.SliderLabeled("BEWH.Votann.ModSettings.GrudgeTier2Kills".Translate(votannSettings.grudgeTier2Kills), votannSettings.grudgeTier2Kills, 1, 100);
        scrollViewHeight += ListingHeightIncrease;

        votannSettings.grudgeTier3Kills = (int)listingStandard.SliderLabeled("BEWH.Votann.ModSettings.GrudgeTier3Kills".Translate(votannSettings.grudgeTier3Kills), votannSettings.grudgeTier3Kills, 1, 200);
        scrollViewHeight += ListingHeightIncrease;

        votannSettings.grudgeTier1Factor = RoundFactor(listingStandard.SliderLabeled("BEWH.Votann.ModSettings.GrudgeTier1Factor".Translate(votannSettings.grudgeTier1Factor.ToStringPercent()), votannSettings.grudgeTier1Factor, 1f, 3f));
        scrollViewHeight += ListingHeightIncrease;

        votannSettings.grudgeTier2Factor = RoundFactor(listingStandard.SliderLabeled("BEWH.Votann.ModSettings.GrudgeTier2Factor".Translate(votannSettings.grudgeTier2Factor.ToStringPercent()), votannSettings.grudgeTier2Factor, 1f, 3f));
        scrollViewHeight += ListingHeightIncrease;

        votannSettings.grudgeTier3Factor = RoundFactor(listingStandard.SliderLabeled("BEWH.Votann.ModSettings.GrudgeTier3Factor".Translate(votannSettings.grudgeTier3Factor.ToStringPercent()), votannSettings.grudgeTier3Factor, 1f, 3f));
        scrollViewHeight += ListingHeightIncrease;

        votannSettings.grudgeReforgeKills = (int)listingStandard.SliderLabeled("BEWH.Votann.ModSettings.GrudgeReforgeKills".Translate(votannSettings.grudgeReforgeKills), votannSettings.grudgeReforgeKills, 1, 200, tooltip: "BEWH.Votann.ModSettings.GrudgeReforgeTooltip".Translate());
        scrollViewHeight += ListingHeightIncrease;

        votannSettings.grudgeReforgeDays = (int)listingStandard.SliderLabeled("BEWH.Votann.ModSettings.GrudgeReforgeDays".Translate(votannSettings.grudgeReforgeDays), votannSettings.grudgeReforgeDays, 1, 120, tooltip: "BEWH.Votann.ModSettings.GrudgeReforgeTooltip".Translate());
        scrollViewHeight += ListingHeightIncrease;

        votannSettings.grudgeSwearGoodwillCost = (int)listingStandard.SliderLabeled("BEWH.Votann.ModSettings.GrudgeSwearCost".Translate(votannSettings.grudgeSwearGoodwillCost), votannSettings.grudgeSwearGoodwillCost, 0, 100);
        scrollViewHeight += ListingHeightIncrease;

        votannSettings.grudgeBreakDebtMax = (int)listingStandard.SliderLabeled("BEWH.Votann.ModSettings.GrudgeBreakDebt".Translate(votannSettings.grudgeBreakDebtMax), votannSettings.grudgeBreakDebtMax, 0, 5000, tooltip: "BEWH.Votann.ModSettings.GrudgeBreakDebtTooltip".Translate());
        scrollViewHeight += ListingHeightIncrease;

        votannSettings.kinGrudgeDebtPerKill = (int)listingStandard.SliderLabeled("BEWH.Votann.ModSettings.KinGrudgeDebtPerKill".Translate(votannSettings.kinGrudgeDebtPerKill), votannSettings.kinGrudgeDebtPerKill, 0, 200, tooltip: "BEWH.Votann.ModSettings.KinGrudgeDebtPerKillTooltip".Translate());
        scrollViewHeight += ListingHeightIncrease;

        votannSettings.kinGrudgeKinKillerFactor = RoundFactor(listingStandard.SliderLabeled("BEWH.Votann.ModSettings.KinGrudgeKinFactor".Translate(votannSettings.kinGrudgeKinKillerFactor.ToStringPercent()), votannSettings.kinGrudgeKinKillerFactor, 1f, 4f));
        scrollViewHeight += ListingHeightIncrease;

        votannSettings.kinGrudgeAlignedFactor = RoundFactor(listingStandard.SliderLabeled("BEWH.Votann.ModSettings.KinGrudgeAlignedFactor".Translate(votannSettings.kinGrudgeAlignedFactor.ToStringPercent()), votannSettings.kinGrudgeAlignedFactor, 1f, 4f));
        scrollViewHeight += ListingHeightIncrease;

        votannSettings.kinGrudgeDebtFloor = (int)listingStandard.SliderLabeled("BEWH.Votann.ModSettings.KinGrudgeDebtFloor".Translate(votannSettings.kinGrudgeDebtFloor), votannSettings.kinGrudgeDebtFloor, -10000, 0, tooltip: "BEWH.Votann.ModSettings.KinGrudgeDebtFloorTooltip".Translate());
        scrollViewHeight += ListingHeightIncrease;

        votannSettings.kinGrudgeRerollDays = (int)listingStandard.SliderLabeled("BEWH.Votann.ModSettings.KinGrudgeRerollDays".Translate(votannSettings.kinGrudgeRerollDays), votannSettings.kinGrudgeRerollDays, 1, 120, tooltip: "BEWH.Votann.ModSettings.KinGrudgeRerollTooltip".Translate());
        scrollViewHeight += ListingHeightIncrease;

        scrollViewHeight += ListingHeightIncrease;

        listingStandard.End();
        Widgets.EndScrollView();
    }

    private static float RoundFactor(float value)
    {
        return Mathf.Round(value * 100f) / 100f;
    }
}
