using System;
using System.Collections.Generic;
using System.Text;
using RimWorld;
using UnityEngine;
using Verse;
using Verse.Sound;

namespace Votann40k;

/// <summary>
/// Kin drill that surfaces near the requested item. For a contract it drops the Kin's goods on arrival and waits to be
/// loaded with the requested item; for a tribute it accepts silver. Leaves on its own when the deadline passes.
/// </summary>
[StaticConstructorOnStartup]
public class Building_KinDrill : Building, IThingHolder
{
    private const int TributeWindowTicks = 120000;
    private const int DepartDurationTicks = 180;

    private static readonly Texture2D SendIcon = ContentFinder<Texture2D>.Get("UI/Commands/LaunchShip");
    private static readonly Texture2D UnloadIcon = ContentFinder<Texture2D>.Get("UI/Designators/Cancel");

    private ThingOwner<Thing> innerContainer;

    private int contractId = -1;

    private int leaveTick = -1;

    private int departingSinceTick = -1;

    private bool resolved;

    [Unsaved(false)]
    private Effecter departEffecter;

    public Building_KinDrill()
    {
        innerContainer = new ThingOwner<Thing>(this);
    }

    public bool IsTribute => contractId < 0;

    public bool Departing => departingSinceTick >= 0;

    private GameComponent_KinContracts Component => Current.Game.GetComponent<GameComponent_KinContracts>();

    private KinContract Contract => IsTribute ? null : Component?.GetContract(contractId);

    public void SetContract(int id)
    {
        contractId = id;
    }

    public bool Accepts(Thing thing)
    {
        if (thing == null || Departing)
        {
            return false;
        }

        if (IsTribute)
        {
            return thing.def == ThingDefOf.Silver;
        }

        var contract = Contract;
        return contract != null && contract.state == KinContractState.Loading && contract.Matches(thing);
    }

    public int LoadedCount
    {
        get
        {
            var count = 0;
            for (var i = 0; i < innerContainer.Count; i++)
            {
                if (IsTribute ? innerContainer[i].def == ThingDefOf.Silver : Contract?.Matches(innerContainer[i]) == true)
                {
                    count += innerContainer[i].stackCount;
                }
            }

            return count;
        }
    }

    public int NeededCount => IsTribute ? (int)MathF.Ceiling(Math.Max(0f, Component?.playerDebt ?? 0f)) : Contract?.requestedCount ?? 0;

    public int StillNeeded => Math.Max(0, NeededCount - LoadedCount);

    public bool IsLoaded => IsTribute ? LoadedCount > 0 : Contract != null && LoadedCount >= NeededCount;

    public bool WantsMore => !Departing && StillNeeded > 0 && (IsTribute || Contract is { state: KinContractState.Loading });

    public int TicksUntilLeave => Math.Max(0, leaveTick - Find.TickManager.TicksGame);

    public override void SpawnSetup(Map map, bool respawningAfterLoad)
    {
        base.SpawnSetup(map, respawningAfterLoad);
        if (respawningAfterLoad)
        {
            return;
        }

        if (IsTribute)
        {
            leaveTick = Find.TickManager.TicksGame + TributeWindowTicks;
            Messages.Message("BEWH.Votann.Drill.TributeArrived".Translate(), this, MessageTypeDefOf.NeutralEvent);
            return;
        }

        var contract = Contract;
        if (contract == null || contract.state != KinContractState.DrillEnRoute)
        {
            leaveTick = Find.TickManager.TicksGame + 2500;
            return;
        }

        contract.NotifyDrillArrived(this);
        leaveTick = contract.deliveryDeadline;
        DropKinGoods(contract);

        Find.LetterStack.ReceiveLetter(
            "BEWH.Votann.Drill.ArrivedTitle".Translate(),
            "BEWH.Votann.Drill.ArrivedMessage".Translate(contract.givenItem.label, contract.givenAmount, contract.RequestedLabel, TicksUntilLeave.ToStringTicksToPeriod()),
            LetterDefOf.PositiveEvent,
            this);
    }

    private void DropKinGoods(KinContract contract)
    {
        var remaining = contract.givenAmount;
        while (remaining > 0)
        {
            var stack = ThingMaker.MakeThing(contract.givenItem);
            stack.stackCount = Math.Min(remaining, contract.givenItem.stackLimit);
            remaining -= stack.stackCount;
            GenPlace.TryPlaceThing(stack, Position, Map, ThingPlaceMode.Near);
        }
    }

    protected override void Tick()
    {
        base.Tick();
        if (Departing)
        {
            departEffecter?.EffectTick(this, this);
            if (Find.TickManager.TicksGame - departingSinceTick >= DepartDurationTicks)
            {
                FinishDeparture();
            }
            return;
        }

        if (this.IsHashIntervalTick(250) && leaveTick >= 0 && Find.TickManager.TicksGame >= leaveTick)
        {
            Depart();
        }
    }

