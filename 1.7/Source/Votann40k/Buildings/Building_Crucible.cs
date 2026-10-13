using System;
using System.Collections.Generic;
using System.Text;
using RimWorld;
using UnityEngine;
using Verse;
using Verse.Sound;

namespace Votann40k;

[StaticConstructorOnStartup]
public class Building_Crucible  : Building_Enterable, IStoreSettingsParent, IThingHolderWithDrawnPawn
{
	private float containedNutrition;

	private StorageSettings allowedNutritionSettings;

	private KinLeagueDef selectedLeague;

	[Unsaved(false)]
	private CompPowerTrader cachedPowerComp;

	[Unsaved(false)]
	private CompAffectedByFacilities cachedFacilitiesComp;

	[Unsaved(false)]
	private Graphic cachedTopGraphic;

	[Unsaved(false)]
	private Sustainer sustainerWorking;

	[Unsaved(false)]
	private Hediff cachedVatLearning;

	[Unsaved(false)]
	private Effecter bubbleEffecter;

	public static readonly CachedTexture InsertPawnIcon = new CachedTexture("UI/Gizmos/InsertPawn");

	private static readonly Texture2D CancelGrowthIcon = ContentFinder<Texture2D>.Get("UI/Designators/Cancel");

	private const float BiostarvationGainPerDayNoFoodOrPower = 0.5f;

	private const float BiostarvationFallPerDayPoweredAndFed = 0.1f;

	private const float BasePawnConsumedNutritionPerDay = 3f;

	private const float AgeToEject = 18f;

	public const float NutritionBuffer = 10f;

	public const int AgeTicksPerTickInGrowthVat = 20;

	private const int GlowIntervalTicks = 132;

	private static Dictionary<Rot4, ThingDef> GlowMotePerRotation;

	private static Dictionary<Rot4, EffecterDef> BubbleEffecterPerRotation;

	public bool StorageTabVisible => true;

	public float HeldPawnDrawPos_Y => DrawPos.y + 0.03658537f;

	public float HeldPawnBodyAngle => Rotation.AsAngle;

	public PawnPosture HeldPawnPosture => PawnPosture.LayingOnGroundFaceUp;

	public bool PowerOn => PowerTraderComp.PowerOn;

	public override Vector3 PawnDrawOffset => CompBiosculpterPod.FloatingOffset(Find.TickManager.TicksGame);

	private CompPowerTrader PowerTraderComp
	{
		get
		{
			if (cachedPowerComp == null)
			{
				cachedPowerComp = this.TryGetComp<CompPowerTrader>();
			}
			return cachedPowerComp;
		}
	}

	private CompAffectedByFacilities FacilitiesComp
	{
		get
		{
			if (cachedFacilitiesComp == null)
			{
				cachedFacilitiesComp = this.TryGetComp<CompAffectedByFacilities>();
			}
			return cachedFacilitiesComp;
		}
	}

	private Building_AncestorCore AncestorCore
	{
		get
		{
			var linked = FacilitiesComp?.LinkedFacilitiesListForReading;
			if (linked == null)
			{
				return null;
			}
			for (var i = 0; i < linked.Count; i++)
			{
				if (linked[i] is Building_AncestorCore core && core.Spawned && core.PowerOn)
				{
					return core;
				}
			}
			return null;
		}
	}

	private KinLeagueDef SelectedLeague => selectedLeague ??= KinLeagueUtility.DefaultLeague;

	public float BiostarvationDailyOffset
	{
		get
		{
			if (!base.Working)
			{
				return 0f;
			}
			if (!PowerOn || containedNutrition <= 0f)
			{
				return BiostarvationGainPerDayNoFoodOrPower;
			}
			return -BiostarvationFallPerDayPoweredAndFed;
		}
	}

	private float BiostarvationSeverityPercent
	{
		get
		{
			var firstHediffOfDef = selectedPawn?.health.hediffSet.GetFirstHediffOfDef(HediffDefOf.BioStarvation);
			if (firstHediffOfDef != null)
			{
				return firstHediffOfDef.Severity / HediffDefOf.BioStarvation.maxSeverity;
			}
			return 0f;
		}
	}

	public float NutritionConsumedPerDay
	{
		get
		{
			var num = BasePawnConsumedNutritionPerDay;
			if (BiostarvationSeverityPercent > 0f)
			{
				var num2 = 1.1f;
				num *= num2;
			}
			return num;
		}
	}

