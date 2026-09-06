using System.Collections.Generic;
using HarmonyLib;
using RimWorld;
using Verse;
using Verse.AI;

namespace Votann40k;

[HarmonyPatch(typeof(ThingWithComps), "GetFloatMenuOptions")]
public class BuryKinCorpsePatch
{
    public static IEnumerable<FloatMenuOption> Postfix(IEnumerable<FloatMenuOption> __result, ThingWithComps __instance, Pawn selPawn)
    {
        foreach (var floatMenu in __result)
        {
            yield return floatMenu;
        }

        if (__instance is not Corpse corpse || !corpse.Spawned || !Votann40kUtils.IsPlayerKinCorpse(corpse))
        {
            yield break;
        }

        var ancestorCore = Votann40kUtils.AncestorCoreOnMap(corpse.Map);
        if (ancestorCore == null)
        {
            yield break;
        }

        var label = "BEWH.Votann.Kin.UploadToAncestorCore".Translate(corpse.InnerPawn).CapitalizeFirst();

        if (!ancestorCore.PowerOn)
        {
            yield return new FloatMenuOption(label + ": " + "NoPower".Translate().CapitalizeFirst(), null);
            yield break;
        }

        if (!selPawn.CanReach(corpse, PathEndMode.ClosestTouch, Danger.Deadly))
        {
            yield return new FloatMenuOption(label + ": " + "NoPath".Translate().CapitalizeFirst(), null);
            yield break;
        }

        yield return FloatMenuUtility.DecoratePrioritizedTask(new FloatMenuOption(label, delegate
        {
            selPawn.jobs.TryTakeOrderedJob(JobMaker.MakeJob(Votann40kDefOf.BEWH_CarryKinToAncestorCore, ancestorCore, corpse), JobTag.Misc);
        }), selPawn, corpse);
    }
}
