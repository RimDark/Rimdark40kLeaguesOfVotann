using Core40k;
using HarmonyLib;
using Verse;

namespace Votann40k;

[HarmonyPatch(typeof(ShotReport), nameof(ShotReport.HitReportFor))]
public static class GrudgeAccuracyPatch
{
    private static readonly AccessTools.StructFieldRef<ShotReport, float> FactorFromEquipment =
        AccessTools.StructFieldRefAccess<ShotReport, float>("factorFromEquipment");

    public static void Postfix(ref ShotReport __result, Verb verb, LocalTargetInfo target)
    {
        var grudged = GameComponent_KinGrudges.CachedGrudgedFaction;
        if (grudged == null || FactorFromEquipment == null)
        {
            return;
        }

        if (target.Thing is not Pawn victim || victim.Faction != grudged)
        {
            return;
        }

        var projectile = verb?.EquipmentSource?.GetComp<Comp_AmmoChanger>()?.CurrentlySelectedProjectile;
        var extension = projectile?.GetModExtension<DefModExtension_GrudgeAmmo>();
        if (extension == null)
        {
            return;
        }

        FactorFromEquipment(ref __result) *= extension.hitChanceFactor;
    }
}