	public float NutritionStored
	{
		get
		{
			var stored = containedNutrition;
			for (var i = 0; i < innerContainer.Count; i++)
			{
				var thing = innerContainer[i];
				if (thing == selectedPawn)
				{
					continue;
				}
				stored += thing.stackCount * thing.GetStatValue(StatDefOf.Nutrition);
			}
			return stored;
		}
	}

	public float NutritionNeeded
	{
		get
		{
			if (selectedPawn == null)
			{
				return 0f;
			}
			return NutritionBuffer - NutritionStored;
		}
	}

	private Graphic TopGraphic
	{
		get
		{
			if (cachedTopGraphic == null)
			{
				cachedTopGraphic = GraphicDatabase.Get<Graphic_Multi>("Things/Building/Misc/GrowthVat/GrowthVatTop", ShaderDatabase.Transparent, def.graphicData.drawSize, Color.white);
			}
			return cachedTopGraphic;
		}
	}

	private Hediff VatLearning
	{
		get
		{
			if (cachedVatLearning == null)
			{
				cachedVatLearning = selectedPawn.health.hediffSet.GetFirstHediffOfDef(HediffDefOf.VatLearning) ?? selectedPawn.health.AddHediff(HediffDefOf.VatLearning);
			}
			return cachedVatLearning;
		}
	}

	public override void PostMake()
	{
		base.PostMake();
		allowedNutritionSettings = new StorageSettings(this);
		if (def.building.defaultStorageSettings != null)
		{
			allowedNutritionSettings.CopyFrom(def.building.defaultStorageSettings);
		}
	}

	public override void DeSpawn(DestroyMode mode = DestroyMode.Vanish)
	{
		if (mode != DestroyMode.WillReplace && selectedPawn != null && innerContainer.Contains(selectedPawn))
		{
			Notify_PawnRemoved();
		}
		sustainerWorking = null;
		cachedVatLearning = null;
		base.DeSpawn(mode);
	}

	protected override void TickInterval(int delta)
	{
		base.TickInterval(delta);
		if (Working && selectedPawn != null && innerContainer.Contains(selectedPawn))
		{
			VatLearning?.TickInterval(delta);
			VatLearning?.PostTickInterval(delta);
		}
	}

	protected override void Tick()
	{
		base.Tick();
		if (this.IsHashIntervalTick(250))
		{
			PowerTraderComp.PowerOutput = Working ? 0f - PowerComp.Props.PowerConsumption : 0f - PowerComp.Props.idlePowerDraw;
		}
		if (selectedPawn != null && selectedPawn.Destroyed)
		{
			OnStop();
		}
		if (Working)
		{
			if (selectedPawn != null)
			{
				if (selectedPawn.ageTracker.AgeBiologicalYearsFloat >= AgeToEject)
				{
					Messages.Message("OccupantEjectedFromGrowthVat".Translate(selectedPawn.Named("PAWN")) + ": " + "PawnIsTooOld".Translate(selectedPawn.Named("PAWN")), selectedPawn, MessageTypeDefOf.NeutralEvent);
					Finish();
					return;
				}
				if (innerContainer.Contains(selectedPawn))
				{
					var ticks = (int)MathF.Round(AgeTicksPerTickInGrowthVat * selectedPawn.GetStatValue(StatDefOf.GrowthVatOccupantSpeed));
					selectedPawn.ageTracker.Notify_TickedInGrowthVat(ticks);
					VatLearning?.Tick();
					VatLearning?.PostTick();
				}
				var num = BiostarvationDailyOffset / 60000f * HediffDefOf.BioStarvation.maxSeverity;
				var firstHediffOfDef = selectedPawn.health.hediffSet.GetFirstHediffOfDef(HediffDefOf.BioStarvation);
				if (firstHediffOfDef != null)
				{
					firstHediffOfDef.Severity += num;
					if (firstHediffOfDef.ShouldRemove)
					{
						selectedPawn.health.RemoveHediff(firstHediffOfDef);
					}
				}
				else if (num > 0f)
				{
					var hediff = HediffMaker.MakeHediff(HediffDefOf.BioStarvation, selectedPawn);
					hediff.Severity = num;
					selectedPawn.health.AddHediff(hediff);
				}
			}
			if (BiostarvationSeverityPercent >= 1f)
			{
				Fail();
				return;
			}
			if (sustainerWorking == null || sustainerWorking.Ended)
			{
				sustainerWorking = SoundDefOf.GrowthVat_Working.TrySpawnSustainer(SoundInfo.InMap(this, MaintenanceType.PerTick));
			}
			else
			{
				sustainerWorking.Maintain();
			}
			containedNutrition = Math.Clamp(containedNutrition - NutritionConsumedPerDay / 60000f, 0f, 2.14748365E+09f);
			if (containedNutrition <= 0f)
			{
				TryAbsorbNutritiousThing();
			}
			if (GlowMotePerRotation == null)
			{
				GlowMotePerRotation = new Dictionary<Rot4, ThingDef>
				{
					{ Rot4.South, ThingDefOf.Mote_VatGlowVertical },
					{ Rot4.East, ThingDefOf.Mote_VatGlowHorizontal },
					{ Rot4.West, ThingDefOf.Mote_VatGlowHorizontal },
					{ Rot4.North, ThingDefOf.Mote_VatGlowVertical }
				};
				BubbleEffecterPerRotation = new Dictionary<Rot4, EffecterDef>
				{
					{ Rot4.South, EffecterDefOf.Vat_Bubbles_South },
					{ Rot4.East, EffecterDefOf.Vat_Bubbles_East },
					{ Rot4.West, EffecterDefOf.Vat_Bubbles_West },
					{ Rot4.North, EffecterDefOf.Vat_Bubbles_North }
				};
			}
			if (this.IsHashIntervalTick(GlowIntervalTicks))
			{
				MoteMaker.MakeStaticMote(DrawPos, MapHeld, GlowMotePerRotation[Rotation]);
			}
			if (bubbleEffecter == null)
			{
				bubbleEffecter = BubbleEffecterPerRotation[Rotation].SpawnAttached(this, MapHeld);
			}
			bubbleEffecter.EffectTick(this, this);
		}
		else
		{
			bubbleEffecter?.Cleanup();
			bubbleEffecter = null;
		}
	}

