using System;
using System.Collections.Generic;
using System.Linq;
using RimWorld;
using UnityEngine;
using Verse;

namespace Votann40k;

public class GameComponent_KinContracts : GameComponent
{
    private const int TickCheckInterval = 250;
    private const int HostileGoodwill = -100;
    private const float FailedContractFlatDebt = 500f;
    private const float FailedContractDebtMult = 1.2f;
    private static readonly IntRange ReclamationDelayTicks = new(30000, 60000);

    private List<KinContract> kinContracts = [];
    public List<KinContract> KinContracts => kinContracts;

    private List<KinReclamationRequest> pendingReclamations = [];

    private int nextContractId;

    private Dictionary<StuffCategoryDef, List<ThingDef>> possibleItemsToGive;
    public Dictionary<StuffCategoryDef, List<ThingDef>> PossibleItemsToGive => possibleItemsToGive;

    private LinkedList<StuffCategoryDef> stuffWheel = [];

    private int nextRefreshTick = -1;

    private bool setupComplete;

    public int CurrentMaxContractAmount => (int)Math.Floor(contractMaxAmountByPlayerDebt.Evaluate(playerDebt));
    public int CurrentMaxContractAmountForView => Math.Max(1, kinContracts.Count);

    private readonly SimpleCurve contractMaxAmountByPlayerDebt =
    [
        new CurvePoint(5000, 0),
        new CurvePoint(2000, 1),
        new CurvePoint(0, 3),
        new CurvePoint(-1000, 5),
        new CurvePoint(-5000, 7),
        new CurvePoint(-10000, 10)
    ];

    public float CurrentContractPriceMultAmount => contractPriceMultByPlayerDebt.Evaluate(playerDebt);

    private readonly SimpleCurve contractPriceMultByPlayerDebt =
    [
        new CurvePoint(5000, 0.45f),
        new CurvePoint(2000, 0.6f),
        new CurvePoint(0, 0.75f),
        new CurvePoint(-1000, 0.9f),
        new CurvePoint(-5000, 1f),
        new CurvePoint(-10000, 1.35f)
    ];

    public float CurrentGoodwillByPlayerDebt => (float)Math.Floor(goodwillByPlayerDebt.Evaluate(playerDebt));

    private readonly SimpleCurve goodwillByPlayerDebt =
    [
        new CurvePoint(5000, -100),
        new CurvePoint(0, 0),
        new CurvePoint(-10000, 100)
    ];

    private static int MinimumMarketValue => Votann40kMod.VotannSettings?.minimumItemValueToTrade ?? 500;

    private static int RefreshIntervalTicks => Mathf.Max(1, Votann40kMod.VotannSettings?.contractRefreshDays ?? 5) * GenDate.TicksPerDay;

    //Positive is debt, negative is good
    public float playerDebt;

    public bool PlayerInDebt => playerDebt > 0;

    public Faction KinFaction => Find.FactionManager?.FirstFactionOfDef(Votann40kDefOf.BEWH_OffworldKinFaction);

    public bool KinHostile => KinFaction?.HostileTo(Faction.OfPlayer) ?? false;

    public bool CanRefresh => nextRefreshTick >= 0 && Find.TickManager.TicksGame >= nextRefreshTick;

    public string RefreshIn => Mathf.Max(0, nextRefreshTick - Find.TickManager.TicksGame).ToStringTicksToPeriod();

    public bool TributeDrillPending => Find.Maps.Any(map => map.listerThings.ThingsOfDef(Votann40kDefOf.BEWH_KinDrillSpawner).Any(t => t is KinDrillSpawner { IsTribute: true })
                                                             || map.listerThings.ThingsOfDef(Votann40kDefOf.BEWH_KinDrill).Any(t => t is Building_KinDrill { IsTribute: true }));

    public GameComponent_KinContracts(Game game)
    {
    }

