using System.Collections.Generic;
using HarmonyLib;
using RimWorld;
using VEF.Maps;
using Verse;
using Verse.AI;

namespace Votann40k;

[HarmonyPatch(typeof(Building_CommsConsole), "GetFloatMenuOptions")]
public class CommsConsoleKinContractsPatch
{
    public static IEnumerable<FloatMenuOption> Postfix(IEnumerable<FloatMenuOption> __result, Pawn myPawn, Building_CommsConsole __instance)
    {
        foreach (var floatMenu in __result)
        {
            yield return floatMenu;
        }

        var label = "BEWH.Votann.Contract.OpenContracts".Translate().CapitalizeFirst();

        if (!__instance.Powered())
        {
            yield return new FloatMenuOption(label + " (" + "CannotUseNoPower".Translate() + ")", null);
            yield break;
        }

        if (!Votann40kDefOf.TransportPod.IsFinished)
        {
            yield return new FloatMenuOption(label + " (" + "BEWH.Framework.Common.MissingResearch".Translate(Votann40kDefOf.TransportPod.LabelCap) + ")", null);
            yield break;
        }

        yield return FloatMenuUtility.DecoratePrioritizedTask(new FloatMenuOption(label, delegate
        {
            myPawn.jobs.TryTakeOrderedJob(JobMaker.MakeJob(Votann40kDefOf.BEWH_OpenContractWindow, __instance), JobTag.Misc);
        }), myPawn, __instance);
    }
}
