using System.Collections.Generic;
using Core40k;
using RimWorld;
using UnityEngine;
using VEF.Utils;
using Verse;

namespace Votann40k;

public class Dialog_Grudges : Window
{
    private const float TitleRowHeight = 34f;
    private const float StateRowHeight = 26f;
    private const float HeaderHeight = TitleRowHeight + StateRowHeight + 6f;

    private static readonly Color DisabledColor = new(0.55f, 0.55f, 0.55f);

    public override Vector2 InitialSize => new(950f, 750f);

    private readonly GameComponent_KinGrudges grudges = Current.Game.GetComponent<GameComponent_KinGrudges>();

    private readonly List<Faction> factions = [];

    private Vector2 factionScrollPosition;

    private Faction currentFaction;

    public Dialog_Grudges()
    {
        doCloseX = true;
        closeOnClickedOutside = true;
        absorbInputAroundWindow = true;
    }

    public override void DoWindowContents(Rect inRect)
    {
        if (grudges == null)
        {
            Close();
            return;
        }

        RebuildFactionList();

        var headerRect = new Rect(inRect)
        {
            height = HeaderHeight
        };

        TitleSpaceWindow(headerRect);

        var infoRect = new Rect(inRect);
        infoRect.yMin += headerRect.height;

        var scrollRect = infoRect.TakeLeftPart(infoRect.width * 0.7f);

        scrollRect = scrollRect.ContractedBy(10);
        infoRect = infoRect.ContractedBy(10);

        FactionWindow(scrollRect, scrollRect.height / 8f);
        InfoWindow(infoRect);
    }

    private void RebuildFactionList()
    {
        factions.Clear();
        foreach (var faction in Find.FactionManager.AllFactionsVisibleInViewOrder)
        {
            if (faction.IsPlayer || faction.defeated || faction.temporary)
            {
                continue;
            }

            factions.Add(faction);
        }

        if (currentFaction != null && !factions.Contains(currentFaction))
        {
            currentFaction = null;
        }
    }

    private void TitleSpaceWindow(Rect headerRect)
    {
        headerRect.xMin += 10f;
        headerRect.xMax -= 10f;

        var topRect = headerRect.TakeTopPart(TitleRowHeight);
        var bottomRect = headerRect.TakeTopPart(StateRowHeight);

        var reforgeRect = topRect.TakeRightPart(topRect.width * 0.5f);

        Text.Font = GameFont.Medium;
        Text.Anchor = TextAnchor.MiddleLeft;
        Widgets.Label(topRect, "BEWH.Votann.Grudge.Title".Translate().CapitalizeFirst());

        Text.Font = GameFont.Small;
        Text.Anchor = TextAnchor.MiddleRight;
        Widgets.Label(reforgeRect, grudges.CanReforge
            ? "BEWH.Votann.Grudge.HeaderReforgeReady".Translate()
            : "BEWH.Votann.Grudge.HeaderReforgeLocked".Translate(grudges.ReforgeKillsLeft, grudges.ReforgeTicksLeft.ToStringTicksToPeriod()));
        TooltipHandler.TipRegion(reforgeRect, "BEWH.Votann.Grudge.ReforgeTooltip".Translate());

        Text.Anchor = TextAnchor.MiddleLeft;
        Widgets.Label(bottomRect, grudges.GrudgedFaction != null
            ? "BEWH.Votann.Grudge.HeaderCurrent".Translate(grudges.GrudgedFaction.Name, Votann40kUtils.JudgementTierLabel(grudges.JudgementTier), grudges.KillsSinceSworn)
            : "BEWH.Votann.Grudge.HeaderNone".Translate());
        TooltipHandler.TipRegion(bottomRect, "BEWH.Votann.Grudge.HeaderTooltip".Translate());

        Text.Anchor = TextAnchor.UpperLeft;
        Text.Font = GameFont.Medium;
    }