    public override void FinalizeInit()
    {
        base.FinalizeInit();
        kinContracts ??= [];
        pendingReclamations ??= [];
        CalculateStuff();
    }

    private void CalculateStuff()
    {
        possibleItemsToGive = new Dictionary<StuffCategoryDef, List<ThingDef>>();
        stuffWheel = [];

        var allDefs = DefDatabase<ThingDef>.AllDefsListForReading;
        for (var i = 0; i < allDefs.Count; i++)
        {
            var stuff = allDefs[i];
            if (!stuff.IsStuff || stuff.stuffProps.categories.NullOrEmpty())
            {
                continue;
            }
            foreach (var stuffCategory in stuff.stuffProps.categories)
            {
                if (!possibleItemsToGive.TryGetValue(stuffCategory, out var list))
                {
                    possibleItemsToGive.Add(stuffCategory, [stuff]);
                    stuffWheel.AddLast(stuffCategory);
                }
                else
                {
                    list.Add(stuff);
                }
            }
        }
    }

    public KinContract GetContract(int id)
    {
        for (var i = 0; i < kinContracts.Count; i++)
        {
            if (kinContracts[i].id == id)
            {
                return kinContracts[i];
            }
        }

        return null;
    }

    public override void GameComponentTick()
    {
        var tick = Find.TickManager.TicksGame;
        if (tick % TickCheckInterval != 0)
        {
            return;
        }

        if (!Votann40kDefOf.TransportPod.IsFinished)
        {
            return;
        }

        if (!setupComplete)
        {
            setupComplete = true;
            nextRefreshTick = tick + RefreshIntervalTicks;
            TryMakeNewContracts();
            return;
        }

        CheckContractsInProgress(tick);
        FirePendingReclamations(tick);
    }

    private void CheckContractsInProgress(int tick)
    {
        for (var i = kinContracts.Count - 1; i >= 0; i--)
        {
            var contract = kinContracts[i];
            switch (contract.state)
            {
                case KinContractState.DrillEnRoute when tick > contract.drillArrivalTick + KinContract.DrillTravelTicks * 2:
                    // The drill spawner is gone without ever surfacing (e.g. map lost)
                    FailContract(contract, sendLetter: true);
                    break;
                case KinContractState.Loading when contract.drill == null || contract.drill.Destroyed:
                    FailContract(contract, sendLetter: true);
                    break;
                case KinContractState.Loading when tick >= contract.deliveryDeadline:
                    contract.drill.Depart();
                    break;
            }
        }
    }

    private void FirePendingReclamations(int tick)
    {
        for (var i = pendingReclamations.Count - 1; i >= 0; i--)
        {
            var request = pendingReclamations[i];
            if (tick < request.fireTick)
            {
                continue;
            }

            pendingReclamations.RemoveAt(i);
            KinReclamationUtility.Start(request, this);
        }
    }

    public void RefreshContracts()
    {
        nextRefreshTick = Find.TickManager.TicksGame + RefreshIntervalTicks;
        TryMakeNewContracts();
    }

    public void PruneInvalidContracts()
    {
        kinContracts.RemoveAll(contract => contract.Invalid);
    }

    private void TryMakeNewContracts()
    {
        kinContracts.RemoveAll(contract => !contract.InProgress || contract.Invalid);

        if (KinHostile)
        {
            return;
        }

        var max = CurrentMaxContractAmount;
        if (kinContracts.Count >= max)
        {
            return;
        }

        var map = Find.AnyPlayerHomeMap;
        if (map == null)
        {
            return;
        }

        var candidates = map.listerThings.GetAllThings(AllowedThing, true).ToList();
        while (kinContracts.Count < max && candidates.Count > 0)
        {
            var index = Rand.Range(0, candidates.Count);
            var requested = candidates[index];
            candidates.RemoveAt(index);

            var given = FindThingToSend(requested);
            if (given == null)
            {
                continue;
            }

            var amount = (int)Math.Floor(requested.MarketValue * requested.stackCount * CurrentContractPriceMultAmount / given.BaseMarketValue);
            if (amount <= 0)
            {
                continue;
            }

            kinContracts.Add(new KinContract(nextContractId++, given, amount, requested, map));
        }
    }

