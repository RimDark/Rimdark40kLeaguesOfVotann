using Core40k;
using RimWorld;
using UnityEngine;
using VEF.Utils;
using Verse;

namespace Votann40k;

public class Dialog_Contracts : Window
{
    public override Vector2 InitialSize => new(950f, 750f);

    private readonly GameComponent_KinContracts gameComponentKinContracts = Current.Game.GetComponent<GameComponent_KinContracts>();

    private float viewRectHeight;

    private Vector2 contractScrollPosition;

    private KinContract currentKinContract;

    public Dialog_Contracts()
    {
        doCloseX = true;
        closeOnClickedOutside = true;
        absorbInputAroundWindow = true;
    }

    public override void DoWindowContents(Rect inRect)
    {
        if (gameComponentKinContracts == null)
        {
            Close();
            return;
        }

        gameComponentKinContracts.PruneInvalidContracts();
        if (currentKinContract != null && !gameComponentKinContracts.KinContracts.Contains(currentKinContract))
        {
            currentKinContract = null;
        }

        var titleRect = new Rect(inRect)
        {
            height = Text.LineHeight * 2f
        };

        TitleSpaceWindow(titleRect);

        var infoRect = new Rect(inRect);
        infoRect.yMin += titleRect.height;

        var scrollRect = infoRect.TakeLeftPart(infoRect.width * 0.7f);
        var resetRect = scrollRect.TakeBottomPart(scrollRect.width * 0.1f);

        scrollRect = scrollRect.ContractedBy(10);
        infoRect = infoRect.ContractedBy(10);
        resetRect = resetRect.ContractedBy(10);

        var kinContractHeight = scrollRect.height / 6f;

        ContractWindow(scrollRect, kinContractHeight);
        RefreshWindow(resetRect);
        InfoWindow(infoRect);
    }

    private void TitleSpaceWindow(Rect titleRect)
    {
        titleRect = titleRect.ContractedBy(10);
        Text.Font = GameFont.Medium;
        Text.Anchor = TextAnchor.MiddleLeft;
        var contractAvailableRect = titleRect.TakeRightPart(titleRect.width * 0.75f);
        Widgets.Label(titleRect, "BEWH.Votann.Contract.Title".Translate().CapitalizeFirst());

        Text.Font = GameFont.Small;
        var priceMultRect = contractAvailableRect.TakeRightPart(contractAvailableRect.width * 0.66f);
        Widgets.Label(contractAvailableRect, "BEWH.Votann.Contract.MaximumContracts".Translate(gameComponentKinContracts.CurrentMaxContractAmount));
        TooltipHandler.TipRegion(contractAvailableRect, "BEWH.Votann.Contract.MaximumContractsTooltip".Translate());

        var playerDebtRect = priceMultRect.TakeRightPart(priceMultRect.width * 0.3f);
        Widgets.Label(priceMultRect, "BEWH.Votann.Contract.PriceMult".Translate(gameComponentKinContracts.CurrentContractPriceMultAmount.ToStringDecimalIfSmall()));
        TooltipHandler.TipRegion(priceMultRect, "BEWH.Votann.Contract.PriceMultTooltip".Translate());

        Widgets.Label(playerDebtRect, "BEWH.Votann.Contract.Goodwill".Translate(gameComponentKinContracts.CurrentGoodwillByPlayerDebt));
        TooltipHandler.TipRegion(playerDebtRect, "BEWH.Votann.Contract.GoodwillTooltip".Translate());

        Text.Anchor = TextAnchor.UpperLeft;
        Text.Font = GameFont.Medium;
    }

