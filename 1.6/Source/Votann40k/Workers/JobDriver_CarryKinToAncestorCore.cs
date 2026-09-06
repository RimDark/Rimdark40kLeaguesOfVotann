using System.Collections.Generic;
using Verse;
using Verse.AI;

namespace Votann40k;

public class JobDriver_CarryKinToAncestorCore : JobDriver
{
    private const int Duration = 200;

    private Building_AncestorCore AncestorCore => job.GetTarget(TargetIndex.A).Thing as Building_AncestorCore;

    private Corpse Corpse => job.GetTarget(TargetIndex.B).Thing as Corpse;

    public override bool TryMakePreToilReservations(bool errorOnFailed)
    {
        if (!pawn.Reserve(AncestorCore, job, 1, 1, null, errorOnFailed))
        {
            return false;
        }
        if (!pawn.Reserve(Corpse, job, 1, 1, null, errorOnFailed))
        {
            return false;
        }
        return true;
    }

    protected override IEnumerable<Toil> MakeNewToils()
    {
        this.FailOnDespawnedNullOrForbidden(TargetIndex.A);
        this.FailOnBurningImmobile(TargetIndex.A);
        this.FailOn(() => AncestorCore == null || !AncestorCore.PowerOn || !AncestorCore.CanUploadCorpse(Corpse));
        job.count = 1;
        yield return Toils_Goto.GotoThing(TargetIndex.B, PathEndMode.ClosestTouch).FailOnDespawnedNullOrForbidden(TargetIndex.B).FailOnSomeonePhysicallyInteracting(TargetIndex.B);
        yield return Toils_Haul.StartCarryThing(TargetIndex.B).FailOnDestroyedNullOrForbidden(TargetIndex.B);
        yield return Toils_Goto.GotoThing(TargetIndex.A, PathEndMode.Touch);
        yield return Toils_General.Wait(Duration)
            .FailOnDestroyedNullOrForbidden(TargetIndex.B)
            .FailOnDestroyedNullOrForbidden(TargetIndex.A)
            .FailOnCannotTouch(TargetIndex.A, PathEndMode.Touch)
            .WithProgressBarToilDelay(TargetIndex.A);
        var upload = ToilMaker.MakeToil("UploadKin");
        upload.initAction = delegate
        {
            AncestorCore.UploadKinToCore(Corpse);
        };
        upload.defaultCompleteMode = ToilCompleteMode.Instant;
        yield return upload;
    }
}
