using Verse;

namespace Votann40k;

public class KinReclamationRequest : IExposable
{
    public Map map;
    public Thing targetItem;
    public ThingDef targetDef;
    public ThingDef targetStuff;
    public int targetCount;
    public float targetValue;
    public int silverToTake;
    public int fireTick;

    public void ExposeData()
    {
        Scribe_References.Look(ref map, "map");
        Scribe_References.Look(ref targetItem, "targetItem");
        Scribe_Defs.Look(ref targetDef, "targetDef");
        Scribe_Defs.Look(ref targetStuff, "targetStuff");
        Scribe_Values.Look(ref targetCount, "targetCount");
        Scribe_Values.Look(ref targetValue, "targetValue");
        Scribe_Values.Look(ref silverToTake, "silverToTake");
        Scribe_Values.Look(ref fireTick, "fireTick");
    }
}