    /// <summary>
    /// Resolve the contract/tribute with whatever is loaded, then tunnel away.
    /// </summary>
    public void Depart()
    {
        if (Departing)
        {
            return;
        }

        Resolve();
        departingSinceTick = Find.TickManager.TicksGame;
        departEffecter = Votann40kDefOf.EmergencePointSustained2X2?.Spawn(this, Map);
        SoundDefOf.Tunnel?.PlayOneShot(SoundInfo.InMap(this));
    }

    private void Resolve()
    {
        if (resolved)
        {
            return;
        }

        resolved = true;
        var component = Component;
        if (component == null)
        {
            return;
        }

        if (IsTribute)
        {
            component.PayTribute(LoadedCount);
            return;
        }

        var contract = Contract;
        if (contract == null)
        {
            return;
        }

        if (IsLoaded)
        {
            component.CompleteContract(contract);
        }
        else
        {
            component.FailContract(contract, sendLetter: true);
        }
    }

    private void FinishDeparture()
    {
        departEffecter?.Cleanup();
        departEffecter = null;
        var map = Map;
        var position = Position;
        Votann40kDefOf.EmergencePointComplete2X2?.SpawnMaintained(position, map);
        innerContainer.ClearAndDestroyContents();
        Destroy();
    }

    public override void Destroy(DestroyMode mode = DestroyMode.Vanish)
    {
        if (!resolved && mode != DestroyMode.Vanish)
        {
            Resolve();
        }

        departEffecter?.Cleanup();
        departEffecter = null;
        if (Spawned && innerContainer.Count > 0)
        {
            innerContainer.TryDropAll(Position, Map, ThingPlaceMode.Near);
        }

        base.Destroy(mode);
    }

    public void UnloadAll()
    {
        if (Departing || !Spawned)
        {
            return;
        }

        innerContainer.TryDropAll(Position, Map, ThingPlaceMode.Near);
    }

    public override IEnumerable<Gizmo> GetGizmos()
    {
        foreach (var gizmo in base.GetGizmos())
        {
            yield return gizmo;
        }

        if (Departing)
        {
            yield break;
        }

        var send = new Command_Action
        {
            defaultLabel = IsTribute ? "BEWH.Votann.Drill.SendTribute".Translate() : "BEWH.Votann.Drill.Send".Translate(),
            defaultDesc = IsTribute ? "BEWH.Votann.Drill.SendTributeDesc".Translate() : "BEWH.Votann.Drill.SendDesc".Translate(),
            icon = SendIcon,
            action = Depart
        };
        if (!IsLoaded)
        {
            send.Disable(IsTribute ? "BEWH.Votann.Drill.NoSilverLoaded".Translate() : "BEWH.Votann.Drill.NotLoaded".Translate(StillNeeded));
        }
        yield return send;

        var unload = new Command_Action
        {
            defaultLabel = "BEWH.Votann.Drill.Unload".Translate(),
            defaultDesc = "BEWH.Votann.Drill.UnloadDesc".Translate(),
            icon = UnloadIcon,
            action = UnloadAll
        };
        if (innerContainer.Count == 0)
        {
            unload.Disable();
        }
        yield return unload;

        if (DebugSettings.ShowDevGizmos)
        {
            yield return new Command_Action
            {
                defaultLabel = "DEV: Leave now",
                action = Depart
            };
        }
    }

    public override string GetInspectString()
    {
        var stringBuilder = new StringBuilder(base.GetInspectString());
        if (Departing)
        {
            stringBuilder.AppendLineIfNotEmpty().Append("BEWH.Votann.Drill.Departing".Translate());
            return stringBuilder.ToString();
        }

        if (IsTribute)
        {
            stringBuilder.AppendLineIfNotEmpty().Append("BEWH.Votann.Drill.TributeLoaded".Translate(LoadedCount));
        }
        else if (Contract != null)
        {
            stringBuilder.AppendLineIfNotEmpty().Append("BEWH.Votann.Drill.Loaded".Translate(Contract.RequestedLabel, LoadedCount, NeededCount));
        }

        stringBuilder.AppendLineIfNotEmpty().Append("BEWH.Votann.Drill.LeavesIn".Translate(TicksUntilLeave.ToStringTicksToPeriod()));
        return stringBuilder.ToString();
    }

    public ThingOwner GetDirectlyHeldThings()
    {
        return innerContainer;
    }

    public void GetChildHolders(List<IThingHolder> outChildren)
    {
        ThingOwnerUtility.AppendThingHoldersFromThings(outChildren, GetDirectlyHeldThings());
    }

    public override void ExposeData()
    {
        base.ExposeData();
        Scribe_Deep.Look(ref innerContainer, "innerContainer", this);
        Scribe_Values.Look(ref contractId, "contractId", -1);
        Scribe_Values.Look(ref leaveTick, "leaveTick", -1);
        Scribe_Values.Look(ref departingSinceTick, "departingSinceTick", -1);
        Scribe_Values.Look(ref resolved, "resolved");
        if (Scribe.mode == LoadSaveMode.PostLoadInit)
        {
            innerContainer ??= new ThingOwner<Thing>(this);
        }
    }
}
