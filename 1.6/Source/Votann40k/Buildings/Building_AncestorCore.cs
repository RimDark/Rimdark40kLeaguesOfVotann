using System.Collections.Generic;
using System.Text;
using RimWorld;
using RimWorld.Planet;
using Verse;

namespace Votann40k;

public class Building_AncestorCore : Building
{
    private int storedKin;
    public int StoredKin => storedKin;

    private Pawn ancestorCorePawn;

    private Corpse targetCorpse;

    [Unsaved(false)]
    private CompPowerTrader cachedPowerComp;

    private CompPowerTrader PowerTraderComp => cachedPowerComp ??= this.TryGetComp<CompPowerTrader>();

    public bool PowerOn => PowerTraderComp == null || PowerTraderComp.PowerOn;

    /// <summary>
    /// The dead "ancestor" pawn that Crucible-grown Kin get as their parent. Generated once and kept in world pawns.
    /// </summary>
    public Pawn AncestorCorePawn
    {
        get
        {
            if (ancestorCorePawn != null && !ancestorCorePawn.Discarded)
            {
                return ancestorCorePawn;
            }

            var request = new PawnGenerationRequest(PawnKindDefOf.Colonist, null, PawnGenerationContext.NonPlayer,
                forceGenerateNewPawn: true, allowDead: true, canGeneratePawnRelations: false, allowAddictions: false,
                forceNoIdeo: true, forceDead: true, forcedXenotype: Votann40kDefOf.BEWH_Kin);
            ancestorCorePawn = PawnGenerator.GeneratePawn(request);
            ancestorCorePawn.Name = new NameSingle("BEWH.Votann.Kin.AncestorCore".Translate());
            Find.WorldPawns.PassToWorld(ancestorCorePawn, PawnDiscardDecideMode.KeepForever);
            return ancestorCorePawn;
        }
    }

    public Corpse TargetCorpse
    {
        get
        {
            if (targetCorpse == null)
            {
                return null;
            }

            if (targetCorpse.Destroyed || !targetCorpse.Spawned || targetCorpse.Map != Map || !CanUploadCorpse(targetCorpse))
            {
                targetCorpse = null;
            }

            return targetCorpse;
        }
    }

    public bool CanUploadCorpse(Corpse corpse)
    {
        return Votann40kUtils.IsPlayerKinCorpse(corpse);
    }

    public void UploadKinToCore(Corpse corpse)
    {
        if (corpse == null || corpse.Destroyed || !PowerOn)
        {
            return;
        }

        if (targetCorpse == corpse)
        {
            targetCorpse = null;
        }

        if (!CanUploadCorpse(corpse))
        {
            return;
        }

        storedKin++;
        Votann40kUtils.MarkBuriedKinDirty();
        corpse.Destroy();
    }

    public override void SpawnSetup(Map map, bool respawningAfterLoad)
    {
        base.SpawnSetup(map, respawningAfterLoad);
        var power = PowerTraderComp;
        if (power != null)
        {
            power.powerStartedAction += Votann40kUtils.MarkBuriedKinDirty;
            power.powerStoppedAction += Votann40kUtils.MarkBuriedKinDirty;
        }
        Votann40kUtils.MarkBuriedKinDirty();
    }

    public override void DeSpawn(DestroyMode mode = DestroyMode.Vanish)
    {
        var power = PowerTraderComp;
        if (power != null)
        {
            power.powerStartedAction -= Votann40kUtils.MarkBuriedKinDirty;
            power.powerStoppedAction -= Votann40kUtils.MarkBuriedKinDirty;
        }
        base.DeSpawn(mode);
        Votann40kUtils.MarkBuriedKinDirty();
    }

    private bool AnyDeadKinOnMap()
    {
        var corpses = Map.listerThings.ThingsInGroup(ThingRequestGroup.Corpse);
        for (var i = 0; i < corpses.Count; i++)
        {
            if (corpses[i] is Corpse corpse && CanUploadCorpse(corpse))
            {
                return true;
            }
        }

        return false;
    }

    public override IEnumerable<Gizmo> GetGizmos()
    {
        foreach (var gizmo in base.GetGizmos())
        {
            yield return gizmo;
        }

        var funeralCommand = new Command_Action
        {
            defaultLabel = "BEWH.Votann.Kin.AncestorCoreFuneral".Translate(),
            defaultDesc = "BEWH.Votann.Kin.AncestorCoreFuneralDesc".Translate(),
            action = delegate
            {
                var list = new List<FloatMenuOption>();
                var corpses = Map.listerThings.ThingsInGroup(ThingRequestGroup.Corpse);
                for (var i = 0; i < corpses.Count; i++)
                {
                    if (corpses[i] is not Corpse corpse || !CanUploadCorpse(corpse))
                    {
                        continue;
                    }

                    list.Add(new FloatMenuOption(corpse.InnerPawn.LabelShort, delegate
                    {
                        targetCorpse = corpse;
                    }));
                }

                if (list.Count > 0)
                {
                    Find.WindowStack.Add(new FloatMenu(list));
                }
            }
        };

        if (!PowerOn)
        {
            funeralCommand.Disable("NoPower".Translate().CapitalizeFirst());
        }
        else if (!AnyDeadKinOnMap())
        {
            funeralCommand.Disable("BEWH.Votann.Kin.AncestorCoreNoDeadKin".Translate());
        }

        yield return funeralCommand;

        if (TargetCorpse != null)
        {
            yield return new Command_Action
            {
                defaultLabel = "BEWH.Votann.Kin.AncestorCoreCancelFuneral".Translate(),
                defaultDesc = "BEWH.Votann.Kin.AncestorCoreCancelFuneralDesc".Translate(targetCorpse.InnerPawn.LabelShort),
                action = delegate
                {
                    targetCorpse = null;
                }
            };
        }
    }

    public override string GetInspectString()
    {
        var stringBuilder = new StringBuilder(base.GetInspectString());
        stringBuilder.AppendLineIfNotEmpty().Append("BEWH.Votann.Kin.AncestorCoreStoredKin".Translate(storedKin));
        if (!PowerOn && storedKin > 0)
        {
            stringBuilder.AppendLineIfNotEmpty().Append("BEWH.Votann.Kin.AncestorCoreUnpowered".Translate());
        }
        var pending = TargetCorpse;
        if (pending != null)
        {
            stringBuilder.AppendLineIfNotEmpty().Append("BEWH.Votann.Kin.AncestorCorePendingUpload".Translate(pending.InnerPawn.LabelShort));
        }

        return stringBuilder.ToString();
    }

    public override void ExposeData()
    {
        base.ExposeData();
        Scribe_References.Look(ref ancestorCorePawn, "ancestorCorePawn");
        Scribe_References.Look(ref targetCorpse, "targetCorpse");
        Scribe_Values.Look(ref storedKin, "storedKin");
    }
}
