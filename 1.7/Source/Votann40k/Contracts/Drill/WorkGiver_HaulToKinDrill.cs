using System;
using RimWorld;
using Verse;
using Verse.AI;

namespace Votann40k;

public class WorkGiver_HaulToKinDrill : WorkGiver_Scanner
{
	public override ThingRequest PotentialWorkThingRequest => ThingRequest.ForDef(Votann40kDefOf.BEWH_KinDrill);

	public override PathEndMode PathEndMode => PathEndMode.Touch;

	public override bool HasJobOnThing(Pawn pawn, Thing t, bool forced = false)
	{
		if (t is not Building_KinDrill drill || !drill.WantsMore)
		{
			return false;
		}
		if (!pawn.CanReserve(t, 1, -1, null, forced) || t.IsBurning())
		{
			return false;
		}
		if (FindThingToLoad(pawn, drill) == null)
		{
			JobFailReason.Is("BEWH.Votann.Drill.NothingToLoad".Translate());
			return false;
		}
		return true;
	}

	public override Job JobOnThing(Pawn pawn, Thing t, bool forced = false)
	{
		if (t is not Building_KinDrill drill || !drill.WantsMore)
		{
			return null;
		}
		var thing = FindThingToLoad(pawn, drill);
		if (thing == null)
		{
			return null;
		}
		var job = HaulAIUtility.HaulToContainerJob(pawn, thing, t);
		if (job == null)
		{
			return null;
		}
		job.count = Math.Min(job.count, drill.StillNeeded);
		return job.count > 0 ? job : null;
	}

	private static Thing FindThingToLoad(Pawn pawn, Building_KinDrill drill)
	{
		return GenClosest.ClosestThingReachable(pawn.Position, pawn.Map, ThingRequest.ForGroup(ThingRequestGroup.HaulableEver), PathEndMode.ClosestTouch, TraverseParms.For(pawn), 9999f, Validator);

		bool Validator(Thing x)
		{
			return drill.Accepts(x) && !x.IsForbidden(pawn) && pawn.CanReserve(x);
		}
	}
}
