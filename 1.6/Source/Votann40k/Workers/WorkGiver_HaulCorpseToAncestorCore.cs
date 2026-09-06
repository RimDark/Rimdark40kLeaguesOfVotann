using RimWorld;
using Verse;
using Verse.AI;

namespace Votann40k;

public class WorkGiver_HaulCorpseToAncestorCore : WorkGiver_Scanner
{
	public override ThingRequest PotentialWorkThingRequest => ThingRequest.ForDef(Votann40kDefOf.BEWH_AncestorCore);

	public override PathEndMode PathEndMode => PathEndMode.Touch;

	public override bool HasJobOnThing(Pawn pawn, Thing t, bool forced = false)
	{
		if (t is not Building_AncestorCore ancestorCore)
		{
			return false;
		}

		if (!ancestorCore.PowerOn)
		{
			return false;
		}

		var corpse = ancestorCore.TargetCorpse;
		if (corpse == null)
		{
			return false;
		}

		if (!pawn.CanReserve(t, 1, -1, null, forced))
		{
			return false;
		}
		if (pawn.Map.designationManager.DesignationOn(t, DesignationDefOf.Deconstruct) != null)
		{
			return false;
		}
		if (t.IsBurning())
		{
			return false;
		}
		if (corpse.IsForbidden(pawn) || !pawn.CanReserve(corpse, 1, -1, null, forced))
		{
			return false;
		}
		if (!pawn.CanReach(corpse, PathEndMode.ClosestTouch, pawn.NormalMaxDanger()))
		{
			return false;
		}

		return true;
	}

	public override Job JobOnThing(Pawn pawn, Thing t, bool forced = false)
	{
		if (t is not Building_AncestorCore ancestorCore || ancestorCore.TargetCorpse == null)
		{
			return null;
		}

		return JobMaker.MakeJob(Votann40kDefOf.BEWH_CarryKinToAncestorCore, ancestorCore, ancestorCore.TargetCorpse);
	}
}
