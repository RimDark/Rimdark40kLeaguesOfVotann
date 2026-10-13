using System.Collections.Generic;
using RimWorld;
using UnityEngine;
using Verse;

namespace Votann40k;

public class GameComponent_KinGrudges : GameComponent
{
    private const int TickCheckInterval = 2500;
    private const int LetterCooldownTicks = GenDate.TicksPerDay;
    private const int LetterTimeoutTicks = GenDate.TicksPerDay;

    private Faction grudgedFaction;
    private int grudgeSwornTick = -1;
    private int killsSinceSworn;
    private int carriedKills;

    private Faction kinGrudgeFaction;
    private int kinGrudgeSetTick = -1;
    private int kinGrudgeKills;

    private Faction lastLetterFaction;
    private int lastLetterTick = -99999;

    private Dictionary<Faction, int> lifetimeKills = new();

    private List<Faction> lifetimeKillsKeysWork;
    private List<int> lifetimeKillsValuesWork;

    private static Faction cachedGrudgedFaction;
    private static Faction cachedKinGrudgeFaction;
    private static GameComponent_KinGrudges cachedInstance;

    public static Faction CachedGrudgedFaction => cachedGrudgedFaction;

    public static GameComponent_KinGrudges Instance => cachedInstance;

    private static Votann40kModSettings Settings => Votann40kMod.VotannSettings;

    private static int Tier2Kills => Settings?.grudgeTier2Kills ?? 10;
    private static int Tier3Kills => Settings?.grudgeTier3Kills ?? 25;
    private static float Tier1Factor => Settings?.grudgeTier1Factor ?? 1.15f;
    private static float Tier2Factor => Settings?.grudgeTier2Factor ?? 1.3f;
    private static float Tier3Factor => Settings?.grudgeTier3Factor ?? 1.5f;
    private static int ReforgeKills => Mathf.Max(1, Settings?.grudgeReforgeKills ?? 20);
    private static int ReforgeTicks => Mathf.Max(1, Settings?.grudgeReforgeDays ?? 15) * GenDate.TicksPerDay;
    private static int SwearGoodwillCost => Settings?.grudgeSwearGoodwillCost ?? 20;
    private static int BreakDebtMax => Settings?.grudgeBreakDebtMax ?? 1000;
    private static int DebtPerKill => Settings?.kinGrudgeDebtPerKill ?? 20;
    private static float KinKillerFactor => Settings?.kinGrudgeKinKillerFactor ?? 1.75f;
    private static float AlignedFactor => Settings?.kinGrudgeAlignedFactor ?? 2f;
    private static int DebtFloor => Settings?.kinGrudgeDebtFloor ?? -5000;
    private static int KinRerollTicks => Mathf.Max(1, Settings?.kinGrudgeRerollDays ?? 15) * GenDate.TicksPerDay;

    public Faction GrudgedFaction => grudgedFaction;
    public Faction KinGrudgeFaction => kinGrudgeFaction;
    public int KinGrudgeKills => kinGrudgeKills;
    public int KillsSinceSworn => killsSinceSworn;
    public int EffectiveKills => carriedKills + killsSinceSworn;

    public int JudgementTier
    {
        get
        {
            if (grudgedFaction == null)
            {
                return 0;
            }

            var kills = EffectiveKills;
            if (kills >= Tier3Kills)
            {
                return 3;
            }

            return kills >= Tier2Kills ? 2 : 1;
        }
    }

    public float JudgementDamageFactor => JudgementTier switch
    {
        3 => Tier3Factor,
        2 => Tier2Factor,
        1 => Tier1Factor,
        _ => 1f
    };

    public float TierFactor(int tier) => tier switch
    {
        3 => Tier3Factor,
        2 => Tier2Factor,
        _ => Tier1Factor
    };

    public int TierThreshold(int tier) => tier switch
    {
        3 => Tier3Kills,
        2 => Tier2Kills,
        _ => 0
    };

    public int SwearGoodwillCostForView => SwearGoodwillCost;

    public bool CanReforge => grudgedFaction == null || killsSinceSworn >= ReforgeKills || Find.TickManager.TicksGame - grudgeSwornTick >= ReforgeTicks;

    public int ReforgeKillsLeft => Mathf.Max(0, ReforgeKills - killsSinceSworn);

