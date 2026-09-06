using RimWorld;
using UnityEngine;
using Verse;

namespace Votann40k;

public enum KinContractState
{
    Offered,
    DrillEnRoute,
    Loading,
    Completed,
    Failed
}

public class KinContract : IExposable
{
    private static readonly IntRange DeliveryWindowTicks = new(60000, 240000);

    public const int DrillTravelTicks = 5000;

    public int id = -1;

    public KinContractState state = KinContractState.Offered;

    // Offer disappears at this tick if not accepted
    public int offerExpiresOn;

    public int drillArrivalTick = -1;

    public int deliveryDeadline = -1;

    public Thing requestedItem;
    public ThingDef requestedDef;
    public ThingDef requestedStuff;
    public int requestedQuality = -1;
    public int requestedCount;
    public float requestedValue;
    public Map requestedItemMap;

    public ThingDef givenItem;
    public int givenAmount;

    public Building_KinDrill drill;

    public bool Invalid => requestedDef == null || givenItem == null || givenAmount <= 0 || requestedCount <= 0 || (state == KinContractState.Offered && (requestedItem == null || requestedItem.Destroyed));
    public bool OfferExpired => state == KinContractState.Offered && Find.TickManager.TicksGame >= offerExpiresOn;
    public bool InProgress => state is KinContractState.DrillEnRoute or KinContractState.Loading;
    public bool Closed => state is KinContractState.Completed or KinContractState.Failed;
    public int OfferTicksLeft => Mathf.Max(0, offerExpiresOn - Find.TickManager.TicksGame);
    public int DrillTicksLeft => Mathf.Max(0, drillArrivalTick - Find.TickManager.TicksGame);
    public int DeliveryTicksLeft => Mathf.Max(0, deliveryDeadline - Find.TickManager.TicksGame);

    public KinContract()
    {
    }

    public KinContract(int id, ThingDef itemGiven, int amount, Thing itemRequested, Map map)
    {
        this.id = id;
        givenItem = itemGiven;
        givenAmount = amount;
        requestedItem = itemRequested;
        requestedDef = itemRequested.def;
        requestedStuff = itemRequested.Stuff;
        requestedCount = itemRequested.stackCount;
        requestedValue = itemRequested.MarketValue * itemRequested.stackCount;
        requestedQuality = itemRequested.TryGetQuality(out var quality) ? (int)quality : -1;
        requestedItemMap = map;
        offerExpiresOn = Find.TickManager.TicksGame + DeliveryWindowTicks.RandomInRange;
    }

    public string RequestedLabel => GenLabel.ThingLabel(requestedDef, requestedStuff, requestedCount).CapitalizeFirst();

    public bool Matches(Thing thing)
    {
        if (thing == null || thing.def != requestedDef || thing.Stuff != requestedStuff)
        {
            return false;
        }

        if (requestedQuality >= 0 && (!thing.TryGetQuality(out var quality) || (int)quality < requestedQuality))
        {
            return false;
        }

        return true;
    }

    public void Accept()
    {
        state = KinContractState.DrillEnRoute;
        drillArrivalTick = Find.TickManager.TicksGame + DrillTravelTicks;
        deliveryDeadline = drillArrivalTick + DeliveryWindowTicks.RandomInRange;
    }

    public void NotifyDrillArrived(Building_KinDrill arrivedDrill)
    {
        drill = arrivedDrill;
        state = KinContractState.Loading;
    }

    public string GetStatusLabel()
    {
        switch (state)
        {
            case KinContractState.DrillEnRoute:
                return "BEWH.Votann.Contract.StatusDrillEnRoute".Translate(DrillTicksLeft.ToStringTicksToPeriod());
            case KinContractState.Loading:
                return "BEWH.Votann.Contract.StatusLoading".Translate(DeliveryTicksLeft.ToStringTicksToPeriod());
            case KinContractState.Completed:
                return "BEWH.Votann.Contract.StatusCompleted".Translate();
            case KinContractState.Failed:
                return "BEWH.Votann.Contract.StatusFailed".Translate();
            default:
                return OfferExpired ? "BEWH.Votann.Contract.StatusExpired".Translate() : "BEWH.Votann.Contract.StatusAvailable".Translate();
        }
    }

    public string GetTimeLabel()
    {
        switch (state)
        {
            case KinContractState.DrillEnRoute:
                return DrillTicksLeft.ToStringTicksToPeriod();
            case KinContractState.Loading:
                return DeliveryTicksLeft.ToStringTicksToPeriod();
            case KinContractState.Offered:
                return OfferTicksLeft.ToStringTicksToPeriod();
            default:
                return "-";
        }
    }

    public void JumpToThingRequested()
    {
        if (requestedItem != null && !requestedItem.Destroyed && requestedItem.Spawned)
        {
            CameraJumper.TryJumpAndSelect(requestedItem);
        }
    }

    public void JumpToDrill()
    {
        if (drill != null && !drill.Destroyed && drill.Spawned)
        {
            CameraJumper.TryJumpAndSelect(drill);
        }
    }

    public void ExposeData()
    {
        Scribe_Values.Look(ref id, "id", -1);
        Scribe_Values.Look(ref state, "state", KinContractState.Offered);
        Scribe_Values.Look(ref offerExpiresOn, "offerExpiresOn");
        Scribe_Values.Look(ref drillArrivalTick, "drillArrivalTick", -1);
        Scribe_Values.Look(ref deliveryDeadline, "deliveryDeadline", -1);
        Scribe_References.Look(ref requestedItem, "requestedItem");
        Scribe_Defs.Look(ref requestedDef, "requestedDef");
        Scribe_Defs.Look(ref requestedStuff, "requestedStuff");
        Scribe_Values.Look(ref requestedQuality, "requestedQuality", -1);
        Scribe_Values.Look(ref requestedCount, "requestedCount");
        Scribe_Values.Look(ref requestedValue, "requestedValue");
        Scribe_References.Look(ref requestedItemMap, "requestedItemMap");
        Scribe_Defs.Look(ref givenItem, "givenItem");
        Scribe_Values.Look(ref givenAmount, "givenAmount");
        Scribe_References.Look(ref drill, "drill");
    }
}
