using RimWorld;
using UnityEngine;
using Verse;
using Verse.AI;
using Verse.AI.Group;

namespace Votann40k;

/// <summary>
/// Drives a reclamation party member: walk (and breach) towards the current target, grab it, then head back to the tunnel.
/// </summary>
public class JobGiver_KinReclaim : ThinkNode_JobGiver
{
    private const int JobExpiry = 180;

    protected override Job TryGiveJob(Pawn pawn)
    {
        if (pawn.GetLord()?.LordJob is not LordJob_KinReclamation lordJob)
        {
            return null;
        }

        switch (lordJob.Phase)
        {
            case KinReclamationPhase.SeekItem:
                return SeekItem(pawn, lordJob);
            case KinReclamationPhase.SeekSilver:
                return SeekSilver(pawn, lordJob);
            case KinReclamationPhase.SeekSubstitute:
                return SeekSubstitute(pawn, lordJob);
            default:
                return ReturnToTunnel(pawn, lordJob);
        }
    }

    private Job SeekItem(Pawn pawn, LordJob_KinReclamation lordJob)
    {
        if (lordJob.ItemSecured)
        {
            lordJob.SetPhase(KinReclamationPhase.SeekSilver);
            return SeekSilver(pawn, lordJob);
        }

        var target = KinReclamationUtility.FindTarget(pawn.Map, lordJob.Request);
        if (target == null)
        {
            lordJob.Notify_ItemMissing();
            return SeekSilver(pawn, lordJob);
        }

        var wanted = lordJob.Request.targetCount - lordJob.CarriedCount(lordJob.Request.targetDef, lordJob.Request.targetStuff);
        return ApproachOrTake(pawn, target, Mathf.Clamp(wanted, 1, target.stackCount));
    }

    private Job SeekSilver(Pawn pawn, LordJob_KinReclamation lordJob)
    {
        var wanted = lordJob.SilverStillWanted;
        if (wanted <= 0)
        {
            return NextAfterSilver(pawn, lordJob);
        }

        var silver = KinReclamationUtility.FindSilver(pawn.Map, pawn.Position);
        if (silver == null)
        {
            return NextAfterSilver(pawn, lordJob);
        }

        return ApproachOrTake(pawn, silver, Mathf.Min(wanted, silver.stackCount));
    }

    private Job NextAfterSilver(Pawn pawn, LordJob_KinReclamation lordJob)
    {
        if (!lordJob.ItemSecured && lordJob.SubstituteValueWanted > 0f)
        {
            lordJob.SetPhase(KinReclamationPhase.SeekSubstitute);
            return SeekSubstitute(pawn, lordJob);
        }

        lordJob.SetPhase(KinReclamationPhase.Return);
        return ReturnToTunnel(pawn, lordJob);
    }

    private Job SeekSubstitute(Pawn pawn, LordJob_KinReclamation lordJob)
    {
        var valueWanted = lordJob.SubstituteValueWanted;
        if (valueWanted <= 0f)
        {
            lordJob.SetPhase(KinReclamationPhase.Return);
            return ReturnToTunnel(pawn, lordJob);
        }

        var substitute = KinReclamationUtility.FindSubstituteItem(pawn.Map, valueWanted, lordJob.SubstituteExclusions);
        if (substitute == null)
        {
            lordJob.SetPhase(KinReclamationPhase.Return);
            return ReturnToTunnel(pawn, lordJob);
        }

        var unitValue = Mathf.Max(0.01f, substitute.MarketValue);
        var count = Mathf.Clamp(Mathf.CeilToInt(valueWanted / unitValue), 1, substitute.stackCount);
        return ApproachOrTake(pawn, substitute, count);
    }

    private Job ApproachOrTake(Pawn pawn, Thing target, int count)
    {
        if (pawn.Position.AdjacentTo8WayOrInside(target) || pawn.CanReach(target, PathEndMode.ClosestTouch, Danger.Deadly))
        {
            if (!pawn.CanReserve(target, 10, count))
            {
                return Wait(pawn);
            }

            var take = JobMaker.MakeJob(JobDefOf.TakeInventory, target);
            take.count = count;
            take.expiryInterval = JobExpiry * 2;
            take.checkOverrideOnExpire = true;
            return take;
        }

        return ApproachOrBreach(pawn, target, PathEndMode.ClosestTouch);
    }

    private Job ReturnToTunnel(Pawn pawn, LordJob_KinReclamation lordJob)
    {
        var hole = lordJob.Hole;
        if (hole == null || !hole.Spawned)
        {
            if (RCellFinder.TryFindBestExitSpot(pawn, out var exit))
            {
                var leave = JobMaker.MakeJob(JobDefOf.Goto, exit);
                leave.exitMapOnArrival = true;
                leave.expiryInterval = JobExpiry * 4;
                leave.checkOverrideOnExpire = true;
                return leave;
            }

            return Wait(pawn);
        }

        if (pawn.Position.AdjacentTo8WayOrInside(hole) || pawn.CanReach(hole, PathEndMode.Touch, Danger.Deadly))
        {
            return JobMaker.MakeJob(Votann40kDefOf.BEWH_KinEnterTunnel, hole);
        }

        return ApproachOrBreach(pawn, hole, PathEndMode.Touch);
    }

    private Job ApproachOrBreach(Pawn pawn, Thing target, PathEndMode peMode)
    {
        var traverse = TraverseParms.For(pawn, Danger.Deadly, TraverseMode.PassAllDestroyableThings, canBashDoors: true);
        using var path = pawn.Map.pathFinder.FindPathNow(pawn.Position, target, traverse, null, peMode);
        if (path == null || !path.Found)
        {
            return Wait(pawn);
        }

        var blocker = path.FirstBlockingBuilding(out var cellBefore, pawn);
        if (blocker != null && cellBefore.IsValid)
        {
            var breach = JobMaker.MakeJob(Votann40kDefOf.BEWH_KinBreach, blocker, cellBefore);
            breach.expiryInterval = JobExpiry * 3;
            breach.checkOverrideOnExpire = true;
            return breach;
        }

        var nodes = path.NodesReversed;
        var destination = nodes.Count > 0 ? nodes[0] : target.Position;
        var goTo = JobMaker.MakeJob(JobDefOf.Goto, destination, JobExpiry, checkOverrideOnExpiry: true);
        goTo.canBashDoors = true;
        goTo.locomotionUrgency = LocomotionUrgency.Jog;
        return goTo;
    }

    private static Job Wait(Pawn pawn)
    {
        var wait = JobMaker.MakeJob(JobDefOf.Wait);
        wait.expiryInterval = 60;
        wait.checkOverrideOnExpire = true;
        return wait;
    }
}
