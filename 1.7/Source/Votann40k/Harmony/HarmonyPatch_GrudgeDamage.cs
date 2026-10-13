using HarmonyLib;
using Verse;

namespace Votann40k;

[HarmonyPatch(typeof(Thing), nameof(Thing.TakeDamage))]
public static class GrudgeDamagePatch
{
    public static void Prefix(Thing __instance, ref DamageInfo dinfo)
    {
        var grudged = GameComponent_KinGrudges.CachedGrudgedFaction;
        if (grudged == null)
        {
            return;
        }

        if (__instance is not Pawn victim || victim.Faction != grudged)
        {
            return;
        }

        if (dinfo.Instigator is not Pawn instigator || !Votann40kUtils.IsKin(instigator))
        {
            return;
        }

        var grudges = GameComponent_KinGrudges.Instance;
        if (grudges == null)
        {
            return;
        }

        dinfo.SetAmount(dinfo.Amount * grudges.JudgementDamageFactor);
    }
}
