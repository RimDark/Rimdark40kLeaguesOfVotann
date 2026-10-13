using RimWorld;
using Verse;

namespace Votann40k;

/// <summary>
/// Tunnelling effect that surfaces as a Kin drill after a fixed delay. Carries which contract (or tribute) the drill is for.
/// </summary>
public class KinDrillSpawner : GroundSpawner
{
    private int contractId = -1;

    private static readonly IntRange SpawnDelay = new(KinContract.DrillTravelTicks, KinContract.DrillTravelTicks);

    public bool IsTribute => contractId < 0;

    protected override IntRange ResultSpawnDelay => SpawnDelay;

    protected override bool SpawnRubble => true;

    public static void Place(Map map, IntVec3 center, int contractId, bool isTribute)
    {
        if (!TryFindDrillCell(map, center, out var cell))
        {
            cell = DropCellFinder.TradeDropSpot(map);
        }

        var spawner = (KinDrillSpawner)ThingMaker.MakeThing(Votann40kDefOf.BEWH_KinDrillSpawner);
        spawner.contractId = isTribute ? -1 : contractId;
        GenSpawn.Spawn(spawner, cell, map);
    }

    private static bool TryFindDrillCell(Map map, IntVec3 center, out IntVec3 result)
    {
        var size = Votann40kDefOf.BEWH_KinDrill.Size;
        return CellFinder.TryFindRandomCellNear(center, map, 8, cell => CellIsFree(map, cell, size), out result, 200);
    }

    private static bool CellIsFree(Map map, IntVec3 cell, IntVec2 size)
    {
        foreach (var c in GenAdj.OccupiedRect(cell, Rot4.North, size))
        {
            if (!c.InBounds(map) || c.Fogged(map) || !c.Standable(map) || c.GetEdifice(map) != null || c.GetFirstItem(map) != null || c.GetFirstPawn(map) != null)
            {
                return false;
            }
        }

        return true;
    }

    protected override void Spawn(Map map, IntVec3 loc)
    {
        var drill = (Building_KinDrill)ThingMaker.MakeThing(Votann40kDefOf.BEWH_KinDrill);
        drill.SetContract(contractId);
        GenSpawn.Spawn(drill, loc, map, Rot4.North, WipeMode.VanishOrMoveAside);
    }

    public override void ExposeData()
    {
        base.ExposeData();
        Scribe_Values.Look(ref contractId, "contractId", -1);
    }
}