    private void ContractWindow(Rect scrollRect, float kinContractHeight)
    {
        var adjustedWidth = gameComponentKinContracts.CurrentMaxContractAmountForView > 6 ? 16f : 0f;

        var statusRect = scrollRect.TakeTopPart(scrollRect.height * 0.1f);
        statusRect.width -= adjustedWidth;

        Text.Font = GameFont.Small;
        Text.Anchor = TextAnchor.MiddleCenter;
        var requestedRect = statusRect.TakeRightPart(statusRect.width * 0.75f);
        var givenRect = requestedRect.TakeRightPart(requestedRect.width * 0.66f);
        var expiresInRect = givenRect.TakeRightPart(givenRect.width * 0.5f);
        expiresInRect.width += adjustedWidth;

        Widgets.DrawMenuSection(statusRect);
        Widgets.DrawMenuSection(requestedRect);
        Widgets.DrawMenuSection(givenRect);
        Widgets.DrawMenuSection(expiresInRect);

        Widgets.Label(statusRect, "BEWH.Votann.Contract.Status".Translate());
        Widgets.Label(requestedRect, "BEWH.Votann.Contract.RequestedItem".Translate());
        Widgets.Label(givenRect, "BEWH.Votann.Contract.GivenItem".Translate());
        Widgets.Label(expiresInRect, "BEWH.Votann.Contract.ExpiresIn".Translate());

        Text.Anchor = TextAnchor.UpperLeft;
        Text.Font = GameFont.Medium;

        viewRectHeight = kinContractHeight * Mathf.Max(1, gameComponentKinContracts.CurrentMaxContractAmountForView);

        var viewRect = new Rect(scrollRect.x, scrollRect.y, scrollRect.width - adjustedWidth, viewRectHeight);

        Widgets.DrawMenuSection(scrollRect);

        Widgets.BeginScrollView(scrollRect, ref contractScrollPosition, viewRect);

        if (gameComponentKinContracts.KinHostile)
        {
            var contractRect = new Rect(viewRect)
            {
                height = kinContractHeight
            };
            contractRect = contractRect.ContractedBy(3f);
            Widgets.DrawRectFast(contractRect, new Color(0.5f, 0.1f, 0.1f));
            Text.Font = GameFont.Small;
            Text.Anchor = TextAnchor.MiddleCenter;
            Widgets.Label(contractRect, "BEWH.Votann.Contract.HostileDebt".Translate(Mathf.CeilToInt(Mathf.Max(0f, gameComponentKinContracts.playerDebt))));
            Text.Anchor = TextAnchor.UpperLeft;
            Text.Font = GameFont.Medium;
            Widgets.EndScrollView();
            return;
        }

        var curY = scrollRect.y + 5f;

        foreach (var kinContract in gameComponentKinContracts.KinContracts)
        {
            var contractRect = new Rect(viewRect)
            {
                height = kinContractHeight,
                y = curY,
            };
            curY += kinContractHeight;
            contractRect = contractRect.ContractedBy(3f);
            Color? secondaryCol = null;
            if (currentKinContract == kinContract)
            {
                secondaryCol = Color.yellow;
            }
            if (Mouse.IsOver(contractRect))
            {
                Core40kUtils.DrawColoredMenuSection(contractRect, GenUI.MouseoverColor, secondaryCol);
            }
            else
            {
                Core40kUtils.DrawColoredMenuSection(contractRect, null, secondaryCol);
            }

            if (Widgets.ButtonInvisible(contractRect))
            {
                currentKinContract = kinContract;
            }
            var requestedRectInternal = contractRect.TakeRightPart(contractRect.width * 0.75f);
            var givenRectInternal = requestedRectInternal.TakeRightPart(requestedRectInternal.width * 0.66f);
            var expiresInRectInternal = givenRectInternal.TakeRightPart(givenRectInternal.width * 0.5f);

            Text.Font = GameFont.Small;
            Text.Anchor = TextAnchor.MiddleCenter;
            Widgets.Label(contractRect, kinContract.GetStatusLabel());
            var requestedRectIcon = requestedRectInternal.TakeLeftPart(requestedRectInternal.width * 0.5f);
            Widgets.Label(requestedRectInternal, kinContract.RequestedLabel);
            Widgets.ThingIcon(requestedRectIcon, kinContract.requestedDef, kinContract.requestedStuff);
            var givenRectIcon = givenRectInternal.TakeLeftPart(givenRectInternal.width * 0.5f);
            Widgets.Label(givenRectInternal, "BEWH.Votann.Contract.GivenItemContract".Translate(kinContract.givenItem.LabelCap, kinContract.givenAmount));
            Widgets.ThingIcon(givenRectIcon, kinContract.givenItem);
            Widgets.Label(expiresInRectInternal, "BEWH.Votann.Contract.ExpiresInContract".Translate(kinContract.GetTimeLabel()));
            Text.Anchor = TextAnchor.UpperLeft;
            Text.Font = GameFont.Medium;
        }

        Widgets.EndScrollView();
    }

