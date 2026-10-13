using System.Collections.Generic;
using System.Linq;
using Verse;

namespace Votann40k;

public static class KinLeagueUtility
{
    private static List<KinLeagueDef> cachedLeagues;

    private static Dictionary<GeneDef, KinLeagueDef> cachedGeneToLeague;

    public static List<KinLeagueDef> AllLeagues => cachedLeagues ??= DefDatabase<KinLeagueDef>.AllDefsListForReading
        .OrderBy(league => league.displayOrder)
        .ThenBy(league => league.label)
        .ToList();

    private static Dictionary<GeneDef, KinLeagueDef> GeneToLeague
    {
        get
        {
            if (cachedGeneToLeague != null)
            {
                return cachedGeneToLeague;
            }

            cachedGeneToLeague = new Dictionary<GeneDef, KinLeagueDef>();
            foreach (var league in AllLeagues)
            {
                foreach (var gene in league.genes)
                {
                    if (gene != null && !cachedGeneToLeague.ContainsKey(gene))
                    {
                        cachedGeneToLeague[gene] = league;
                    }
                }
            }

            return cachedGeneToLeague;
        }
    }

    /// <summary>
    /// The league a Crucible starts with: the one flagged as default, otherwise the first unlocked league.
    /// </summary>
    public static KinLeagueDef DefaultLeague
    {
        get
        {
            var leagues = AllLeagues;
            for (var i = 0; i < leagues.Count; i++)
            {
                if (leagues[i].defaultLeague && Unlocked(leagues[i]))
                {
                    return leagues[i];
                }
            }

            for (var i = 0; i < leagues.Count; i++)
            {
                if (Unlocked(leagues[i]))
                {
                    return leagues[i];
                }
            }

            return null;
        }
    }

    /// <summary>
    /// Whether the colony can currently grow kin of the given league. Every league is available for now;
    /// this is the single hook for future league progression.
    /// </summary>
    public static bool Unlocked(KinLeagueDef league)
    {
        return league != null;
    }

    /// <summary>
    /// The league whose gene this pawn carries, or null if the pawn has no league gene.
    /// </summary>
    public static KinLeagueDef LeagueOf(Pawn pawn)
    {
        if (pawn?.genes == null)
        {
            return null;
        }

        var map = GeneToLeague;
        var genes = pawn.genes.GenesListForReading;
        for (var i = 0; i < genes.Count; i++)
        {
            if (map.TryGetValue(genes[i].def, out var league))
            {
                return league;
            }
        }

        return null;
    }
}
