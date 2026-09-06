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

        listingStandard.End();
        Widgets.EndScrollView();
    }
}