	public override AcceptanceReport CanAcceptPawn(Pawn pawn)
	{
		return false;
	}

	public override void TryAcceptPawn(Pawn pawn)
	{
		selectedPawn = pawn;
		var num = pawn.DeSpawnOrDeselect();
		if (innerContainer.TryAddOrTransfer(pawn))
		{
			SoundDefOf.GrowthVat_Close.PlayOneShot(SoundInfo.InMap(this));
			startTick = Find.TickManager.TicksGame;
			if (!pawn.health.hediffSet.HasHediff(HediffDefOf.VatLearning))
			{
				pawn.health.AddHediff(HediffDefOf.VatLearning);
			}
			if (!pawn.health.hediffSet.HasHediff(HediffDefOf.VatGrowing))
			{
				pawn.health.AddHediff(HediffDefOf.VatGrowing);
			}
		}
		if (num)
		{
			Find.Selector.Select(pawn, playSound: false, forceDesignatorDeselect: false);
		}
	}

	private void TryAbsorbNutritiousThing()
	{
		for (int i = 0; i < innerContainer.Count; i++)
		{
			if (innerContainer[i] != selectedPawn && innerContainer[i].def != ThingDefOf.Xenogerm && innerContainer[i].def != ThingDefOf.HumanEmbryo)
			{
				float statValue = innerContainer[i].GetStatValue(StatDefOf.Nutrition);
				if (statValue > 0f)
				{
					containedNutrition += statValue;
					innerContainer[i].SplitOff(1).Destroy();
					break;
				}
			}
		}
	}

	private void Finish()
	{
		if (selectedPawn != null)
		{
			FinishPawn();
		}
	}

	private void FinishPawn()
	{
		if (selectedPawn != null && innerContainer.Contains(selectedPawn))
		{
			Notify_PawnRemoved();
			innerContainer.TryDrop(selectedPawn, InteractionCell, Map, ThingPlaceMode.Near, 1, out _);
			OnStop();
		}
	}

	private void Fail()
	{
		if (innerContainer.Contains(selectedPawn))
		{
			Notify_PawnRemoved();
			innerContainer.TryDrop(selectedPawn, InteractionCell, base.Map, ThingPlaceMode.Near, 1, out var _);
			var firstHediffOfDef = selectedPawn.health.hediffSet.GetFirstHediffOfDef(HediffDefOf.BioStarvation);
			selectedPawn.Kill(null, firstHediffOfDef);
		}
		OnStop();
	}

	private void OnStop()
	{
		selectedPawn = null;
		startTick = -1;
		sustainerWorking = null;
		cachedVatLearning = null;
	}

