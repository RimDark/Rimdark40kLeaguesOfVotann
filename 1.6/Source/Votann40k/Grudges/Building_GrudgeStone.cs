using System.Collections.Generic;
using System.Text;
using RimWorld;
using Verse;
using Verse.AI;

namespace Votann40k;

public class Building_GrudgeStone : Building
{
    public override IEnumerable<FloatMenuOption> GetFloatMenuOptions(Pawn selPawn)
    {
        foreach (var option in base.GetFloatMenuOptions(selPawn))
        {
            yield return option;
        }

        var label = "BEWH.Votann.Grudge.Recite".Translate().CapitalizeFirst();

        if (!Votann40kUtils.IsKin(selPawn))
        {
            yield return new FloatMenuOption(label + " (" + "BEWH.Votann.Grudge.OnlyKin".Translate() + ")", null);
            yield break;
        }

        if (!selPawn.CanReach(this, PathEndMode.InteractionCell, Danger.Deadly))
        {
            yield return new FloatMenuOption(label + " (" + "BEWH.Votann.Grudge.CannotReach".Translate() + ")", null);
            yield break;
        }

        yield return FloatMenuUtility.DecoratePrioritizedTask(new FloatMenuOption(label, delegate
        {
            selPawn.jobs.TryTakeOrderedJob(JobMaker.MakeJob(Votann40kDefOf.BEWH_OpenGrudgeWindow, this), JobTag.Misc);
        }), selPawn, this);
    }

    public override string GetInspectString()
    {
        var stringBuilder = new StringBuilder(base.GetInspectString());
        var grudges = GameComponent_KinGrudges.Instance;
        if (grudges == null)
        {
            return stringBuilder.ToString();
        }

        if (grudges.GrudgedFaction == null)
        {
            stringBuilder.AppendLineIfNotEmpty().Append("BEWH.Votann.Grudge.InspectNone".Translate());
            return stringBuilder.ToString();
        }

        stringBuilder.AppendLineIfNotEmpty().Append("BEWH.Votann.Grudge.InspectCurrent".Translate(
            grudges.GrudgedFaction.Name,
            Votann40kUtils.JudgementTierLabel(grudges.JudgementTier),
            grudges.KillsSinceSworn));

        stringBuilder.AppendLineIfNotEmpty().Append(grudges.CanReforge
            ? "BEWH.Votann.Grudge.InspectReady".Translate()
            : "BEWH.Votann.Grudge.InspectLocked".Translate(grudges.ReforgeKillsLeft, grudges.ReforgeTicksLeft.ToStringTicksToPeriod()));

        return stringBuilder.ToString();
    }
}
