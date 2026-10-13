using RimWorld;
using Verse;
using Verse.AI;

namespace Votann40k;

/// <summary>
/// Adds the option to carry a player Kin corpse to the Ancestor Core.
/// </summary>
public class FloatMenuOptionProvider_UploadKinToAncestorCore : FloatMenuOptionProvider
{
    protected override bool Drafted => true;
    protected override bool Undrafted => true;
    protected override bool Multiselect => false;
    protected override bool RequiresManipulation => true;

    protected override FloatMenuOption GetSingleOptionFor(Thing clickedThing, FloatMenuContext context)
    {
        if (clickedThing is not Corpse corpse || !corpse.Spawned || !Votann40kUtils.IsPlayerKinCorpse(corpse))
        {
            return null;
        }

        var ancestorCore = Votann40kUtils.AncestorCoreOnMap(corpse.Map);
        if (ancestorCore == null)
        {
            return null;
        }

        var selPawn = context.FirstSelectedPawn;
        var label = "BEWH.Votann.Kin.UploadToAncestorCore".Translate(corpse.InnerPawn).CapitalizeFirst();

        if (!ancestorCore.PowerOn)
        {
            return new FloatMenuOption(label + ": " + "NoPower".Translate().CapitalizeFirst(), null);
        }

        if (!selPawn.CanReach(corpse, PathEndMode.ClosestTouch, Danger.Deadly))
        {
            return new FloatMenuOption(label + ": " + "NoPath".Translate().CapitalizeFirst(), null);
        }

        return FloatMenuUtility.DecoratePrioritizedTask(new FloatMenuOption(label, delegate
        {
            selPawn.jobs.TryTakeOrderedJob(JobMaker.MakeJob(Votann40kDefOf.BEWH_CarryKinToAncestorCore, ancestorCore, corpse), JobTag.Misc);
        }), selPawn, corpse);
    }
}
