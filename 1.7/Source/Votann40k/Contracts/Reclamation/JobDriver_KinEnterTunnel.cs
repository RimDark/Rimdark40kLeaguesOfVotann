using System.Collections.Generic;
using RimWorld;
using Verse;
using Verse.AI;
using Verse.AI.Group;

namespace Votann40k;

/// <summary>
/// Kin reclaimer climbs back down the tunnel hole with everything it carries.
/// </summary>
public class JobDriver_KinEnterTunnel : JobDriver
{
    private const int EnterTicks = 60;

    public override bool TryMakePreToilReservations(bool errorOnFailed)
    {
        return true;
    }

    protected override IEnumerable<Toil> MakeNewToils()
    {
        this.FailOnDespawnedOrNull(TargetIndex.A);
        yield return Toils_Goto.GotoThing(TargetIndex.A, PathEndMode.Touch);

        var wait = Toils_General.Wait(EnterTicks);
        wait.WithProgressBarToilDelay(TargetIndex.A);
        wait.FailOnDespawnedOrNull(TargetIndex.A);
        yield return wait;

        var enter = ToilMaker.MakeToil("KinEnterTunnel");
        enter.initAction = delegate
        {
            if (pawn.GetLord()?.LordJob is LordJob_KinReclamation lordJob)
            {
                lordJob.Notify_PawnEnteredTunnel(pawn);
            }

            FleckMaker.ThrowDustPuff(pawn.DrawPos, pawn.Map, 1.5f);
            pawn.ExitMap(false, Rot4.Invalid);
        };
        enter.defaultCompleteMode = ToilCompleteMode.Instant;
        yield return enter;
    }
}
