using RimWorld;
using Verse;

namespace Votann40k;

public static class Votann40kUtils
{
    public static readonly CachedTexture RefreshIcon = new ("UI/Misc/BEWH_Refresh");

    public const float SharedKnowledgePerKin = 0.02f;

    private const int BuriedKinCacheTicks = 2500;

    private static int cachedBuriedKin;
    private static int cachedBuriedKinTick = -1;
    private static Game cachedBuriedKinGame;

    public static void MarkBuriedKinDirty()
    {
        cachedBuriedKinTick = -1;
    }

    /// <summary>
    /// Total Kin uploaded into powered Ancestor Cores across all player maps. Cached and only recounted
    /// when a core changes or the cache window has passed.
    /// </summary>
    public static int TotalBuriedKin()
    {
        var game = Current.Game;
        if (game == null)
        {
            return 0;
        }

        var tick = Find.TickManager.TicksGame;
        if (cachedBuriedKinGame == game && cachedBuriedKinTick >= 0 && tick - cachedBuriedKinTick < BuriedKinCacheTicks)
        {
            return cachedBuriedKin;
        }

        var result = 0;
        var maps = Find.Maps;
        for (var i = 0; i < maps.Count; i++)
        {
            var things = maps[i].listerThings.ThingsOfDef(Votann40kDefOf.BEWH_AncestorCore);
            for (var j = 0; j < things.Count; j++)
            {
                if (things[j] is Building_AncestorCore core && core.PowerOn)
                {
                    result += core.StoredKin;
                }
            }
        }

        cachedBuriedKin = result;
        cachedBuriedKinTick = tick;
        cachedBuriedKinGame = game;
        return result;
    }

    public static bool IsKin(Pawn pawn)
    {
        return pawn?.genes != null && pawn.genes.HasActiveGene(Votann40kDefOf.BEWH_KinCloneskein);
    }

    public static bool IsPlayerKinCorpse(Corpse corpse)
    {
        var inner = corpse?.InnerPawn;
        return inner != null && inner.RaceProps.Humanlike && inner.Faction == Faction.OfPlayer && IsKin(inner);
    }

    public static Building_AncestorCore AncestorCoreOnMap(Map map)
    {
        if (map == null)
        {
            return null;
        }

        var things = map.listerThings.ThingsOfDef(Votann40kDefOf.BEWH_AncestorCore);
        for (var i = 0; i < things.Count; i++)
        {
            if (things[i] is Building_AncestorCore core)
            {
                return core;
            }
        }

        return null;
    }
}