    private void FactionWindow(Rect scrollRect, float rowHeight)
    {
        var adjustedWidth = factions.Count > 8 ? 16f : 0f;

        var nameRect = scrollRect.TakeTopPart(scrollRect.height * 0.08f);
        nameRect.width -= adjustedWidth;

        Text.Font = GameFont.Small;
        Text.Anchor = TextAnchor.MiddleCenter;
        var relationRect = nameRect.TakeRightPart(nameRect.width * 0.55f);
        var killsRect = relationRect.TakeRightPart(relationRect.width * 0.6f);
        var markerRect = killsRect.TakeRightPart(killsRect.width * 0.5f);
        markerRect.width += adjustedWidth;

        Widgets.DrawMenuSection(nameRect);
        Widgets.DrawMenuSection(relationRect);
        Widgets.DrawMenuSection(killsRect);
        Widgets.DrawMenuSection(markerRect);

        Widgets.Label(nameRect, "BEWH.Votann.Grudge.ColFaction".Translate());
        Widgets.Label(relationRect, "BEWH.Votann.Grudge.ColRelation".Translate());
        Widgets.Label(killsRect, "BEWH.Votann.Grudge.ColKills".Translate());
        Widgets.Label(markerRect, "BEWH.Votann.Grudge.ColGrudge".Translate());

        Text.Anchor = TextAnchor.UpperLeft;
        Text.Font = GameFont.Medium;

        var viewRect = new Rect(scrollRect.x, scrollRect.y, scrollRect.width - adjustedWidth, rowHeight * Mathf.Max(1, factions.Count));

        Widgets.DrawMenuSection(scrollRect);
        Widgets.BeginScrollView(scrollRect, ref factionScrollPosition, viewRect);

        var curY = scrollRect.y + 5f;

        foreach (var faction in factions)
        {
            var rowRect = new Rect(viewRect)
            {
                height = rowHeight,
                y = curY
            };
            curY += rowHeight;
            rowRect = rowRect.ContractedBy(3f);

            Color? secondaryCol = null;
            if (currentFaction == faction)
            {
                secondaryCol = Color.yellow;
            }

            Core40kUtils.DrawColoredMenuSection(rowRect, Mouse.IsOver(rowRect) ? GenUI.MouseoverColor : (Color?)null, secondaryCol);

            if (Widgets.ButtonInvisible(rowRect))
            {
                currentFaction = faction;
            }

            var relationRectInternal = rowRect.TakeRightPart(rowRect.width * 0.55f);
            var killsRectInternal = relationRectInternal.TakeRightPart(relationRectInternal.width * 0.6f);
            var markerRectInternal = killsRectInternal.TakeRightPart(killsRectInternal.width * 0.5f);

            var iconRect = rowRect.TakeLeftPart(rowRect.height).ContractedBy(4f);
            DrawFactionIcon(iconRect, faction);

            Text.Font = GameFont.Small;
            Text.Anchor = TextAnchor.MiddleLeft;
            Widgets.Label(rowRect, faction.Name);

            Text.Anchor = TextAnchor.MiddleCenter;
            GUI.color = RelationColor(faction);
            Widgets.Label(relationRectInternal, RelationLabel(faction));
            GUI.color = Color.white;

            Widgets.Label(killsRectInternal, grudges.LifetimeKillsAgainst(faction).ToString());
            Widgets.Label(markerRectInternal, MarkerLabel(faction));

            Text.Anchor = TextAnchor.UpperLeft;
            Text.Font = GameFont.Medium;
        }

        Widgets.EndScrollView();
    }

    private string MarkerLabel(Faction faction)
    {
        if (faction == grudges.GrudgedFaction && faction == grudges.KinGrudgeFaction)
        {
            return "BEWH.Votann.Grudge.MarkerBoth".Translate();
        }

        if (faction == grudges.GrudgedFaction)
        {
            return "BEWH.Votann.Grudge.MarkerYours".Translate();
        }

        if (faction == grudges.KinGrudgeFaction)
        {
            return "BEWH.Votann.Grudge.MarkerKin".Translate();
        }

        return string.Empty;
    }

    private static void DrawFactionIcon(Rect rect, Faction faction)
    {
        var icon = faction.def.FactionIcon;
        if (icon == null)
        {
            return;
        }

        GUI.color = faction.Color;
        Widgets.DrawTextureFitted(rect, icon, 1f);
        GUI.color = Color.white;
    }

    private static string RelationLabel(Faction faction)
    {
        return faction.PlayerRelationKind switch
        {
            FactionRelationKind.Hostile => "BEWH.Votann.Grudge.RelationHostile".Translate(),
            FactionRelationKind.Ally => "BEWH.Votann.Grudge.RelationAlly".Translate(),
            _ => "BEWH.Votann.Grudge.RelationNeutral".Translate()
        };
    }