    public int ReforgeTicksLeft => Mathf.Max(0, grudgeSwornTick + ReforgeTicks - Find.TickManager.TicksGame);

    /// <summary>
    /// How far the current grudge has come, by kills or by time, whichever is further along. 1 when there is no grudge.
    /// </summary>
    public float GrudgeProgress
    {
        get
        {
            if (grudgedFaction == null)
            {
                return 1f;
            }

            var byKills = killsSinceSworn / (float)ReforgeKills;
            var byTime = (Find.TickManager.TicksGame - grudgeSwornTick) / (float)ReforgeTicks;
            return Mathf.Clamp01(Mathf.Max(byKills, byTime));
        }
    }

    public int BreakDebt => grudgedFaction == null ? 0 : Mathf.RoundToInt(BreakDebtMax * (1f - GrudgeProgress));

    public GameComponent_KinGrudges(Game game)
    {
        cachedInstance = this;
        cachedGrudgedFaction = null;
        cachedKinGrudgeFaction = null;
    }

    public override void FinalizeInit()
    {
        base.FinalizeInit();
        cachedInstance = this;
        lifetimeKills ??= new Dictionary<Faction, int>();
        RefreshCaches();
    }

    private void RefreshCaches()
    {
        cachedGrudgedFaction = grudgedFaction;
        cachedKinGrudgeFaction = kinGrudgeFaction;
    }

    public override void GameComponentTick()
    {
        var tick = Find.TickManager.TicksGame;
        if (tick % TickCheckInterval != 0)
        {
            return;
        }

        ValidatePlayerGrudge();
        UpdateKinGrudge(tick);
    }

    public int LifetimeKillsAgainst(Faction faction)
    {
        if (faction == null || lifetimeKills == null)
        {
            return 0;
        }

        return lifetimeKills.TryGetValue(faction, out var count) ? count : 0;
    }

    /// <summary>
    /// Records a grudge against the faction. Resets the judgement ladder to whatever half the lifetime tally carries over.
    /// </summary>
    public void SwearGrudge(Faction faction, bool bypassLock)
    {
        if (faction == null || faction.IsPlayer || faction == grudgedFaction)
        {
            return;
        }

        if (!bypassLock && !CanReforge)
        {
            return;
        }

        var abandoned = grudgedFaction;
        var breakCost = abandoned != null ? BreakDebt : 0;

        grudgedFaction = faction;
        grudgeSwornTick = Find.TickManager.TicksGame;
        killsSinceSworn = 0;
        carriedKills = Mathf.Min(LifetimeKillsAgainst(faction) / 2, Tier3Kills);
        RefreshCaches();

        faction.TryAffectGoodwillWith(Faction.OfPlayer, -SwearGoodwillCost, canSendMessage: true, canSendHostilityLetter: true);

        if (breakCost > 0)
        {
            Contracts?.AdjustDebt(breakCost);
        }

        Find.LetterStack.ReceiveLetter(
            "BEWH.Votann.Grudge.SwornTitle".Translate(),
            abandoned != null
                ? "BEWH.Votann.Grudge.SwornMessageReplaced".Translate(faction.Name, abandoned.Name, breakCost)
                : "BEWH.Votann.Grudge.SwornMessage".Translate(faction.Name),
            LetterDefOf.NeutralEvent);
    }

    private void ValidatePlayerGrudge()
    {
        if (grudgedFaction == null)
        {
            return;
        }

        if (!grudgedFaction.defeated && Find.FactionManager.AllFactionsListForReading.Contains(grudgedFaction))
        {
            return;
        }

        var name = grudgedFaction.Name;
        grudgedFaction = null;
        grudgeSwornTick = -1;
        killsSinceSworn = 0;
        carriedKills = 0;
        RefreshCaches();

        Find.LetterStack.ReceiveLetter(
            "BEWH.Votann.Grudge.StruckOutTitle".Translate(),
            "BEWH.Votann.Grudge.StruckOutMessage".Translate(name),
            LetterDefOf.PositiveEvent);
    }

