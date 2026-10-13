using RimWorld;
using Verse;

namespace Votann40k;

/// <summary>
/// Rumbling ground that opens into a tunnel hole and releases the Kin reclamation party.
/// </summary>
public class KinTunnelSpawner : GroundSpawner
{
    private static readonly IntRange SpawnDelay = new(600, 900);

    public KinReclamationRequest request;

    protected override IntRange ResultSpawnDelay => SpawnDelay;

    protected override void Spawn(Map map, IntVec3 loc)
    {
        var hole = (Building_KinTunnelHole)ThingMaker.MakeThing(Votann40kDefOf.BEWH_KinTunnelHole);
        GenSpawn.Spawn(hole, loc, map, WipeMode.VanishOrMoveAside);
        if (request != null)
        {
            KinReclamationUtility.SpawnParty(map, hole, request);
        }
    }

    public override void ExposeData()
    {
        base.ExposeData();
        Scribe_Deep.Look(ref request, "request");
    }
}
