using HarmonyLib;
using RimWorld;
using Verse;

namespace Votann40k;

[HarmonyPatch(typeof(Pawn), nameof(Pawn.Kill))]
public static class GrudgeKillPatch
{
    public static void Prefix(Pawn __instance, out Faction __state)
    {
        __state = __instance.Faction;
    }

    public static void Postfix(Pawn __instance, DamageInfo? dinfo, Faction __state)
    {
        GameComponent_KinGrudges.Instance?.Notify_PawnKilled(__instance, __state, dinfo?.Instigator);
    }
}