    private void UpdateKinGrudge(int tick)
    {
        var kinFaction = Contracts?.KinFaction;
        if (kinFaction == null)
        {
            if (kinGrudgeFaction == null)
            {
                return;
            }

            kinGrudgeFaction = null;
            kinGrudgeKills = 0;
            RefreshCaches();
            return;
        }

        if (kinGrudgeFaction != null && KinGrudgeStillValid(kinFaction) && tick - kinGrudgeSetTick < KinRerollTicks)
        {
            return;
        }

        var next = PickKinGrudgeTarget(kinFaction);
        kinGrudgeSetTick = tick;
        if (next == kinGrudgeFaction)
        {
            return;
        }

        kinGrudgeFaction = next;
        kinGrudgeKills = 0;
        RefreshCaches();

        if (next != null)
        {
            Messages.Message("BEWH.Votann.Grudge.KinGrudgeChanged".Translate(next.Name), MessageTypeDefOf.NeutralEvent, historical: false);
        }
    }

    private bool KinGrudgeStillValid(Faction kinFaction)
    {
        if (!EligibleTarget(kinGrudgeFaction, kinFaction))
        {
            return false;
        }

        if (kinFaction.HostileTo(kinGrudgeFaction))
        {
            return true;
        }

        return !AnyHostileTarget(kinFaction);
    }

    private static bool EligibleTarget(Faction faction, Faction kinFaction)
    {
        return faction != null && !faction.IsPlayer && !faction.Hidden && !faction.defeated && !faction.temporary && faction != kinFaction
               && Find.FactionManager.AllFactionsListForReading.Contains(faction);
    }

    private static bool AnyHostileTarget(Faction kinFaction)
    {
        var factions = Find.FactionManager.AllFactionsListForReading;
        for (var i = 0; i < factions.Count; i++)
        {
            if (EligibleTarget(factions[i], kinFaction) && kinFaction.HostileTo(factions[i]))
            {
                return true;
            }
        }

        return false;
    }

    private static Faction PickKinGrudgeTarget(Faction kinFaction)
    {
        var hostiles = new List<Faction>();
        var others = new List<Faction>();
        var factions = Find.FactionManager.AllFactionsListForReading;
        for (var i = 0; i < factions.Count; i++)
        {
            var faction = factions[i];
            if (!EligibleTarget(faction, kinFaction))
            {
                continue;
            }

            if (kinFaction.HostileTo(faction))
            {
                hostiles.Add(faction);
            }
            else
            {
                others.Add(faction);
            }
        }

        if (hostiles.Count > 0)
        {
            return hostiles.RandomElement();
        }

        return others.Count > 0 ? others.RandomElement() : null;
    }

    /// <summary>
    /// Single entry point from the kill patch: tallies the ledger and runs every grudge consequence.
    /// </summary>
    public void Notify_PawnKilled(Pawn victim, Faction victimFaction, Thing instigator)
    {
        if (victim == null)
        {
            return;
        }

        var killer = instigator as Pawn;

        if (victimFaction != null && !victimFaction.IsPlayer)
        {
            AddLifetimeKill(victimFaction);

            if (victimFaction == grudgedFaction)
            {
                killsSinceSworn++;
                GiveGrudgeThoughts(victim, killer);
            }

            if (victimFaction == kinGrudgeFaction && killer?.Faction != null && killer.Faction.IsPlayer)
            {
                RewardKinGrudgeKill(killer);
            }
        }

        NotifyKinFell(victim, instigator);
    }

    private void AddLifetimeKill(Faction faction)
    {
        lifetimeKills ??= new Dictionary<Faction, int>();
        lifetimeKills[faction] = lifetimeKills.TryGetValue(faction, out var count) ? count + 1 : 1;
    }

    private static void GiveGrudgeThoughts(Pawn victim, Pawn killer)
    {
        if (killer == null || !Votann40kUtils.IsKin(killer))
        {
            return;
        }

        if (!victim.RaceProps.Humanlike && !victim.RaceProps.IsMechanoid)
        {
            return;
        }

        killer.needs?.mood?.thoughts?.memories?.TryGainMemory(Votann40kDefOf.BEWH_GrudgeStruckOut);

        var map = killer.MapHeld;
        if (map == null)
        {
            return;
        }

        var colonists = map.mapPawns.SpawnedPawnsInFaction(Faction.OfPlayer);
        for (var i = 0; i < colonists.Count; i++)
        {
            var pawn = colonists[i];
            if (pawn == killer || !Votann40kUtils.IsKin(pawn))
            {
                continue;
            }

            pawn.needs?.mood?.thoughts?.memories?.TryGainMemory(Votann40kDefOf.BEWH_GrudgeStruckOutHeard);
        }
    }

