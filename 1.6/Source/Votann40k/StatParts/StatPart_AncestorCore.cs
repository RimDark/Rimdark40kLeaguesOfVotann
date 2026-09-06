using RimWorld;
using Verse;

namespace Votann40k;

/// <summary>
/// Adds the Ancestor Core "shared knowledge" bonus to GlobalLearningFactor for pawns with the Cloneskein gene.
/// Attached to the stat via 1.6/Patches/StatPart_AncestorCore.xml.
/// </summary>
public class StatPart_AncestorCore : StatPart
{
    public override void TransformValue(StatRequest req, ref float val)
    {
        if (TryGetOffset(req, out var offset))
        {
            val += offset;
        }
    }

    public override string ExplanationPart(StatRequest req)
    {
        if (!TryGetOffset(req, out var offset))
        {
            return null;
        }

        return "BEWH.Votann.Kin.AncestorCoreSharedKnowledge".Translate() + ": " + parentStat.ValueToString(offset, ToStringNumberSense.Offset, finalized: false);
    }

    private static bool TryGetOffset(StatRequest req, out float offset)
    {
        offset = 0f;
        if (!req.HasThing || req.Thing is not Pawn pawn || !Votann40kUtils.IsKin(pawn))
        {
            return false;
        }

        var buried = Votann40kUtils.TotalBuriedKin();
        if (buried <= 0)
        {
            return false;
        }

        offset = buried * Votann40kUtils.SharedKnowledgePerKin;
        return true;
    }
}