	private void Notify_PawnRemoved()
	{
		SoundDefOf.GrowthVat_Open.PlayOneShot(SoundInfo.InMap(this));
	}

	public bool CanAcceptNutrition(Thing thing)
	{
		return allowedNutritionSettings.AllowedToAccept(thing);
	}

	public StorageSettings GetStoreSettings()
	{
		return allowedNutritionSettings;
	}

	public StorageSettings GetParentStoreSettings()
	{
		return def.building.fixedStorageSettings;
	}

	public void Notify_SettingsChanged()
	{
	}

	private void StartGrowingKin()
	{
		var core = AncestorCore;
		if (core == null || selectedPawn != null)
		{
			return;
		}
		var parent = core.AncestorCorePawn;
		var request = new PawnGenerationRequest(PawnKindDefOf.Colonist, Faction.OfPlayer, PawnGenerationContext.NonPlayer,
			forceGenerateNewPawn: true, allowDowned: true, canGeneratePawnRelations: false, allowAddictions: false,
			forcedEndogenes: SelectedLeague?.genes, forcedXenotype: Votann40kDefOf.BEWH_Kin,
			developmentalStages: DevelopmentalStage.Newborn)
		{
			DontGivePreArrivalPathway = true
		};
		var newPawn = PawnGenerator.GeneratePawn(request);
		if (parent != null)
		{
			newPawn.relations.AddDirectRelation(PawnRelationDefOf.Parent, parent);
		}
		GenSpawn.Spawn(newPawn, Position, Map);
		TryAcceptPawn(newPawn);
	}

	public override IEnumerable<Gizmo> GetGizmos()
	{
		foreach (var gizmo in base.GetGizmos())
		{
			yield return gizmo;
		}
		foreach (var item in StorageSettingsClipboard.CopyPasteGizmosFor(allowedNutritionSettings))
		{
			yield return item;
		}
		yield return LeagueGizmo();
		if (Working)
		{
			yield return new Command_Action
			{
				defaultLabel = "CommandCancelGrowth".Translate(),
				defaultDesc = "CommandCancelGrowthDesc".Translate(),
				icon = CancelGrowthIcon,
				activateSound = SoundDefOf.Designate_Cancel,
				action = delegate
				{
					Finish();
					innerContainer.TryDropAll(InteractionCell, Map, ThingPlaceMode.Near);
				}
			};
			if (DebugSettings.ShowDevGizmos)
			{
				if (selectedPawn != null && innerContainer.Contains(selectedPawn))
				{
					yield return new Command_Action
					{
						defaultLabel = "DEV: Advance 1 year",
						action = delegate
						{
							selectedPawn.ageTracker.Notify_TickedInGrowthVat(3600000);
						}
					};
					yield return new Command_Action
					{
						defaultLabel = "DEV: Learn",
						action = ((Hediff_VatLearning)VatLearning).Learn
					};
				}
			}
		}
		else
		{
			var command_Action = new Command_Action();
			command_Action.defaultLabel = "BEWH.Votann.Kin.CrucibleGrow".Translate();
			command_Action.defaultDesc = "BEWH.Votann.Kin.CrucibleGrowDesc".Translate();
			command_Action.icon = InsertPawnIcon.Texture;
			command_Action.activateSound = SoundDefOf.Designate_Cancel;
			command_Action.action = StartGrowingKin;

			if (AncestorCore == null)
			{
				command_Action.Disable("BEWH.Votann.Kin.CrucibleNoAncestorCore".Translate());
			}
			else if (!PowerOn)
			{
				command_Action.Disable("NoPower".Translate().CapitalizeFirst());
			}
			else if (selectedPawn != null)
			{
				command_Action.Disable("Occupied".Translate());
			}
			else if (SelectedLeague == null)
			{
				command_Action.Disable("BEWH.Votann.Kin.CrucibleNoLeague".Translate());
			}
			yield return command_Action;
		}
		if (DebugSettings.ShowDevGizmos)
		{
			yield return new Command_Action
			{
				defaultLabel = "DEV: Fill nutrition",
				action = delegate
				{
					containedNutrition = 10f;
				}
			};
			yield return new Command_Action
			{
				defaultLabel = "DEV: Empty nutrition",
				action = delegate
				{
					containedNutrition = 0f;
				}
			};
		}
	}

