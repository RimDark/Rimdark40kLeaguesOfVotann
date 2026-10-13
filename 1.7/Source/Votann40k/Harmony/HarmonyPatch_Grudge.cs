using HarmonyLib;
using RimWorld;
using Verse;

namespace Votann40k;

[HarmonyPatch(typeof(MemoryThoughtHandler), nameof(MemoryThoughtHandler.TryGainMemory), typeof(Thought_Memory), typeof(Pawn))]
public static class GrudgePatch
{
    public static void Postfix(MemoryThoughtHandler __instance, Thought_Memory newThought)
    {
        if (newThought is not Thought_MemorySocial social || social.opinionOffset >= 0f || social.durationTicksOverride >= 0)
        {
            return;
        }

        var pawn = __instance.pawn;
        if (pawn?.genes == null || !pawn.genes.HasActiveGene(Votann40kDefOf.BEWH_KinGrudgy))
        {
            return;
        }

        social.durationTicksOverride = social.def.DurationTicks * 2;
    }
}