    private void RefreshWindow(Rect resetRect)
    {
        if (Mouse.IsOver(resetRect))
        {
            Core40kUtils.DrawColoredMenuSection(resetRect, GenUI.MouseoverColor, null);
        }
        else
        {
            Widgets.DrawMenuSection(resetRect);
        }

        var resetButRect = resetRect.TakeLeftPart(resetRect.height);
        GUI.DrawTexture(resetButRect, Votann40kUtils.RefreshIcon.Texture);

        Text.Font = GameFont.Small;
        Text.Anchor = TextAnchor.MiddleLeft;
        Widgets.Label(resetRect,
            !gameComponentKinContracts.CanRefresh
                ? "BEWH.Votann.Contract.RefreshCooldown".Translate(gameComponentKinContracts.RefreshIn)
                : "BEWH.Votann.Contract.CanRefresh".Translate());
        Text.Anchor = TextAnchor.UpperLeft;
        Text.Font = GameFont.Medium;

        if ((gameComponentKinContracts.CanRefresh || DebugSettings.godMode) && Widgets.ButtonInvisible(resetRect))
        {
            gameComponentKinContracts.RefreshContracts();
            currentKinContract = null;
        }
    }

    private void InfoWindow(Rect infoRect)
    {
        Widgets.DrawMenuSection(infoRect);

        var tributeRect = infoRect.TakeBottomPart(infoRect.height * 0.2f).ContractedBy(15f);
        DrawTributeButton(tributeRect);

        if (currentKinContract == null || currentKinContract.Invalid)
        {
            return;
        }

        var nameRect = infoRect.TakeTopPart(infoRect.height * 0.12f);
        Text.Font = GameFont.Small;
        Text.Anchor = TextAnchor.MiddleCenter;
        Widgets.Label(nameRect, "BEWH.Votann.Contract.InfoTitle".Translate(currentKinContract.givenItem.LabelCap, currentKinContract.givenAmount, currentKinContract.RequestedLabel));
        Text.Anchor = TextAnchor.UpperLeft;

        var buttonRow = infoRect.TakeBottomPart(infoRect.height * 0.25f);
        var descRect = infoRect.ContractedBy(10f);
        Widgets.Label(descRect, InfoText(currentKinContract));
        Text.Font = GameFont.Medium;

        var jumpToBut = buttonRow.TakeLeftPart(buttonRow.width * 0.5f).ContractedBy(15f);
        var acceptBut = buttonRow.ContractedBy(15f);

        if (currentKinContract.InProgress)
        {
            if (Widgets.ButtonText(jumpToBut, "BEWH.Votann.Contract.JumpToDrill".Translate(), active: currentKinContract.drill is { Spawned: true }))
            {
                currentKinContract.JumpToDrill();
                Close();
            }
        }
        else if (Widgets.ButtonText(jumpToBut, "BEWH.Votann.Contract.JumpTo".Translate(), active: currentKinContract.requestedItem is { Spawned: true }))
        {
            currentKinContract.JumpToThingRequested();
            Close();
        }

        var canAccept = currentKinContract.state == KinContractState.Offered && !currentKinContract.OfferExpired && !gameComponentKinContracts.KinHostile;
        if (Widgets.ButtonText(acceptBut, "BEWH.Votann.Contract.Accept".Translate(), active: canAccept))
        {
            gameComponentKinContracts.AcceptContract(currentKinContract);
        }
    }

    private static string InfoText(KinContract contract)
    {
        switch (contract.state)
        {
            case KinContractState.DrillEnRoute:
                return "BEWH.Votann.Contract.InfoDrillEnRoute".Translate(contract.givenItem.label, contract.givenAmount, contract.RequestedLabel, contract.DrillTicksLeft.ToStringTicksToPeriod());
            case KinContractState.Loading:
                return "BEWH.Votann.Contract.InfoLoading".Translate(contract.RequestedLabel, contract.DeliveryTicksLeft.ToStringTicksToPeriod());
            case KinContractState.Completed:
                return "BEWH.Votann.Contract.InfoCompleted".Translate(contract.RequestedLabel);
            case KinContractState.Failed:
                return "BEWH.Votann.Contract.InfoFailed".Translate(contract.RequestedLabel);
            default:
                return "BEWH.Votann.Contract.InfoDesc".Translate(contract.RequestedLabel, contract.givenItem.label, contract.givenAmount, contract.OfferTicksLeft.ToStringTicksToPeriod());
        }
    }

    private void DrawTributeButton(Rect rect)
    {
        var debt = Mathf.CeilToInt(Mathf.Max(0f, gameComponentKinContracts.playerDebt));
        var pending = gameComponentKinContracts.TributeDrillPending;
        var label = pending
            ? "BEWH.Votann.Contract.TributePending".Translate()
            : "BEWH.Votann.Contract.PayTribute".Translate(debt);
        TooltipHandler.TipRegion(rect, "BEWH.Votann.Contract.PayTributeTooltip".Translate());
        if (Widgets.ButtonText(rect, label, active: debt > 0 && !pending))
        {
            gameComponentKinContracts.RequestTributeDrill(Find.AnyPlayerHomeMap);
        }
    }
}
