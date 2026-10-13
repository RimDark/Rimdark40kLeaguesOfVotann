using RimWorld;
using VEF.Maps;
using Verse;
using Verse.AI;

namespace Votann40k;

/// <summary>
/// Adds the Kin contracts option to comms consoles.
/// </summary>
public class FloatMenuOptionProvider_KinContracts : FloatMenuOptionProvider
{
    protected override bool Drafted => true;
    protected override bool Undrafted => true;
    protected override bool Multiselect => false;

    protected override FloatMenuOption GetSingleOptionFor(Thing clickedThing, FloatMenuContext context)
    {
        if (clickedThing is not Building_CommsConsole console)
        {
            return null;
        }

        var myPawn = context.FirstSelectedPawn;
        var label = "BEWH.Votann.Contract.OpenContracts".Translate().CapitalizeFirst();

        if (!console.Powered())
        {
            return new FloatMenuOption(label + " (" + "CannotUseNoPower".Translate() + ")", null);
        }

        if (!Votann40kDefOf.TransportPod.IsFinished)
        {
            return new FloatMenuOption(label + " (" + "BEWH.Framework.Common.MissingResearch".Translate(Votann40kDefOf.TransportPod.LabelCap) + ")", null);
        }

        if (!myPawn.CanReach(console, PathEndMode.InteractionCell, Danger.Some))
        {
            return new FloatMenuOption(label + " (" + "NoPath".Translate().CapitalizeFirst() + ")", null);
        }

        return FloatMenuUtility.DecoratePrioritizedTask(new FloatMenuOption(label, delegate
        {
            myPawn.jobs.TryTakeOrderedJob(JobMaker.MakeJob(Votann40kDefOf.BEWH_OpenContractWindow, console), JobTag.Misc);
        }), myPawn, console);
    }
}
