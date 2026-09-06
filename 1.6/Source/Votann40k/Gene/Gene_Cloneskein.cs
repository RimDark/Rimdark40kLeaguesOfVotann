using System.Collections.Generic;
using System.Linq;
using Verse;

namespace Votann40k;

public class Gene_Cloneskein : Gene
{
    private GeneDef chosenGene;

    private static List<GeneDef> cachedMutations;

    private static List<GeneDef> Mutations => cachedMutations ??= DefDatabase<GeneDef>.AllDefsListForReading
        .Where(geneDef => geneDef.HasModExtension<DefModExtension_CloneskeinMutation>())
        .ToList();

    private bool IsXenogene => pawn?.genes != null && pawn.genes.Xenogenes.Contains(this);

    public override void PostMake()
    {
        base.PostMake();
        ChooseMutation();
    }

    public override void PostAdd()
    {
        base.PostAdd();
        ChooseMutation();
        if (chosenGene == null || pawn.genes.HasGene(chosenGene))
        {
            return;
        }

        pawn.genes.AddGene(chosenGene, IsXenogene);
    }

    public override void PostRemove()
    {
        base.PostRemove();
        if (chosenGene == null)
        {
            return;
        }

        var gene = pawn.genes.GetGene(chosenGene);
        if (gene != null)
        {
            pawn.genes.RemoveGene(gene);
        }
    }

    private void ChooseMutation()
    {
        if (chosenGene != null || Mutations.Count == 0)
        {
            return;
        }

        chosenGene = Mutations.RandomElement();
    }

    public override void ExposeData()
    {
        base.ExposeData();
        Scribe_Defs.Look(ref chosenGene, "chosenGene");
    }
}
