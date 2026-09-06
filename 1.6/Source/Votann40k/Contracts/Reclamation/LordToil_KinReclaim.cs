using Verse.AI;
using Verse.AI.Group;

namespace Votann40k;

public class LordToil_KinReclaim : LordToil
{
    public override bool AllowSatisfyLongNeeds => false;

    public override bool AllowSelfTend => false;

    public override bool ForceHighStoryDanger => true;

    public override void UpdateAllDuties()
    {
        for (var i = 0; i < lord.ownedPawns.Count; i++)
        {
            lord.ownedPawns[i].mindState.duty = new PawnDuty(Votann40kDefOf.BEWH_KinReclaim);
        }
    }
}
