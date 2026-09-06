using System.Collections.Generic;
using RimWorld;
using UnityEngine;
using Verse;
using Verse.AI.Group;

namespace Votann40k;

public static class KinReclamationUtility
{
    private const float MinPoints = 300f;
    private const float PointsPerValue = 1.5f;

    /// <summary>
    /// Places the tunnel spawner for a failed contract. The party itself emerges when the spawner finishes.
    /// </summary>
    public static void Start(KinReclamationRequest request, GameComponent_KinContracts component)
    {
        var map = request.map;
        if (map == null || !Find.Maps.Contains(map) || component.KinFaction == null)
        {
            return;
        }

        var target = FindTarget(map, request);
        var center = target?.Position ?? FindSilverCenter(map) ?? map.Center;
        if (!TryFindHoleCell(map, center, out var cell))
        {
            if (!RCellFinder.TryFindRandomPawnEntryCell(out cell, map, 0f))
            {
                return;
            }
        }

        var spawner = (KinTunnelSpawner)ThingMaker.MakeThing(Votann40kDefOf.BEWH_KinTunnelSpawner);
        spawner.request = request;
        GenSpawn.Spawn(spawner, cell, map);

        Find.LetterStack.ReceiveLetter(
            "BEWH.Votann.Reclamation.IncomingTitle".Translate(),
            "BEWH.Votann.Reclamation.IncomingMessage".Translate(GenLabel.ThingLabel(request.targetDef, request.targetStuff, request.targetCount), request.silverToTake),
            LetterDefOf.ThreatBig,
            new TargetInfo(cell, map));
    }

    public static void SpawnParty(Map map, Building_KinTunnelHole hole, KinReclamationRequest request)
    {
        var component = Current.Game.GetComponent<GameComponent_KinContracts>();
        var kinFaction = component?.KinFaction;
        if (kinFaction == null)
        {
            return;
        }

        var points = Mathf.Clamp(request.targetValue * PointsPerValue, MinPoints, Mathf.Max(MinPoints, StorytellerUtility.DefaultThreatPointsNow(map)));
        var parms = new PawnGroupMakerParms
        {
            groupKind = Votann40kDefOf.BEWH_KinReclamation,
            tile = map.Tile,
            faction = kinFaction,
            points = points,
            generateFightersOnly = true
        };

        var pawns = new List<Pawn>(PawnGroupMakerUtility.GeneratePawns(parms));
        if (pawns.Count == 0)
        {
            Log.Warning("[Votann] Kin reclamation party generated no pawns.");
            return;
        }

        for (var i = 0; i < pawns.Count; i++)
        {
            var pawn = pawns[i];
            BiocodeGear(pawn);
            GenSpawn.Spawn(pawn, CellFinder.RandomClosewalkCellNear(hole.Position, map, 2), map);
        }

        LordJob lordJob;
        if (kinFaction.HostileTo(Faction.OfPlayer))
        {
            lordJob = new LordJob_AssaultColony(kinFaction, canKidnap: false, canTimeoutOrFlee: true, sappers: false, useAvoidGridSmart: true, canSteal: true);
        }
        else
        {
            lordJob = new LordJob_KinReclamation(request, hole);
        }

        LordMaker.MakeNewLord(kinFaction, lordJob, map, pawns);

        Find.LetterStack.ReceiveLetter(
            "BEWH.Votann.Reclamation.ArrivedTitle".Translate(),
            "BEWH.Votann.Reclamation.ArrivedMessage".Translate(GenLabel.ThingLabel(request.targetDef, request.targetStuff, request.targetCount), request.silverToTake),
            LetterDefOf.ThreatBig,
            pawns[0]);
    }

    private static void BiocodeGear(Pawn pawn)
    {
        if (pawn.equipment != null)
        {
            var equipment = pawn.equipment.AllEquipmentListForReading;
            for (var i = 0; i < equipment.Count; i++)
            {
                var comp = equipment[i].TryGetComp<CompBiocodable>();
                if (comp != null && !comp.Biocoded)
                {
                    comp.CodeFor(pawn);
                }
            }
        }

        if (pawn.apparel != null)
        {
            var worn = pawn.apparel.WornApparel;
            for (var i = 0; i < worn.Count; i++)
            {
                var comp = worn[i].TryGetComp<CompBiocodable>();
                if (comp != null && !comp.Biocoded)
                {
                    comp.CodeFor(pawn);
                }
            }
        }
    }

    /// <summary>
    /// The exact requested thing if it is still on the map, otherwise any spawned stack of the same def and stuff.
    /// </summary>
    public static Thing FindTarget(Map map, KinReclamationRequest request)
    {
        if (request.targetItem is { Spawned: true, Destroyed: false } && request.targetItem.Map == map)
        {
            return request.targetItem;
        }

        if (request.targetDef == null)
        {
            return null;
        }

        Thing best = null;
        var things = map.listerThings.ThingsOfDef(request.targetDef);
        for (var i = 0; i < things.Count; i++)
        {
            var thing = things[i];
            if (!thing.Spawned || thing.Stuff != request.targetStuff || thing.ParentHolder is Pawn_InventoryTracker)
            {
                continue;
            }

            if (best == null || thing.stackCount > best.stackCount)
            {
                best = thing;
            }
        }

        return best;
    }

    public static Thing FindSilver(Map map, IntVec3 from)
    {
        Thing best = null;
        var bestDist = float.MaxValue;
        var things = map.listerThings.ThingsOfDef(ThingDefOf.Silver);
        for (var i = 0; i < things.Count; i++)
        {
            var thing = things[i];
            if (!thing.Spawned || thing.stackCount <= 0)
            {
                continue;
            }

            var dist = thing.Position.DistanceToSquared(from);
            if (dist < bestDist)
            {
                bestDist = dist;
                best = thing;
            }
        }

        return best;
    }

    private static IntVec3? FindSilverCenter(Map map)
    {
        var silver = FindSilver(map, map.Center);
        return silver?.Position;
    }

    /// <summary>
    /// Most valuable spawned player item worth at most 1.5x the remaining value, used when the requested item is gone.
    /// </summary>
    public static Thing FindSubstituteItem(Map map, float valueRemaining, List<Thing> exclude)
    {
        Thing best = null;
        var bestValue = 0f;
        var things = map.listerThings.ThingsInGroup(ThingRequestGroup.HaulableEver);
        for (var i = 0; i < things.Count; i++)
        {
            var thing = things[i];
            if (!thing.Spawned || thing is Corpse || thing.def == ThingDefOf.Silver || thing.def.category != ThingCategory.Item || !thing.EverSeenByPlayer || exclude.Contains(thing))
            {
                continue;
            }

            var value = thing.MarketValue * thing.stackCount;
            if (value <= 0f || value > valueRemaining * 1.5f || value <= bestValue)
            {
                continue;
            }

            bestValue = value;
            best = thing;
        }

        return best;
    }

    private static bool TryFindHoleCell(Map map, IntVec3 center, out IntVec3 result)
    {
        return CellFinder.TryFindRandomCellNear(center, map, 6, cell => cell.InBounds(map) && !cell.Fogged(map) && cell.Standable(map) && cell.GetEdifice(map) == null && cell.GetFirstItem(map) == null && cell.GetFirstPawn(map) == null, out result, 200);
    }
}
