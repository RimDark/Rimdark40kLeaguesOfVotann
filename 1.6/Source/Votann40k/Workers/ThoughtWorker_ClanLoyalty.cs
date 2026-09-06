using RimWorld;
using Verse;

namespace Votann40k;

public class ThoughtWorker_ClanLoyalty : ThoughtWorker
{
    private static DefModExtension_ClanLoyalty cachedExtension;

    private static DefModExtension_ClanLoyalty Extension => cachedExtension ??= Votann40kDefOf.BEWH_KinClanLoyalty.GetModExtension<DefModExtension_ClanLoyalty>();

    protected override ThoughtState CurrentSocialStateInternal(Pawn pawn, Pawn other)
    {
        if (pawn == other || pawn.genes == null || other.genes == null)
        {
            return false;
        }

        if (!pawn.genes.HasActiveGene(Votann40kDefOf.BEWH_KinClanLoyalty) || !other.genes.HasActiveGene(Votann40kDefOf.BEWH_KinClanLoyalty))
        {
            return false;
        }

        var extension = Extension;
        if (extension == null || extension.doubledByRelations.NullOrEmpty())
        {
            return ThoughtState.ActiveAtStage(0);
        }

        foreach (var pawnRelation in pawn.GetRelations(other))
        {
            if (extension.doubledByRelations.Contains(pawnRelation))
            {
                return ThoughtState.ActiveAtStage(1);
            }
        }

        return ThoughtState.ActiveAtStage(0);
    }
}
