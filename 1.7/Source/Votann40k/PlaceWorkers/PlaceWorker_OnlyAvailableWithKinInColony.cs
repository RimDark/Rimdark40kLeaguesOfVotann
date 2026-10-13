using Verse;

namespace Votann40k;

public class PlaceWorker_OnlyAvailableWithKinInColony : PlaceWorker
{
    public override AcceptanceReport AllowsPlacing(BuildableDef checkingDef, IntVec3 loc, Rot4 rot, Map map, Thing thingToIgnore = null, Thing thing = null)
    {
        var colonists = map.mapPawns.FreeColonistsSpawned;
        for (var i = 0; i < colonists.Count; i++)
        {
            if (Votann40kUtils.IsKin(colonists[i]))
            {
                return true;
            }
        }

        return "BEWH.Votann.PlacementLimit.RequiresKin".Translate(checkingDef.label.CapitalizeFirst());
    }
}