    private bool AllowedThing(Thing thing)
    {
        if (thing is Pawn or Corpse || thing.GetType().Name == "VehiclePawn")
        {
            return false;
        }

        if (thing.def.category != ThingCategory.Item || !thing.EverSeenByPlayer || thing.def == ThingDefOf.Silver)
        {
            return false;
        }

        if (thing.MarketValue * thing.stackCount <= MinimumMarketValue)
        {
            return false;
        }

        if (thing.IsForbidden(Faction.OfPlayer) || thing.IsBurning())
        {
            return false;
        }

        return ThingNotInUse(thing);
    }

    private bool ThingNotInUse(Thing thing)
    {
        for (var i = 0; i < kinContracts.Count; i++)
        {
            if (kinContracts[i].requestedItem == thing)
            {
                return false;
            }
        }

        return true;
    }

    private ThingDef FindThingToSend(Thing requestedItem)
    {
        if (stuffWheel.Count == 0)
        {
            return null;
        }

        var valueToMatch = requestedItem.MarketValue * requestedItem.stackCount * CurrentContractPriceMultAmount;
        var initialStuff = stuffWheel.First.Value;

        do
        {
            var matchesPrice = PossibleItemsToGive[stuffWheel.First.Value].Where(def => def.BaseMarketValue <= valueToMatch).ToList();
            RotateWheel();
            if (matchesPrice.Count > 0)
            {
                return matchesPrice.RandomElement();
            }
        } while (initialStuff != stuffWheel.First.Value);

        return null;
    }

    private void RotateWheel()
    {
        var first = stuffWheel.First;
        stuffWheel.RemoveFirst();
        stuffWheel.AddLast(first);
    }

    /// <summary>
    /// Player accepted an offered contract: a drill starts tunnelling towards the requested item.
    /// </summary>
    public void AcceptContract(KinContract contract)
    {
        if (contract.state != KinContractState.Offered || contract.Invalid || contract.OfferExpired)
        {
            return;
        }

        var map = contract.requestedItemMap ?? contract.requestedItem?.Map ?? Find.AnyPlayerHomeMap;
        if (map == null)
        {
            return;
        }

        contract.requestedItemMap = map;
        contract.Accept();
        var center = contract.requestedItem is { Spawned: true } ? contract.requestedItem.Position : DropCellFinder.TradeDropSpot(map);
        KinDrillSpawner.Place(map, center, contract.id, isTribute: false);

        Find.LetterStack.ReceiveLetter(
            "BEWH.Votann.Contract.AcceptedTitle".Translate(),
            "BEWH.Votann.Contract.AcceptedMessage".Translate(contract.RequestedLabel, contract.givenItem.label, contract.givenAmount, KinContract.DrillTravelTicks.ToStringTicksToPeriod()),
            LetterDefOf.NeutralEvent);
    }

    public void CompleteContract(KinContract contract)
    {
        if (contract.Closed)
        {
            return;
        }

        contract.state = KinContractState.Completed;
        contract.drill = null;
        playerDebt -= contract.requestedValue;
        SyncHostility();

        Find.LetterStack.ReceiveLetter(
            "BEWH.Votann.Contract.CompletedTitle".Translate(),
            "BEWH.Votann.Contract.CompletedMessage".Translate(contract.RequestedLabel),
            LetterDefOf.PositiveEvent);
    }