	/// <summary>
	/// Gizmo for choosing which Kin League the next kin grown here belongs to. Locked while a kin is in the crucible.
	/// </summary>
	private Gizmo LeagueGizmo()
	{
		var league = SelectedLeague;
		var command = new Command_Action
		{
			defaultLabel = "BEWH.Votann.Kin.CrucibleLeague".Translate(league != null ? league.LabelCap : "None".Translate()),
			defaultDesc = "BEWH.Votann.Kin.CrucibleLeagueDesc".Translate(),
			icon = league != null ? league.Icon : BaseContent.BadTex,
			action = delegate
			{
				var options = new List<FloatMenuOption>();
				var leagues = KinLeagueUtility.AllLeagues;
				for (var i = 0; i < leagues.Count; i++)
				{
					var option = leagues[i];
					if (!KinLeagueUtility.Unlocked(option))
					{
						continue;
					}
					options.Add(new FloatMenuOption(option.LabelCap, delegate
					{
						selectedLeague = option;
					}, option.Icon, Color.white));
				}
				if (options.Count > 0)
				{
					Find.WindowStack.Add(new FloatMenu(options));
				}
			}
		};
		if (selectedPawn != null)
		{
			command.Disable("BEWH.Votann.Kin.CrucibleLeagueLocked".Translate());
		}
		return command;
	}

	public override void DynamicDrawPhaseAt(DrawPhase phase, Vector3 drawLoc, bool flip = false)
	{
		if (Working && selectedPawn != null && innerContainer.Contains(selectedPawn))
		{
			selectedPawn.Drawer.renderer.DynamicDrawPhaseAt(phase, drawLoc + PawnDrawOffset, null, neverAimWeapon: true);
		}
		base.DynamicDrawPhaseAt(phase, drawLoc, flip);
	}

	protected override void DrawAt(Vector3 drawLoc, bool flip = false)
	{
		base.DrawAt(drawLoc, flip);
		TopGraphic.Draw(DrawPos + Altitudes.AltIncVect * 2f, base.Rotation, this);
	}

	public override string GetInspectString()
	{
		var stringBuilder = new StringBuilder();
		stringBuilder.Append(base.GetInspectString());
		if (Working)
		{
			if (selectedPawn != null && innerContainer.Contains(selectedPawn))
			{
				stringBuilder.AppendLineIfNotEmpty().Append(string.Format("{0}: {1}, {2}", "CasketContains".Translate().ToString(), selectedPawn.NameShortColored.Resolve(), selectedPawn.ageTracker.AgeBiologicalYears));
			}
			var biostarvationSeverityPercent = BiostarvationSeverityPercent;
			if (biostarvationSeverityPercent > 0f)
			{
				var text = ((BiostarvationDailyOffset >= 0f) ? "+" : string.Empty);
				stringBuilder.AppendLineIfNotEmpty().Append($"{"Biostarvation".Translate()}: {biostarvationSeverityPercent.ToStringPercent()} ({"PerDay".Translate(text + BiostarvationDailyOffset.ToStringPercent())})");
			}
		}
		else if (selectedPawn != null)
		{
			stringBuilder.AppendLineIfNotEmpty().Append("WaitingForPawn".Translate(selectedPawn.Named("PAWN")).Resolve());
		}
		if (SelectedLeague != null)
		{
			stringBuilder.AppendLineIfNotEmpty().Append("BEWH.Votann.Kin.CrucibleLeague".Translate(SelectedLeague.LabelCap));
		}
		stringBuilder.AppendLineIfNotEmpty().Append("Nutrition".Translate()).Append(": ").Append(NutritionStored.ToStringByStyle(ToStringStyle.FloatMaxOne));
		if (Working)
		{
			stringBuilder.Append(" (-").Append("PerDay".Translate(NutritionConsumedPerDay.ToString("F1"))).Append(")");
		}
		return stringBuilder.ToString();
	}

	public override void ExposeData()
	{
		base.ExposeData();
		Scribe_Values.Look(ref containedNutrition, "containedNutrition", 0f);
		Scribe_Defs.Look(ref selectedLeague, "selectedLeague");
		Scribe_Deep.Look(ref allowedNutritionSettings, "allowedNutritionSettings", this);
		if (allowedNutritionSettings == null)
		{
			allowedNutritionSettings = new StorageSettings(this);
			if (def.building.defaultStorageSettings != null)
			{
				allowedNutritionSettings.CopyFrom(def.building.defaultStorageSettings);
			}
		}
	}
}
