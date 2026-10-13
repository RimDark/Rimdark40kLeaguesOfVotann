using System.Collections.Generic;
using RimWorld;
using Verse;

namespace Votann40k;

public class ChoiceLetter_KinGrudge : ChoiceLetter
{
    public Faction target;

    public override IEnumerable<DiaOption> Choices
    {
        get
        {
            if (ArchivedOnly)
            {
                yield return Option_Close;
                yield break;
            }

            var accept = new DiaOption("BEWH.Votann.Grudge.LetterAccept".Translate())
            {
                action = delegate
                {
                    GameComponent_KinGrudges.Instance?.SwearGrudge(target, bypassLock: true);
                    Find.LetterStack.RemoveLetter(this);
                },
                resolveTree = true
            };

            if (target == null || target.defeated)
            {
                accept.Disable(null);
            }

            yield return accept;
            yield return Option_Reject;
            yield return Option_Postpone;
        }
    }

    public override bool CanShowInLetterStack
    {
        get
        {
            if (!base.CanShowInLetterStack)
            {
                return false;
            }

            return target is { defeated: false };
        }
    }

    public override void ExposeData()
    {
        base.ExposeData();
        Scribe_References.Look(ref target, "target");
    }
}
