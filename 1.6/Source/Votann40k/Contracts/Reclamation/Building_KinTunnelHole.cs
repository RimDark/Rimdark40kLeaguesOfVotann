using System.Text;
using RimWorld;
using Verse;

namespace Votann40k;

/// <summary>
/// The hole a Kin reclamation party climbs out of and disappears back into. Seals itself once the party is gone.
/// </summary>
public class Building_KinTunnelHole : Building
{
    private const int SealAfterTicks = 120000;

    protected override void Tick()
    {
        base.Tick();
        if (this.IsHashIntervalTick(2500) && Find.TickManager.TicksGame - spawnedTick > SealAfterTicks && !AnyKinPartyOnMap())
        {
            Seal();
        }
    }

    private bool AnyKinPartyOnMap()
    {
        var lords = Map.lordManager.lords;
        for (var i = 0; i < lords.Count; i++)
        {
            if (lords[i].LordJob is LordJob_KinReclamation)
            {
                return true;
            }
        }

        return false;
    }

    public void Seal()
    {
        if (!Spawned)
        {
            return;
        }

        var map = Map;
        var position = Position;
        FilthMaker.TryMakeFilth(position, map, ThingDefOf.Filth_RubbleRock);
        Destroy();
    }

    public override string GetInspectString()
    {
        var stringBuilder = new StringBuilder(base.GetInspectString());
        stringBuilder.AppendLineIfNotEmpty().Append("BEWH.Votann.Reclamation.HoleInspect".Translate());
        return stringBuilder.ToString();
    }

    public override void ExposeData()
    {
        base.ExposeData();
        Scribe_Values.Look(ref spawnedTick, "spawnedTick");
    }
}