    private static Color RelationColor(Faction faction)
    {
        return faction.PlayerRelationKind switch
        {
            FactionRelationKind.Hostile => new Color(0.9f, 0.4f, 0.4f),
            FactionRelationKind.Ally => new Color(0.5f, 0.9f, 0.5f),
            _ => Color.white
        };
    }

    private void InfoWindow(Rect infoRect)
    {
        Widgets.DrawMenuSection(infoRect);

        if (currentFaction == null)
        {
            Text.Font = GameFont.Small;
            Text.Anchor = TextAnchor.MiddleCenter;
            Widgets.Label(infoRect, "BEWH.Votann.Grudge.SelectFaction".Translate());
            Text.Anchor = TextAnchor.UpperLeft;
            Text.Font = GameFont.Medium;
            return;
        }

        var nameRect = infoRect.TakeTopPart(infoRect.height * 0.1f);
        Text.Font = GameFont.Small;
        Text.Anchor = TextAnchor.MiddleCenter;
        Widgets.Label(nameRect, currentFaction.Name);
        Text.Anchor = TextAnchor.UpperLeft;

        var buttonRect = infoRect.TakeBottomPart(infoRect.height * 0.18f).ContractedBy(15f);
        Widgets.Label(infoRect.ContractedBy(10f), InfoText());
        Text.Font = GameFont.Medium;

        var isCurrent = currentFaction == grudges.GrudgedFaction;
        var canSwear = !isCurrent && grudges.CanReforge;

        if (!canSwear)
        {
            TooltipHandler.TipRegion(buttonRect, isCurrent
                ? "BEWH.Votann.Grudge.SwearDisabledCurrent".Translate()
                : "BEWH.Votann.Grudge.SwearDisabledLocked".Translate(grudges.GrudgedFaction.Name));
        }

        var previousColor = GUI.color;
        GUI.color = canSwear ? previousColor : DisabledColor;
        var pressed = Widgets.ButtonText(buttonRect, "BEWH.Votann.Grudge.Swear".Translate(), active: canSwear);
        GUI.color = previousColor;

        if (pressed)
        {
            var faction = currentFaction;
            Find.WindowStack.Add(Dialog_MessageBox.CreateConfirmation(
                "BEWH.Votann.Grudge.SwearConfirm".Translate(faction.Name, grudges.SwearGoodwillCostForView),
                delegate { grudges.SwearGrudge(faction, bypassLock: false); }));
        }
    }

    private string InfoText()
    {
        string text = "BEWH.Votann.Grudge.InfoLedger".Translate(currentFaction.Name, grudges.LifetimeKillsAgainst(currentFaction));

        text += "\n\n" + "BEWH.Votann.Grudge.InfoTiers".Translate(
            grudges.TierFactor(1).ToStringPercent(),
            grudges.TierThreshold(2),
            grudges.TierFactor(2).ToStringPercent(),
            grudges.TierThreshold(3),
            grudges.TierFactor(3).ToStringPercent());

        if (currentFaction == grudges.GrudgedFaction)
        {
            text += "\n\n" + "BEWH.Votann.Grudge.InfoIsCurrent".Translate(
                Votann40kUtils.JudgementTierLabel(grudges.JudgementTier),
                grudges.EffectiveKills);
        }
        else
        {
            text += "\n\n" + "BEWH.Votann.Grudge.InfoCost".Translate(
                grudges.SwearGoodwillCostForView,
                Mathf.Min(grudges.LifetimeKillsAgainst(currentFaction) / 2, grudges.TierThreshold(3)));

            if (!grudges.CanReforge)
            {
                text += "\n\n" + "BEWH.Votann.Grudge.InfoLocked".Translate(
                    grudges.GrudgedFaction.Name,
                    grudges.ReforgeKillsLeft,
                    grudges.ReforgeTicksLeft.ToStringTicksToPeriod());
            }
        }

        if (currentFaction == grudges.KinGrudgeFaction)
        {
            text += "\n\n" + "BEWH.Votann.Grudge.InfoKinGrudge".Translate(grudges.KinGrudgeKills);
        }

        return text;
    }
}
