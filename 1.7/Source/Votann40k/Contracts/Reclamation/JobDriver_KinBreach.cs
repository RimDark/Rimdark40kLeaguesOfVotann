using System.Collections.Generic;
using RimWorld;
using Verse;
using Verse.AI;

namespace Votann40k;

/// <summary>
/// Kin reclaimer smashes through whatever blocks the way: walk to the cell in front of it, work for a moment, then bring it down.
/// Target A is the blocking building, target B the cell to stand on.
/// </summary>
public class JobDriver_KinBreach : JobDriver
{
    private const int BreachTicks = 90;
    private const float BreachDamage = 10000f;

    private Thing Blocker => job.GetTarget(TargetIndex.A).Thing;

    public override bool TryMakePreToilReservations(bool errorOnFailed)
    {
        return true;
    }

    protected override IEnumerable<Toil> MakeNewToils()
    {
        this.FailOnDestroyedOrNull(TargetIndex.A);
        var goTo = Toils_Goto.GotoCell(TargetIndex.B, PathEndMode.OnCell);
        goTo.FailOn(() => !pawn.Position.InHorDistOf(job.GetTarget(TargetIndex.B).Cell, 30f) && !pawn.CanReach(job.GetTarget(TargetIndex.B), PathEndMode.OnCell, Danger.Deadly));
        yield return goTo;

        var work = Toils_General.Wait(BreachTicks);
        work.WithProgressBarToilDelay(TargetIndex.A);
        work.FailOnDestroyedOrNull(TargetIndex.A);
        work.tickAction = delegate
        {
            pawn.rotationTracker.FaceTarget(Blocker);
            if (pawn.IsHashIntervalTick(15))
            {
                FleckMaker.ThrowDustPuff(Blocker.DrawPos, pawn.Map, 1.2f);
            }
        };
        yield return work;

        var smash = ToilMaker.MakeToil("KinBreach");
        smash.initAction = delegate
        {
            var blocker = Blocker;
            if (blocker == null || blocker.Destroyed)
            {
                return;
            }

            if (blocker is Building_Door door && door.PawnCanOpen(pawn))
            {
                return;
            }

            blocker.TakeDamage(new DamageInfo(DamageDefOf.Blunt, BreachDamage, 999f, -1f, pawn, null, null, DamageInfo.SourceCategory.ThingOrUnknown, null, instigatorGuilty: false));
        };
        smash.defaultCompleteMode = ToilCompleteMode.Instant;
        yield return smash;
    }
}
