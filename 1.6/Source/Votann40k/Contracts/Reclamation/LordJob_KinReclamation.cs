using System.Collections.Generic;
using RimWorld;
using UnityEngine;
using Verse;
using Verse.AI.Group;

namespace Votann40k;

public enum KinReclamationPhase
{
    SeekItem,
    SeekSilver,
    SeekSubstitute,
    Return
}

/// <summary>
/// Non-hostile Kin debt collectors: dig straight to the requested item, then to the colony's silver, then leave through
/// the hole they came from. Turns into a normal assault if the player attacks them.
/// </summary>
public class LordJob_KinReclamation : LordJob
{
    private const int GiveUpAfterTicks = 60000;

    private KinReclamationRequest request;

    private Building_KinTunnelHole hole;

    private KinReclamationPhase phase = KinReclamationPhase.SeekItem;

    private int startTick;

    private float valueTaken;

    private bool itemMissingHandled;

    private bool finished;

    private readonly List<Thing> substituteExclusions = [];

    public override bool GuiltyOnDowned => true;

    public override bool CanBlockHostileVisitors => false;

    public override bool AddFleeToil => false;

    public KinReclamationRequest Request => request;

    public Building_KinTunnelHole Hole => hole;

    public KinReclamationPhase Phase => phase;

    public List<Thing> SubstituteExclusions => substituteExclusions;

    public LordJob_KinReclamation()
    {
    }

    public LordJob_KinReclamation(KinReclamationRequest request, Building_KinTunnelHole hole)
    {
        this.request = request;
        this.hole = hole;
        startTick = Find.TickManager.TicksGame;
    }

    public override StateGraph CreateGraph()
    {
        var stateGraph = new StateGraph();
        var reclaim = new LordToil_KinReclaim();
        stateGraph.AddToil(reclaim);

        var assault = new LordToil_AssaultColony(attackDownedIfStarving: false, canPickUpOpportunisticWeapons: true)
        {
            useAvoidGrid = true
        };
        stateGraph.AddToil(assault);

        var harmed = new Transition(reclaim, assault);
        harmed.AddTrigger(new Trigger_PawnHarmed(1f, requireInstigatorWithFaction: true, requireInstigatorWithSpecificFaction: Faction.OfPlayer));
        harmed.AddPreAction(new TransitionAction_Custom(() =>
        {
            Current.Game.GetComponent<GameComponent_KinContracts>()?.MakeHostile("BEWH.Votann.Reclamation.AttackedReason".Translate());
        }));
        harmed.AddPreAction(new TransitionAction_Letter("BEWH.Votann.Reclamation.AttackedTitle".Translate(), "BEWH.Votann.Reclamation.AttackedMessage".Translate(), LetterDefOf.ThreatBig));
        harmed.AddPostAction(new TransitionAction_EndAllJobs());
        stateGraph.AddTransition(harmed);

        return stateGraph;
    }

    public override void LordJobTick()
    {
        base.LordJobTick();
        if (phase != KinReclamationPhase.Return && Find.TickManager.TicksGame - startTick > GiveUpAfterTicks)
        {
            SetPhase(KinReclamationPhase.Return);
        }
    }

    public void SetPhase(KinReclamationPhase newPhase)
    {
        if (phase == newPhase)
        {
            return;
        }

        phase = newPhase;
        for (var i = 0; i < lord.ownedPawns.Count; i++)
        {
            var pawn = lord.ownedPawns[i];
            if (pawn.jobs?.curJob != null && pawn.CurJobDef != Votann40kDefOf.BEWH_KinEnterTunnel)
            {
                pawn.jobs.CheckForJobOverride();
            }
        }
    }

    public int CarriedCount(ThingDef def, ThingDef stuff)
    {
        var count = 0;
        for (var i = 0; i < lord.ownedPawns.Count; i++)
        {
            var container = lord.ownedPawns[i].inventory?.innerContainer;
            if (container == null)
            {
                continue;
            }

            for (var j = 0; j < container.Count; j++)
            {
                if (container[j].def == def && container[j].Stuff == stuff)
                {
                    count += container[j].stackCount;
                }
            }
        }

        return count;
    }

    public float CarriedValue()
    {
        var value = 0f;
        for (var i = 0; i < lord.ownedPawns.Count; i++)
        {
            var container = lord.ownedPawns[i].inventory?.innerContainer;
            if (container == null)
            {
                continue;
            }

            for (var j = 0; j < container.Count; j++)
            {
                value += container[j].MarketValue * container[j].stackCount;
            }
        }

        return value;
    }

    public bool ItemSecured => CarriedCount(request.targetDef, request.targetStuff) >= request.targetCount;

    public int SilverStillWanted => Mathf.Max(0, request.silverToTake - CarriedCount(ThingDefOf.Silver, null));

    public float SubstituteValueWanted => Mathf.Max(0f, request.targetValue * 1.25f + request.silverToTake - CarriedValue());

    /// <summary>
    /// Called by the job giver when no matching item exists on the map any more.
    /// </summary>
    public void Notify_ItemMissing()
    {
        if (itemMissingHandled)
        {
            return;
        }

        itemMissingHandled = true;
        Current.Game.GetComponent<GameComponent_KinContracts>()?.MakeHostile("BEWH.Votann.Reclamation.ItemMissingReason".Translate());
        Messages.Message("BEWH.Votann.Reclamation.ItemMissing".Translate(), MessageTypeDefOf.ThreatBig);
        SetPhase(KinReclamationPhase.SeekSilver);
    }

    public void Notify_PawnEnteredTunnel(Pawn pawn)
    {
        var container = pawn.inventory?.innerContainer;
        if (container != null)
        {
            for (var i = 0; i < container.Count; i++)
            {
                valueTaken += container[i].MarketValue * container[i].stackCount;
            }
        }
    }

    public override void Notify_PawnLost(Pawn p, PawnLostCondition condition)
    {
        base.Notify_PawnLost(p, condition);
        if (lord.ownedPawns.Count == 0)
        {
            Finish();
        }
    }

    private void Finish()
    {
        if (finished)
        {
            return;
        }

        finished = true;
        Current.Game.GetComponent<GameComponent_KinContracts>()?.Notify_ReclamationTook(valueTaken);
        if (valueTaken > 0f)
        {
            Messages.Message("BEWH.Votann.Reclamation.Left".Translate(Mathf.CeilToInt(valueTaken)), MessageTypeDefOf.NeutralEvent);
        }
        if (hole != null && hole.Spawned)
        {
            hole.Seal();
        }
    }

    public override void ExposeData()
    {
        base.ExposeData();
        Scribe_Deep.Look(ref request, "request");
        Scribe_References.Look(ref hole, "hole");
        Scribe_Values.Look(ref phase, "phase", KinReclamationPhase.SeekItem);
        Scribe_Values.Look(ref startTick, "startTick");
        Scribe_Values.Look(ref valueTaken, "valueTaken");
        Scribe_Values.Look(ref itemMissingHandled, "itemMissingHandled");
        Scribe_Values.Look(ref finished, "finished");
    }
}