    private void RewardKinGrudgeKill(Pawn killer)
    {
        kinGrudgeKills++;

        var contracts = Contracts;
        if (contracts?.KinFaction == null || contracts.playerDebt <= DebtFloor)
        {
            return;
        }

        var amount = (float)DebtPerKill;
        if (Votann40kUtils.IsKin(killer))
        {
            amount *= KinKillerFactor;
        }

        if (kinGrudgeFaction == grudgedFaction)
        {
            amount *= AlignedFactor;
        }

        var target = Mathf.Max(contracts.playerDebt - amount, DebtFloor);
        contracts.AdjustDebt(target - contracts.playerDebt);
    }

    private void NotifyKinFell(Pawn victim, Thing instigator)
    {
        if (victim.Faction == null || !victim.Faction.IsPlayer || !Votann40kUtils.IsKin(victim))
        {
            return;
        }

        var killerFaction = instigator?.Faction;
        if (killerFaction == null || killerFaction.IsPlayer)
        {
            return;
        }

        if (killerFaction == grudgedFaction)
        {
            if (!victim.health.hediffSet.HasHediff(Votann40kDefOf.BEWH_FellInGrudge))
            {
                victim.health.AddHediff(Votann40kDefOf.BEWH_FellInGrudge);
            }

            return;
        }

        TryOfferGrudgeLetter(killerFaction, victim);
    }

    private void TryOfferGrudgeLetter(Faction killerFaction, Pawn victim)
    {
        var kinFaction = Contracts?.KinFaction;
        if (!EligibleTarget(killerFaction, kinFaction) && killerFaction != kinFaction)
        {
            return;
        }

        if (killerFaction.Hidden || killerFaction.defeated || killerFaction.temporary)
        {
            return;
        }

        var tick = Find.TickManager.TicksGame;
        if (lastLetterFaction == killerFaction && tick - lastLetterTick < LetterCooldownTicks)
        {
            return;
        }

        lastLetterFaction = killerFaction;
        lastLetterTick = tick;

        var letter = (ChoiceLetter_KinGrudge)LetterMaker.MakeLetter(
            "BEWH.Votann.Grudge.LetterTitle".Translate(),
            grudgedFaction != null
                ? "BEWH.Votann.Grudge.LetterTextReplace".Translate(victim.LabelShortCap, killerFaction.Name, grudgedFaction.Name, BreakDebt)
                : "BEWH.Votann.Grudge.LetterText".Translate(victim.LabelShortCap, killerFaction.Name),
            Votann40kDefOf.BEWH_KinGrudgeOffer,
            killerFaction);

        letter.target = killerFaction;
        letter.StartTimeout(LetterTimeoutTicks);
        Find.LetterStack.ReceiveLetter(letter);
    }

    private static GameComponent_KinContracts Contracts => Current.Game?.GetComponent<GameComponent_KinContracts>();

    public override void ExposeData()
    {
        Scribe_References.Look(ref grudgedFaction, "grudgedFaction");
        Scribe_Values.Look(ref grudgeSwornTick, "grudgeSwornTick", -1);
        Scribe_Values.Look(ref killsSinceSworn, "killsSinceSworn");
        Scribe_Values.Look(ref carriedKills, "carriedKills");
        Scribe_References.Look(ref kinGrudgeFaction, "kinGrudgeFaction");
        Scribe_Values.Look(ref kinGrudgeSetTick, "kinGrudgeSetTick", -1);
        Scribe_Values.Look(ref kinGrudgeKills, "kinGrudgeKills");
        Scribe_References.Look(ref lastLetterFaction, "lastLetterFaction");
        Scribe_Values.Look(ref lastLetterTick, "lastLetterTick", -99999);
        Scribe_Collections.Look(ref lifetimeKills, "lifetimeKills", LookMode.Reference, LookMode.Value, ref lifetimeKillsKeysWork, ref lifetimeKillsValuesWork);
    }
}