    public void FailContract(KinContract contract, bool sendLetter)
    {
        if (contract.Closed)
        {
            return;
        }

        contract.state = KinContractState.Failed;
        contract.drill = null;
        var penalty = Mathf.Max(contract.requestedValue * 0.25f, 100f);
        playerDebt += contract.requestedValue * FailedContractDebtMult + FailedContractFlatDebt;
        SyncHostility();

        var map = contract.requestedItemMap ?? Find.AnyPlayerHomeMap;
        if (map != null)
        {
            pendingReclamations.Add(new KinReclamationRequest
            {
                map = map,
                targetItem = contract.requestedItem,
                targetDef = contract.requestedDef,
                targetStuff = contract.requestedStuff,
                targetCount = contract.requestedCount,
                targetValue = contract.requestedValue,
                silverToTake = Mathf.CeilToInt(penalty),
                fireTick = Find.TickManager.TicksGame + ReclamationDelayTicks.RandomInRange
            });
        }

        if (sendLetter)
        {
            Find.LetterStack.ReceiveLetter(
                "BEWH.Votann.Contract.FailedTitle".Translate(),
                "BEWH.Votann.Contract.FailedMessage".Translate(contract.RequestedLabel, Mathf.CeilToInt(penalty)),
                LetterDefOf.NegativeEvent);
        }
    }

    /// <summary>
    /// Called when a reclamation party has taken something (item, silver or substitute goods) and left.
    /// </summary>
    public void Notify_ReclamationTook(float valueTaken)
    {
        playerDebt -= valueTaken;
        SyncHostility();
    }

    public void MakeHostile(string reason)
    {
        var kinFaction = KinFaction;
        if (kinFaction == null || kinFaction.HostileTo(Faction.OfPlayer))
        {
            return;
        }

        playerDebt = Mathf.Max(playerDebt, 5000f);
        kinFaction.SetRelationDirect(Faction.OfPlayer, FactionRelationKind.Hostile, true, reason);
        kinContracts.RemoveAll(contract => !contract.InProgress);
    }

    private void SyncHostility()
    {
        var kinFaction = KinFaction;
        if (kinFaction == null)
        {
            return;
        }

        var player = Faction.OfPlayer;
        var hostile = kinFaction.HostileTo(player);
        var goodwill = CurrentGoodwillByPlayerDebt;
        if (goodwill <= HostileGoodwill && !hostile)
        {
            kinFaction.SetRelationDirect(player, FactionRelationKind.Hostile, true, "BEWH.Votann.Contract.BecameHostile".Translate());
            kinContracts.RemoveAll(contract => !contract.InProgress);
        }
        else if (goodwill > HostileGoodwill && hostile)
        {
            kinFaction.SetRelationDirect(player, FactionRelationKind.Neutral, true, "BEWH.Votann.Contract.BecameNeutral".Translate());
        }
    }

    public void RequestTributeDrill(Map map)
    {
        if (map == null || TributeDrillPending)
        {
            return;
        }

        KinDrillSpawner.Place(map, DropCellFinder.TradeDropSpot(map), -1, isTribute: true);
        Messages.Message("BEWH.Votann.Contract.TributeDrillRequested".Translate(KinContract.DrillTravelTicks.ToStringTicksToPeriod()), MessageTypeDefOf.NeutralEvent);
    }

    public void PayTribute(int silver)
    {
        if (silver <= 0)
        {
            return;
        }

        playerDebt -= silver;
        SyncHostility();
        Messages.Message("BEWH.Votann.Contract.TributePaid".Translate(silver, Mathf.CeilToInt(Mathf.Max(0f, playerDebt))), MessageTypeDefOf.PositiveEvent);
    }

    public override void ExposeData()
    {
        Scribe_Collections.Look(ref kinContracts, "kinContracts", LookMode.Deep);
        Scribe_Collections.Look(ref pendingReclamations, "pendingReclamations", LookMode.Deep);
        Scribe_Values.Look(ref nextContractId, "nextContractId");
        Scribe_Values.Look(ref setupComplete, "setupComplete");
        Scribe_Values.Look(ref playerDebt, "playerDebt");
        Scribe_Values.Look(ref nextRefreshTick, "nextRefreshTick", -1);
    }
}
