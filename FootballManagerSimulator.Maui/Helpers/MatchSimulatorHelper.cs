using FootballManagerSimulator.Maui.Enums;
using FootballManagerSimulator.Maui.Interfaces;
using FootballManagerSimulator.Maui.Models;

namespace FootballManagerSimulator.Maui.Helpers;

public class MatchSimulatorHelper(
    IClubHelper clubHelper,
    ITacticHelper tacticHelper,
    IPlayerHelper playerHelper,
    IState state,
    INotificationFactory notificationFactory) : IMatchSimulatorHelper
{
    private readonly IClubHelper ClubHelper = clubHelper;
    private readonly ITacticHelper TacticHelper = tacticHelper;
    private readonly IPlayerHelper PlayerHelper = playerHelper;
    private readonly IState State = state;
    private readonly INotificationFactory NotificationFactory = notificationFactory;

    public void ProcessMatch(FixtureModel fixture, ICompetition competition)
    {
        if (fixture.Minute == 0)
        {
            SimulateFirstHalf(fixture);
            return;
        }

        if (fixture.Minute == 45)
        {
            SimulateSecondHalf(fixture, competition);
            return;
        }

        if (fixture.Minute == 90)
        {
            SimulateExtraTime(fixture);
            return;
        }

        if (fixture.Minute == 120)
        {
            SimulatePenalties(fixture);
            return;
        }
    }

    private void SimulatePenalties(FixtureModel fixture)
    {
        var (homeRating, awayRating) = GetRatingsAtMinute(fixture, 121);
        if (HomeWinsChance(homeRating, awayRating))
            fixture.GoalsHome += 1;
        else
            fixture.GoalsAway += 1;

        var competition = State.Competitions.First(p => p.Fixtures.Contains(fixture));
        EndFixture(fixture, competition);
    }

    private void SimulateExtraTime(FixtureModel fixture)
    {
        var homeClubTacticSlots = ClubHelper.GetStartingElevenByClub(fixture.HomeClub.Id);
        var awayClubTacticSlots = ClubHelper.GetStartingElevenByClub(fixture.AwayClub.Id);

        // Goals after minute 120 would be read as shootout goals, so stop at the end of extra time.
        SimulateGoals(fixture, homeClubTacticSlots, awayClubTacticSlots, 90, 120, LaterMaxStep, BaseGoalChance, countGoals: true);
        fixture.Minute = 120;

        var etCompetition = State.Competitions.First(p => p.Fixtures.Contains(fixture));
        if (fixture.FixtureType == FixtureType.PlayoffSecondLeg)
        {
            var homeAgg = (fixture.GoalsHome ?? 0) + (fixture.FirstLegGoalsAway ?? 0);
            var awayAgg = (fixture.GoalsAway ?? 0) + (fixture.FirstLegGoalsHome ?? 0);
            if (homeAgg != awayAgg)
                EndFixture(fixture, etCompetition);
        }
        else if (fixture.GoalsHome != fixture.GoalsAway)
        {
            EndFixture(fixture, etCompetition);
        }
    }

    // Kick-off: records who started and how each side is pressing.
    private void RecordStartingElevens(FixtureModel fixture,
        IEnumerable<TacticSlotModel> homeSlots, IEnumerable<TacticSlotModel> awaySlots)
    {
        fixture.HomePlayerIds = homeSlots.Where(s => s.PlayerId != null).Select(s => s.PlayerId!.Value).ToList();
        fixture.AwayPlayerIds = awaySlots.Where(s => s.PlayerId != null).Select(s => s.PlayerId!.Value).ToList();
        PressingLog.Start(fixture.HomePressing, CurrentPressing(fixture, true));
        PressingLog.Start(fixture.AwayPressing, CurrentPressing(fixture, false));
        fixture.HomeSubstitutions.Clear();
        fixture.AwaySubstitutions.Clear();
        fixture.FitnessApplied = false;
    }

    private Pressing CurrentPressing(FixtureModel fixture, bool isHome) =>
        ClubHelper.GetClubById(isHome ? fixture.HomeClub.Id : fixture.AwayClub.Id).Instructions?.Pressing ?? Pressing.Medium;

    private void SimulateFirstHalf(FixtureModel fixture)
    {
        var homeClubTacticSlots = ClubHelper.GetStartingElevenByClub(fixture.HomeClub.Id);
        var awayClubTacticSlots = ClubHelper.GetStartingElevenByClub(fixture.AwayClub.Id);

        RecordStartingElevens(fixture, homeClubTacticSlots, awayClubTacticSlots);
        fixture.GoalsHome = 0;
        fixture.GoalsAway = 0;

        SimulateCards(fixture, homeClubTacticSlots, awayClubTacticSlots, 1, 45);
        SimulateInjuries(fixture, homeClubTacticSlots, awayClubTacticSlots, 1, 45);
        SimulateGoals(fixture, homeClubTacticSlots, awayClubTacticSlots, 0, 45, FirstHalfMaxStep, OpenPlayGoalChance, countGoals: true);
        AwardPenalties(fixture, homeClubTacticSlots, awayClubTacticSlots, 0, 45, countGoals: true);

        fixture.Minute = 45;
    }

    // Steps through the minutes after fromMinute up to toMinute, with a chance of a goal at each step.
    // Team instructions scale that chance: more open sides mean more goals, tighter ones fewer.
    private void SimulateGoals(FixtureModel fixture,
        IEnumerable<TacticSlotModel> homeSlots, IEnumerable<TacticSlotModel> awaySlots,
        int fromMinute, int toMinute, int maxStep, double goalChance, bool countGoals)
    {
        var minute = fromMinute;
        while (true)
        {
            minute += RandomNumberHelper.Next(1, maxStep + 1);
            if (minute > toMinute) return;

            var weights = GetGoalWeightsAtMinute(fixture, minute);
            var chance = Math.Min(MaxGoalChancePerStep, goalChance * (weights.Home + weights.Away));
            if (RandomNumberHelper.NextDouble() >= chance) continue;

            var scoredByHome = AddGoal(fixture, minute, homeSlots, awaySlots, weights);
            if (countGoals)
                CountGoal(fixture, scoredByHome);
        }
    }

    // ----- Penalties during play ----------------------------------------------------------

    internal const double PenaltyMatchRate = 0.225;
    internal const double PenaltyConversionRate = 0.78;

    // Rolled once a half, so PenaltyMatchRate of matches have at least one penalty.
    internal static readonly double PenaltyChancePerHalf = 1 - Math.Sqrt(1 - PenaltyMatchRate);

    // A part of a half (a lineup change regenerates the rest of it) gets its share of the half's chance.
    internal static double PenaltyChanceForMinutes(int minutes) =>
        minutes <= 0 ? 0 : 1 - Math.Pow(1 - PenaltyChancePerHalf, minutes / 45.0);

    // Goals come at steps of 1-10 minutes in the first half and 1-9 after it, with a chance of one at each step.
    internal const int FirstHalfMaxStep = 10;
    internal const int LaterMaxStep = 9;
    internal const double BaseGoalChance = 1.0 / 7;

    // Goals in 90 minutes at BaseGoalChance: 16.64 steps a match (exact, from the step sizes), divided by 7.
    internal const double BaseGoalsPerMatch = 2.3775;

    // Open play gives up the goals penalties now score, so goals per match stay where they were.
    // Extra time has no penalties, so it keeps BaseGoalChance.
    internal static readonly double OpenPlayGoalChance =
        BaseGoalChance * (1 - 2 * PenaltyChancePerHalf * PenaltyConversionRate / BaseGoalsPerMatch);

    // The average taker and keeper scores across the game's clubs (see PenaltySimulationTests.Calibration),
    // so an average penalty is scored PenaltyConversionRate of the time.
    internal const double TakerReference = 75.0;
    internal const double KeeperReference = 72.4;
    internal const double TakerWeight = 0.004;
    internal const double KeeperWeight = 0.003;
    internal const double MinScoringChance = 0.55;
    internal const double MaxScoringChance = 0.95;

    // Of the penalties not scored, the share the keeper saves; the rest miss the target.
    internal const double SavedShare = 0.75;

    internal const int AIPenaltyTakers = 3;

    internal static double TakerScore(PlayerModel taker) =>
        0.6 * taker.Penalties + 0.25 * taker.Composure + 0.15 * taker.Finishing;

    internal static double KeeperScore(PlayerModel keeper) => (keeper.GKDiving + keeper.GKReflexes) / 2.0;

    // With no keeper on the pitch the penalty is scored at the rate for an average keeper.
    internal static double ScoringChance(PlayerModel taker, PlayerModel? keeper) =>
        Math.Clamp(PenaltyConversionRate
            + TakerWeight * (TakerScore(taker) - TakerReference)
            - KeeperWeight * ((keeper == null ? KeeperReference : KeeperScore(keeper)) - KeeperReference),
            MinScoringChance, MaxScoringChance);

    internal static double SaveShare(PlayerModel keeper) =>
        Math.Clamp(SavedShare + 0.005 * (KeeperScore(keeper) - KeeperReference), 0.5, 0.95);

    // The first player on the club's list who is on the pitch; failing that, the outfield player with the best Penalties.
    internal static PlayerModel? PickPenaltyTaker(ClubModel club, IReadOnlyList<(TacticSlotModel Slot, PlayerModel Player)> onPitch)
    {
        foreach (var id in club.PenaltyTakerIds)
        {
            if (onPitch.FirstOrDefault(p => p.Player.Id == id) is { Player: { } listed })
                return listed;
        }

        return onPitch
            .OrderBy(p => p.Slot.TacticSlotType == TacticSlotType.GK)
            .ThenByDescending(p => p.Player.Penalties)
            .Select(p => p.Player)
            .FirstOrDefault();
    }

    private List<(TacticSlotModel Slot, PlayerModel Player)> OnPitchAt(FixtureModel fixture, bool isHome, int minute, IEnumerable<TacticSlotModel> slots)
    {
        var missing = GetMissingPlayerIds(fixture, isHome, minute);
        return slots
            .Where(s => s.PlayerId is { } id && !missing.Contains(id))
            .Select(s => (Slot: s, Player: PlayerHelper.GetPlayerById(s.PlayerId!.Value)))
            .Where(p => p.Player != null)
            .Select(p => (p.Slot, p.Player!))
            .ToList();
    }

    // At most one penalty between fromMinute and toMinute, awarded to either side by the same weighting as goals.
    private void AwardPenalties(FixtureModel fixture,
        IEnumerable<TacticSlotModel> homeSlots, IEnumerable<TacticSlotModel> awaySlots,
        int fromMinute, int toMinute, bool countGoals)
    {
        if (RandomNumberHelper.NextDouble() >= PenaltyChanceForMinutes(toMinute - fromMinute))
            return;

        var minute = RandomNumberHelper.Next(fromMinute + 1, toMinute + 1);
        var weights = GetGoalWeightsAtMinute(fixture, minute);
        var isHome = HomeWinsChance(weights.Home, weights.Away);

        var club = ClubHelper.GetClubById(isHome ? fixture.HomeClub.Id : fixture.AwayClub.Id);
        var taker = PickPenaltyTaker(club, OnPitchAt(fixture, isHome, minute, isHome ? homeSlots : awaySlots));
        if (taker == null)
            return;

        var keeper = OnPitchAt(fixture, !isHome, minute, isHome ? awaySlots : homeSlots)
            .FirstOrDefault(p => p.Slot.TacticSlotType == TacticSlotType.GK).Player;

        var outcome = RandomNumberHelper.NextDouble() < ScoringChance(taker, keeper) ? PenaltyOutcome.Scored
            : keeper != null && RandomNumberHelper.NextDouble() < SaveShare(keeper) ? PenaltyOutcome.Saved
            : PenaltyOutcome.Missed;

        (isHome ? fixture.HomePenalties : fixture.AwayPenalties).Add(new PenaltyModel
        {
            Minute = minute,
            TakerId = taker.Id,
            KeeperId = keeper?.Id,
            Outcome = outcome
        });

        if (outcome != PenaltyOutcome.Scored)
            return;

        (isHome ? fixture.HomeScorers : fixture.AwayScorers).Add(new GoalModel { Minute = minute, PlayerId = taker.Id, IsPenalty = true });
        if (countGoals)
            CountGoal(fixture, isHome);
    }

    private int GetGoalScorer(IEnumerable<TacticSlotModel> tacticSlots)
    {
        var playerRatingModels = new List<PlayerRatingModel>();
        var cumulative = 0d;
        foreach (var slot in tacticSlots)
        {
            if (slot.PlayerId is not { } playerId || PlayerHelper.GetPlayerById(playerId) is not { } player)
                continue;

            cumulative += player.ScoringProbability * PositionScoringWeight(slot.TacticSlotType);
            playerRatingModels.Add(new PlayerRatingModel
            {
                PlayerId = playerId,
                Rating = cumulative
            });
        }

        if (cumulative <= 0)
            return playerRatingModels[RandomNumberHelper.Next(0, playerRatingModels.Count)].PlayerId;

        var randomNumber = RandomNumberHelper.NextDouble() * cumulative;

        return playerRatingModels
            .First(p => randomNumber < p.Rating)
            .PlayerId;
    }

    // Scales a player's scoring stats by where they are playing, so a lone striker
    // scores roughly a third of the team's goals rather than competing evenly with midfield.
    internal static double PositionScoringWeight(TacticSlotType slotType) => slotType switch
    {
        TacticSlotType.ST or TacticSlotType.LS or TacticSlotType.LF => 1.6,
        TacticSlotType.CAM or TacticSlotType.LW or TacticSlotType.RW => 1.0,
        TacticSlotType.LM or TacticSlotType.RM => 0.8,
        TacticSlotType.CM => 0.7,
        TacticSlotType.CDM or TacticSlotType.LB or TacticSlotType.RB => 0.6,
        TacticSlotType.CB => 0.8, // set-piece headers
        TacticSlotType.GK => 0.1,
        _ => 1.0
    };

    private void SimulateSecondHalf(FixtureModel fixture, ICompetition competition)
    {
        var homeClubTacticSlots = ClubHelper.GetStartingElevenByClub(fixture.HomeClub.Id);
        var awayClubTacticSlots = ClubHelper.GetStartingElevenByClub(fixture.AwayClub.Id);

        SimulateCards(fixture, homeClubTacticSlots, awayClubTacticSlots, 46, 90);
        SimulateInjuries(fixture, homeClubTacticSlots, awayClubTacticSlots, 46, 90);
        SimulateGoals(fixture, homeClubTacticSlots, awayClubTacticSlots, 45, 90, LaterMaxStep, OpenPlayGoalChance, countGoals: true);
        AwardPenalties(fixture, homeClubTacticSlots, awayClubTacticSlots, 45, 90, countGoals: true);

        fixture.Minute = 90;

        if (fixture.FixtureType == FixtureType.PlayoffSecondLeg)
        {
            PopulateFirstLegScores(fixture, competition);
            var homeAgg = (fixture.GoalsHome ?? 0) + (fixture.FirstLegGoalsAway ?? 0);
            var awayAgg = (fixture.GoalsAway ?? 0) + (fixture.FirstLegGoalsHome ?? 0);
            if (homeAgg != awayAgg || LevelTieGoesToHigherPlacedClub(fixture, competition))
                EndFixture(fixture, competition);
        }
        else if (fixture.FixtureType == FixtureType.PlayoffFinal ||
                 (competition is LeagueModel league && league.IsOneOffPlayoffMatch(fixture)))
        {
            if (fixture.GoalsHome != fixture.GoalsAway)
                EndFixture(fixture, competition);
        }
        else if (!competition.Type.IsKnockout() || fixture.GoalsHome != fixture.GoalsAway)
        {
            EndFixture(fixture, competition);
        }
    }

    public void ConcludeFixture(FixtureModel fixture, ICompetition competition)
    {
        while (!fixture.Concluded)
        {
            ProcessMatch(fixture, competition);
        }
    }

    private void SimulateCards(
        FixtureModel fixture,
        IEnumerable<TacticSlotModel> homeSlots,
        IEnumerable<TacticSlotModel> awaySlots,
        int minuteFrom,
        int minuteTo)
    {
        var homePlayerIds = homeSlots.Where(s => s.PlayerId != null).Select(s => s.PlayerId!.Value).ToList();
        var awayPlayerIds = awaySlots.Where(s => s.PlayerId != null).Select(s => s.PlayerId!.Value).ToList();

        // Goalkeepers are never sent off: they can be booked once, but get no straight red or second yellow.
        var goalkeeperIds = homeSlots.Concat(awaySlots)
            .Where(s => s.TacticSlotType == TacticSlotType.GK && s.PlayerId != null)
            .Select(s => s.PlayerId!.Value)
            .ToHashSet();

        // Pressing changes how often each side is booked (see TacticalModifierRules).
        var homeRate = GetSideModifiers(fixture, true, minuteFrom).CardRate;
        var awayRate = GetSideModifiers(fixture, false, minuteFrom).CardRate;

        for (var minute = minuteFrom; minute <= minuteTo; minute += RandomNumberHelper.Next(5, 15))
        {
            if (!SideEventRoll(CardChancePerStep, homeRate, awayRate, out var isHome))
                continue;

            var cards = isHome ? fixture.HomeCards : fixture.AwayCards;
            var sentOff = GetSentOffPlayerIds(cards, minute + 1);
            var bookedKeepers = cards.Where(c => goalkeeperIds.Contains(c.PlayerId)).Select(c => c.PlayerId).ToHashSet();
            var playerIds = (isHome ? homePlayerIds : awayPlayerIds)
                .Where(id => !sentOff.Contains(id) && !bookedKeepers.Contains(id))
                .ToList();

            if (!playerIds.Any()) continue;

            var playerId = playerIds[RandomNumberHelper.Next(0, playerIds.Count)];

            var isStraightRed = !goalkeeperIds.Contains(playerId) && RandomNumberHelper.Next(1, 12) == 1;

            if (isStraightRed)
            {
                cards.Add(new CardModel { Minute = minute, PlayerId = playerId, IsRed = true });
            }
            else
            {
                cards.Add(new CardModel { Minute = minute, PlayerId = playerId, IsRed = false });

                var yellowCount = cards.Count(c => !c.IsRed && c.PlayerId == playerId);
                if (yellowCount >= 2)
                    cards.Add(new CardModel { Minute = minute, PlayerId = playerId, IsRed = true });
            }
        }
    }

    private static readonly (string Description, int MinDays, int MaxDays, int Weight)[] InjuryTypes =
    [
        ("Minor knock", 3, 7, 40),
        ("Muscle strain", 7, 21, 30),
        ("Hamstring injury", 14, 42, 15),
        ("Ligament damage", 28, 90, 10),
        ("Fracture", 42, 120, 5)
    ];

    private static readonly int TotalInjuryWeight = InjuryTypes.Sum(i => i.Weight);

    private void SimulateInjuries(
        FixtureModel fixture,
        IEnumerable<TacticSlotModel> homeSlots,
        IEnumerable<TacticSlotModel> awaySlots,
        int minuteFrom,
        int minuteTo)
    {
        var homePlayerIds = homeSlots.Where(s => s.PlayerId != null).Select(s => s.PlayerId!.Value).ToList();
        var awayPlayerIds = awaySlots.Where(s => s.PlayerId != null).Select(s => s.PlayerId!.Value).ToList();

        // Pressing changes how often each side picks up injuries (see TacticalModifierRules).
        var homeRate = GetSideModifiers(fixture, true, minuteFrom).InjuryRate;
        var awayRate = GetSideModifiers(fixture, false, minuteFrom).InjuryRate;

        for (var minute = minuteFrom; minute <= minuteTo; minute += RandomNumberHelper.Next(8, 20))
        {
            // Tired players get injured more: each side's rate is scaled by its players' average
            // injury weight, and the injured player is picked in proportion to it.
            var homeWeights = InjuryWeights(fixture, homePlayerIds, fixture.HomeInjuries, minute);
            var awayWeights = InjuryWeights(fixture, awayPlayerIds, fixture.AwayInjuries, minute);
            var homeTiredness = homeWeights.Count == 0 ? 1 : homeWeights.Average(w => w.Weight);
            var awayTiredness = awayWeights.Count == 0 ? 1 : awayWeights.Average(w => w.Weight);

            if (!SideEventRoll(InjuryChancePerStep, homeRate * homeTiredness, awayRate * awayTiredness, out var isHome))
                continue;

            var injuries = isHome ? fixture.HomeInjuries : fixture.AwayInjuries;
            var candidates = isHome ? homeWeights : awayWeights;

            if (candidates.Count == 0) continue;

            var playerId = PickWeighted(candidates);
            var (description, daysOut) = PickInjury();

            injuries.Add(new InjuryModel
            {
                Minute = minute,
                PlayerId = playerId,
                Description = description,
                DaysOut = daysOut
            });
        }
    }

    // Players who could be injured at this minute (not already injured), each with its injury weight.
    private List<(int PlayerId, double Weight)> InjuryWeights(FixtureModel fixture, List<int> playerIds,
        List<InjuryModel> injuries, int minute)
    {
        var alreadyInjured = injuries.Select(i => i.PlayerId).ToHashSet();
        return playerIds
            .Where(id => !alreadyInjured.Contains(id))
            .Select(id => (id, FitnessRules.InjuryWeight(FitnessAt(fixture, id, minute))))
            .ToList();
    }

    private static int PickWeighted(List<(int PlayerId, double Weight)> candidates)
    {
        var roll = RandomNumberHelper.NextDouble() * candidates.Sum(c => c.Weight);
        foreach (var (playerId, weight) in candidates)
        {
            roll -= weight;
            if (roll < 0) return playerId;
        }
        return candidates[^1].PlayerId;
    }

    private static (string Description, int DaysOut) PickInjury()
    {
        var roll = RandomNumberHelper.Next(TotalInjuryWeight);
        var cumulative = 0;
        foreach (var (description, minDays, maxDays, weight) in InjuryTypes)
        {
            cumulative += weight;
            if (roll < cumulative)
                return (description, RandomNumberHelper.Next(minDays, maxDays + 1));
        }
        return (InjuryTypes[0].Description, RandomNumberHelper.Next(InjuryTypes[0].MinDays, InjuryTypes[0].MaxDays + 1));
    }

    private void ApplyInjuries(FixtureModel fixture)
    {
        var physioCount = State.Physios.Count;

        foreach (var injury in fixture.HomeInjuries.Concat(fixture.AwayInjuries))
        {
            var player = PlayerHelper.GetPlayerById(injury.PlayerId);
            if (player == null) continue;

            var isMyPlayer = player.Contract != null && player.Contract.ClubId == State.MyClubId;
            var daysOut = isMyPlayer ? PhysioRules.ReducedDays(injury.DaysOut, physioCount) : injury.DaysOut;

            player.InjuryEndDate = State.Date.AddDays(daysOut);
            player.InjuryDescription = injury.Description;

            if (isMyPlayer)
            {
                var reduction = physioCount > 0
                    ? $" (reduced from {injury.DaysOut} days by your medical team)"
                    : "";
                State.Notifications.Add(new NotificationModel
                {
                    Date = State.Date,
                    Recipient = "Medical Team",
                    Subject = $"{player.Name} injured",
                    Message = $"{player.Name} has suffered a {injury.Description.ToLower()} and is expected to be out for {daysOut} days, until {player.InjuryEndDate:d MMMM yyyy}.{reduction}"
                });
            }
        }
    }

    private bool IsCompetitivFixture(ICompetition competition)
    {
        return competition.Type != CompetitionType.Friendly;
    }

    private void ApplySuspensions(FixtureModel fixture, ICompetition competition)
    {
        if (!IsCompetitivFixture(competition))
            return;

        var allCards = fixture.HomeCards.Concat(fixture.AwayCards);
        var redCardPlayerIds = allCards.Where(c => c.IsRed).Select(c => c.PlayerId).Distinct();

        foreach (var playerId in redCardPlayerIds)
        {
            var player = PlayerHelper.GetPlayerById(playerId);
            if (player == null) continue;

            var allPlayerCards = allCards.Where(c => c.PlayerId == playerId).ToList();
            var hasStraightRed = allPlayerCards.Any(c => c.IsRed && allPlayerCards.Count(y => !y.IsRed && y.PlayerId == playerId) < 2);

            player.SuspensionMatches = hasStraightRed ? 3 : 1;
        }
    }

    private void ReduceSuspensions(FixtureModel fixture, ICompetition competition)
    {
        if (!IsCompetitivFixture(competition))
            return;

        var clubIds = new[] { fixture.HomeClub.Id, fixture.AwayClub.Id };
        var suspendedPlayers = State.Players
            .Where(p => p.SuspensionMatches > 0 && p.Contract != null && clubIds.Contains(p.Contract.ClubId));

        foreach (var player in suspendedPlayers)
        {
            player.SuspensionMatches--;
            if (player.SuspensionMatches == 0 && player.Contract?.ClubId == State.MyClubId)
                NotificationFactory.AddNotification(State.Date, "Club Secretary",
                    "Suspension Served",
                    $"{player.Name} has served their suspension and is available for selection.");
        }
    }

    private void EndFixture(FixtureModel fixture, ICompetition competition)
    {
        var wasConcluded = fixture.Concluded;
        fixture.Concluded = true;

        var homeGoals = fixture.GoalsHome ?? 0;
        var awayGoals = fixture.GoalsAway ?? 0;

        // A second leg goes to whoever leads on aggregate, not whoever won on the night.
        if (fixture.FixtureType == FixtureType.PlayoffSecondLeg)
        {
            PopulateFirstLegScores(fixture, competition);
            homeGoals += fixture.FirstLegGoalsAway ?? 0;
            awayGoals += fixture.FirstLegGoalsHome ?? 0;
        }

        if (homeGoals > awayGoals)
            fixture.ClubWon = fixture.HomeClub;
        else if (awayGoals > homeGoals)
            fixture.ClubWon = fixture.AwayClub;
        else if (LevelTieGoesToHigherPlacedClub(fixture, competition))
            fixture.ClubWon = ((LeagueModel)competition).HigherPlacedClub(fixture.HomeClub, fixture.AwayClub);
        else
            fixture.ClubWon = null;

        ReduceSuspensions(fixture, competition);
        ApplySuspensions(fixture, competition);
        ApplyInjuries(fixture);
        ApplyFitness(fixture);

        if (!wasConcluded)
        {
            UpdateBoardConfidence(fixture, competition);
            RecordManagerResult(fixture, competition);
        }
    }

    private void RecordManagerResult(FixtureModel fixture, ICompetition competition)
    {
        var myClubId = State.MyClubId;
        if (competition.Type == CompetitionType.Friendly || myClubId == null
            || (fixture.HomeClub.Id != myClubId && fixture.AwayClub.Id != myClubId))
            return;

        // A shootout adds one goal to the winner's score (logged after minute 120); it isn't a real goal.
        var isHome = fixture.HomeClub.Id == myClubId;
        var homeGoals = (fixture.GoalsHome ?? 0) - fixture.HomeScorers.Count(g => g.Minute > 120);
        var awayGoals = (fixture.GoalsAway ?? 0) - fixture.AwayScorers.Count(g => g.Minute > 120);
        bool? won = fixture.ClubWon == null ? null : fixture.ClubWon.Id == myClubId;

        State.ManagerHistory.RecordResult(isHome ? homeGoals : awayGoals, isHome ? awayGoals : homeGoals, won);

        if (won != true)
            return;

        if (fixture.FixtureType == FixtureType.PlayoffFinal || (competition as LeagueModel)?.IsTwoLeggedPlayoffFinal(fixture) == true)
            State.ManagerHistory.AddTrophy($"{competition.Name} Play-off Final", fixture.Date);
        else if (IsCupFinal(fixture, competition))
            State.ManagerHistory.AddTrophy(competition.Name, fixture.Date);
    }

    private static bool IsCupFinal(FixtureModel fixture, ICompetition competition) =>
        competition.Type.IsKnockout()
        &&(competition.DrawSettings.Count == 0 || fixture.Round == competition.DrawSettings.Max(p => p.Round));

    private void UpdateBoardConfidence(FixtureModel fixture, ICompetition competition)
    {
        if (competition.Type == CompetitionType.Friendly)
            return;

        // Fixture clubs can be copies after a save/load, so update the canonical State.Clubs entries.
        foreach (var clubId in new[] { fixture.HomeClub.Id, fixture.AwayClub.Id })
        {
            var club = State.Clubs.FirstOrDefault(p => p.Id == clubId);
            if (club == null)
                continue;

            club.JobSecurity = BoardConfidenceRules.Apply(club.JobSecurity, ResultChange(fixture, clubId));

            if (club.Id != State.MyClubId && club.VacantUntil == null && club.JobSecurity < BoardConfidenceRules.SackedBelow)
                SackManager(club);
        }

        var myClubId = State.MyClubId;
        if (myClubId == null || (fixture.HomeClub.Id != myClubId && fixture.AwayClub.Id != myClubId))
            return;

        var before = State.BoardConfidence;
        State.BoardConfidence = BoardConfidenceRules.Apply(before, ResultChange(fixture, myClubId.Value));

        if (before >= BoardConfidenceRules.UnderPressureFrom && State.BoardConfidence < BoardConfidenceRules.UnderPressureFrom)
        {
            State.Notifications.Add(new NotificationModel
            {
                Date = State.Date,
                Recipient = "Chairman",
                Subject = "Your job is at risk",
                Message = $"Results have not been good enough. The board's confidence in you has fallen to {BoardConfidenceRules.Format(State.BoardConfidence)} and your position is now at risk. Things need to improve quickly.",
                RequiresAction = true,
                IsBoardWarning = true
            });
        }
    }

    private void SackManager(ClubModel club)
    {
        club.VacantUntil = State.Date.AddDays(
            RandomNumberHelper.Next(BoardConfidenceRules.VacancyMinDays, BoardConfidenceRules.VacancyMaxDays + 1));

        if (State.ShowManagerNotifications)
            State.Notifications.Add(new NotificationModel
            {
                Date = State.Date,
                Recipient = "Press Officer",
                Subject = $"{club.Name} sack their manager",
                Message = $"After a terrible run of results, {club.Name} have sacked their manager. They expect to appoint a replacement by {club.VacantUntil:d MMMM yyyy}. The job is listed on the Jobs screen until then."
            });
    }

    private static double ResultChange(FixtureModel fixture, int clubId) =>
        fixture.ClubWon == null ? BoardConfidenceRules.Draw
        : fixture.ClubWon.Id == clubId ? BoardConfidenceRules.Win
        : BoardConfidenceRules.Loss;

    public void PreGenerateMatch(FixtureModel fixture)
    {
        var homeClubTacticSlots = ClubHelper.GetStartingElevenByClub(fixture.HomeClub.Id);
        var awayClubTacticSlots = ClubHelper.GetStartingElevenByClub(fixture.AwayClub.Id);

        RecordStartingElevens(fixture, homeClubTacticSlots, awayClubTacticSlots);
        fixture.GoalsHome = 0;
        fixture.GoalsAway = 0;

        SimulateCards(fixture, homeClubTacticSlots, awayClubTacticSlots, 1, 45);
        SimulateInjuries(fixture, homeClubTacticSlots, awayClubTacticSlots, 1, 45);
        SimulateGoals(fixture, homeClubTacticSlots, awayClubTacticSlots, 0, 45, FirstHalfMaxStep, OpenPlayGoalChance, countGoals: false);
        AwardPenalties(fixture, homeClubTacticSlots, awayClubTacticSlots, 0, 45, countGoals: false);

        SimulateCards(fixture, homeClubTacticSlots, awayClubTacticSlots, 46, 90);
        SimulateInjuries(fixture, homeClubTacticSlots, awayClubTacticSlots, 46, 90);
        SimulateGoals(fixture, homeClubTacticSlots, awayClubTacticSlots, 45, 90, LaterMaxStep, OpenPlayGoalChance, countGoals: false);
        AwardPenalties(fixture, homeClubTacticSlots, awayClubTacticSlots, 45, 90, countGoals: false);
    }

    public void GenerateExtraTimeEvents(FixtureModel fixture) => GenerateExtraTimeEventsFrom(fixture, 90);

    private void GenerateExtraTimeEventsFrom(FixtureModel fixture, int fromMinute)
    {
        SimulateGoals(fixture,
            ClubHelper.GetStartingElevenByClub(fixture.HomeClub.Id),
            ClubHelper.GetStartingElevenByClub(fixture.AwayClub.Id),
            fromMinute, 120, LaterMaxStep, BaseGoalChance, countGoals: false);
    }

    // The shoot-out is decided on strength alone: team instructions play no part in it.
    public void GeneratePenaltyEvents(FixtureModel fixture)
    {
        var (homeRating, awayRating) = GetRatingsAtMinute(fixture, 121);
        AddGoal(fixture, 121,
            ClubHelper.GetStartingElevenByClub(fixture.HomeClub.Id),
            ClubHelper.GetStartingElevenByClub(fixture.AwayClub.Id),
            new GoalWeights(homeRating, awayRating));
    }

    public void FinaliseMatch(FixtureModel fixture, ICompetition competition)
    {
        EndFixture(fixture, competition);
    }

    private static bool LevelTieGoesToHigherPlacedClub(FixtureModel fixture, ICompetition competition) =>
        competition is LeagueModel league && league.LevelTieGoesToHigherPlacedClub(fixture);

    public void PopulateFirstLegScores(FixtureModel secondLeg, ICompetition competition)
    {
        if (secondLeg.FirstLegGoalsHome != null)
            return;

        var firstLeg = competition.Fixtures.FirstOrDefault(p =>
            p.FixtureType == FixtureType.Playoff && p.Concluded &&
            p.HomeClub.Id == secondLeg.AwayClub.Id && p.AwayClub.Id == secondLeg.HomeClub.Id);

        if (firstLeg == null)
            return;

        secondLeg.FirstLegGoalsHome = firstLeg.GoalsHome;
        secondLeg.FirstLegGoalsAway = firstLeg.GoalsAway;
    }

    // Raising ratings to this power widens the gap between strong and weak sides, so upsets
    // stay possible but rare. Applied before the multipliers below so those keep their stated strength.
    internal const double RatingGapExponent = 3;

    // Applied after cubing, so it is cubed too: a 6% rating edge.
    internal static readonly double MyClubRatingBoost = Math.Pow(1.06, RatingGapExponent);

    private (double homeRating, double awayRating) GetAdjustedRatings(FixtureModel fixture, int minute = 0)
    {
        var homeRating = Math.Pow(StrengthAt(fixture, true, minute), RatingGapExponent);
        var awayRating = Math.Pow(StrengthAt(fixture, false, minute), RatingGapExponent);

        if (fixture.HomeClub.Id == State.MyClubId)
            homeRating *= MyClubRatingBoost;
        else if (fixture.AwayClub.Id == State.MyClubId)
            awayRating *= MyClubRatingBoost;

        awayRating *= .8;

        return (homeRating, awayRating);
    }

    // ----- Fitness ---------------------------------------------------------------------------

    // The XI's strength at this minute: each player's rating after any out-of-position cut,
    // scaled by how fit he is at that point of the match.
    private double StrengthAt(FixtureModel fixture, bool isHome, int minute)
    {
        var clubId = isHome ? fixture.HomeClub.Id : fixture.AwayClub.Id;
        var sum = 0.0;
        foreach (var slot in ClubHelper.GetStartingElevenByClub(clubId))
        {
            if (slot.PlayerId is not { } id || PlayerHelper.GetPlayerById(id) is not { } player) continue;
            sum += PositionFamiliarityRules.EffectiveRating(player, slot.TacticSlotType)
                   * FitnessRules.StrengthFactor(FitnessAt(fixture, id, minute));
        }
        return sum;
    }

    // A player's fitness at this minute of the match: what he started with, less the drain for the
    // minutes he has been on the pitch (more for minutes his side spent pressing high). Before he
    // has played, or once full time has charged it, it is simply his current fitness.
    public double FitnessAt(FixtureModel fixture, int playerId, int minute)
    {
        if (PlayerHelper.GetPlayerById(playerId) is not { } player)
            return FitnessRules.Max;
        if (fixture.FitnessApplied)
            return player.Fitness;

        bool isHome;
        if (fixture.HomePlayerIds.Contains(playerId)) isHome = true;
        else if (fixture.AwayPlayerIds.Contains(playerId)) isHome = false;
        else return player.Fitness;

        var (on, off) = OnPitchSpan(fixture, isHome, playerId);
        var end = Math.Min(minute, off ?? minute);
        if (end <= on)
            return player.Fitness;

        var log = isHome ? fixture.HomePressing : fixture.AwayPressing;
        var current = CurrentPressing(fixture, isHome);
        var pressed = PressingLog.MinutesPressedHigh(log, end, current) - PressingLog.MinutesPressedHigh(log, on, current);

        var drain = FitnessRules.Drain(player.Stamina, end - on, pressed, FitnessRules.IsGoalkeeper(player));
        return Math.Clamp(player.Fitness - drain, 0, FitnessRules.Max);
    }

    // When a player came on (0 for a starter) and, if he has, when he went off (substituted or sent off).
    private static (int On, int? Off) OnPitchSpan(FixtureModel fixture, bool isHome, int playerId)
    {
        var subs = isHome ? fixture.HomeSubstitutions : fixture.AwaySubstitutions;
        var on = subs.Where(s => s.OnId == playerId).Select(s => s.Minute).DefaultIfEmpty(0).Max();
        int? off = subs.Where(s => s.OffId == playerId && s.Minute >= on).Select(s => (int?)s.Minute).Min();

        var cards = isHome ? fixture.HomeCards : fixture.AwayCards;
        if (cards.Where(c => c.IsRed && c.PlayerId == playerId && c.Minute >= on).Select(c => (int?)c.Minute).Min() is { } sentOff)
            off = off is { } subbed ? Math.Min(subbed, sentOff) : sentOff;

        return (on, off);
    }

    // Full time: every player who took part is charged for the minutes he played.
    private void ApplyFitness(FixtureModel fixture)
    {
        if (fixture.FitnessApplied)
            return;

        var end = Math.Clamp(fixture.Minute, 90, 120);
        var finalFitness = fixture.HomePlayerIds.Concat(fixture.AwayPlayerIds)
            .Distinct()
            .Select(id => (Id: id, Fitness: FitnessAt(fixture, id, end)))
            .ToList();

        foreach (var (id, fitness) in finalFitness)
        {
            if (PlayerHelper.GetPlayerById(id) is { } player)
                player.Fitness = fitness;
        }

        fixture.FitnessApplied = true;
    }

    // Each player missing before the given minute costs the team 20% of its rating.
    private (double homeRating, double awayRating) GetRatingsAtMinute(FixtureModel fixture, int minute)
    {
        var (homeRating, awayRating) = GetAdjustedRatings(fixture, minute);
        var homeMissing = GetMissingPlayerIds(fixture, true, minute).Count;
        var awayMissing = GetMissingPlayerIds(fixture, false, minute).Count;

        return (homeRating * Math.Pow(0.8, homeMissing), awayRating * Math.Pow(0.8, awayMissing));
    }

    // Starters who can't play on at the given minute: those sent off, and the user's injured players
    // still in the XI, who leave the side a man short until they're substituted. AI clubs make no
    // substitutions, so their injured players play on as before.
    private HashSet<int> GetMissingPlayerIds(FixtureModel fixture, bool isHome, int minute)
    {
        var missing = GetSentOffPlayerIds(isHome ? fixture.HomeCards : fixture.AwayCards, minute);
        var club = isHome ? fixture.HomeClub : fixture.AwayClub;
        if (club.Id != State.MyClubId)
            return missing;

        var starters = ClubHelper.GetStartingElevenByClub(club.Id).Select(s => s.PlayerId).ToHashSet();
        missing.UnionWith((isHome ? fixture.HomeInjuries : fixture.AwayInjuries)
            .Where(i => i.Minute < minute && starters.Contains(i.PlayerId))
            .Select(i => i.PlayerId));
        return missing;
    }

    private static bool HomeWinsChance(double homeRating, double awayRating) =>
        RandomNumberHelper.NextDouble() * (homeRating + awayRating) < homeRating;

    // ----- Team instructions ---------------------------------------------------------------

    // Each side's weight to score. With neutral instructions they are the strength shares and add up to 1.
    internal readonly record struct GoalWeights(double Home, double Away);

    // A step's goal chance never goes above this, however open both sides are.
    internal const double MaxGoalChancePerStep = 0.35;

    // The base chance of a card event every 5-15 minutes, and of an injury every 8-20.
    internal const double CardChancePerStep = 1.0 / 11;
    internal const double InjuryChancePerStep = 1.0 / 24;

    // Strength shares scaled by instructions: a side's weight is its share × its Threat × the
    // opponent's Exposure (see TacticalModifierRules).
    internal static GoalWeights CombineWeights(double homeRating, double awayRating, SideModifiers home, SideModifiers away)
    {
        var total = homeRating + awayRating;
        if (total <= 0) return new GoalWeights(0.5, 0.5);
        return new GoalWeights(
            homeRating / total * home.Threat * away.Exposure,
            awayRating / total * away.Threat * home.Exposure);
    }

    private GoalWeights GetGoalWeightsAtMinute(FixtureModel fixture, int minute)
    {
        var (homeRating, awayRating) = GetRatingsAtMinute(fixture, minute);
        return CombineWeights(homeRating, awayRating,
            GetSideModifiers(fixture, true, minute),
            GetSideModifiers(fixture, false, minute));
    }

    private SideModifiers GetSideModifiers(FixtureModel fixture, bool isHome, int minute)
    {
        var own = ClubHelper.GetClubById(isHome ? fixture.HomeClub.Id : fixture.AwayClub.Id);
        var opponent = ClubHelper.GetClubById(isHome ? fixture.AwayClub.Id : fixture.HomeClub.Id);

        return TacticalModifierRules.For(own.Instructions, opponent.Instructions,
            Fatigue(fixture, isHome, minute), Fatigue(fixture, !isHome, minute));
    }

    // How tired a side is at this minute (0 to 1), from how long it has pressed high and its Stamina.
    private double Fatigue(FixtureModel fixture, bool isHome, int minute)
    {
        var log = isHome ? fixture.HomePressing : fixture.AwayPressing;
        var pressed = PressingLog.MinutesPressedHigh(log, minute, CurrentPressing(fixture, isHome));
        var load = TacticalModifierRules.Load(minute, pressed);
        // Below the threshold there's no fatigue whatever the Stamina, so skip the lookup.
        if (load <= TacticalModifierRules.FatigueStartLoad)
            return 0;
        return TacticalModifierRules.FatigueProgress(load, AverageOutfieldStamina(fixture, isHome, minute));
    }

    private double AverageOutfieldStamina(FixtureModel fixture, bool isHome, int minute)
    {
        var club = isHome ? fixture.HomeClub : fixture.AwayClub;
        var outfield = OnPitchAt(fixture, isHome, minute, ClubHelper.GetStartingElevenByClub(club.Id))
            .Where(p => p.Slot.TacticSlotType != TacticSlotType.GK)
            .ToList();
        return outfield.Count == 0 ? 0 : outfield.Average(p => p.Player.Stamina);
    }

    // Rolls whether a card or injury happens at this step, and to which side. Each side's rate
    // scales its share and the overall chance; at neutral rates this is baseChance split 50/50.
    private static bool SideEventRoll(double baseChance, double homeRate, double awayRate, out bool isHome)
    {
        isHome = false;
        var totalRate = homeRate + awayRate;
        if (totalRate <= 0 || RandomNumberHelper.NextDouble() >= baseChance * totalRate / 2)
            return false;

        isHome = RandomNumberHelper.NextDouble() * totalRate < homeRate;
        return true;
    }

    private static HashSet<int> GetSentOffPlayerIds(IEnumerable<CardModel> cards, int minute)
    {
        return cards.Where(c => c.IsRed && c.Minute < minute).Select(c => c.PlayerId).ToHashSet();
    }

    // Returns true if the goal went to the home side, false for the away side, or null if
    // neither side has a player who could score.
    private bool? AddGoal(FixtureModel fixture, int minute,
        IEnumerable<TacticSlotModel> homeSlots, IEnumerable<TacticSlotModel> awaySlots,
        GoalWeights weights)
    {
        var homeFilled = homeSlots.Where(s => s.PlayerId != null).ToList();
        var awayFilled = awaySlots.Where(s => s.PlayerId != null).ToList();

        var isHome = HomeWinsChance(weights.Home, weights.Away);

        if (isHome && !homeFilled.Any()) isHome = false;
        else if (!isHome && !awayFilled.Any()) isHome = true;

        var slots = isHome ? homeFilled : awayFilled;
        if (!slots.Any()) return null;

        var missing = GetMissingPlayerIds(fixture, isHome, minute);
        var onPitch = slots.Where(s => !missing.Contains(s.PlayerId!.Value)).ToList();

        (isHome ? fixture.HomeScorers : fixture.AwayScorers).Add(new GoalModel
        {
            Minute = minute,
            PlayerId = GetGoalScorer(onPitch.Any() ? onPitch : slots)
        });

        return isHome;
    }

    private static void CountGoal(FixtureModel fixture, bool? scoredByHome)
    {
        if (scoredByHome == true)
            fixture.GoalsHome += 1;
        else if (scoredByHome == false)
            fixture.GoalsAway += 1;
    }

    public void ReGenerateFromMinute(FixtureModel fixture, int fromMinute)
    {
        // Extra time and penalties are pre-generated when the match reaches them, so a lineup
        // change after that point must regenerate them too.
        var inExtraTime = !fixture.Concluded && fromMinute >= 90 && fromMinute < 120;
        var hadShootout = fixture.HomeScorers.Concat(fixture.AwayScorers).Any(g => g.Minute > 120);

        fixture.HomeScorers.RemoveAll(g => g.Minute > fromMinute);
        fixture.AwayScorers.RemoveAll(g => g.Minute > fromMinute);
        fixture.HomeCards.RemoveAll(c => c.Minute > fromMinute);
        fixture.AwayCards.RemoveAll(c => c.Minute > fromMinute);
        fixture.HomeInjuries.RemoveAll(i => i.Minute > fromMinute);
        fixture.AwayInjuries.RemoveAll(i => i.Minute > fromMinute);
        fixture.HomePenalties.RemoveAll(p => p.Minute > fromMinute);
        fixture.AwayPenalties.RemoveAll(p => p.Minute > fromMinute);

        // A pressing change takes effect from now; the minutes already pressed still count towards fatigue.
        PressingLog.ChangeAt(fixture.HomePressing, fromMinute, CurrentPressing(fixture, true));
        PressingLog.ChangeAt(fixture.AwayPressing, fromMinute, CurrentPressing(fixture, false));

        var homeClubTacticSlots = ClubHelper.GetStartingElevenByClub(fixture.HomeClub.Id);
        var awayClubTacticSlots = ClubHelper.GetStartingElevenByClub(fixture.AwayClub.Id);

        if (fromMinute < 45)
        {
            SimulateCards(fixture, homeClubTacticSlots, awayClubTacticSlots, fromMinute + 1, 45);
            SimulateInjuries(fixture, homeClubTacticSlots, awayClubTacticSlots, fromMinute + 1, 45);
            SimulateGoals(fixture, homeClubTacticSlots, awayClubTacticSlots, fromMinute, 45, FirstHalfMaxStep, OpenPlayGoalChance, countGoals: false);
            AwardPenalties(fixture, homeClubTacticSlots, awayClubTacticSlots, fromMinute, 45, countGoals: false);
        }

        var secondHalfStart = Math.Max(fromMinute, 45);
        SimulateCards(fixture, homeClubTacticSlots, awayClubTacticSlots, secondHalfStart + 1, 90);
        SimulateInjuries(fixture, homeClubTacticSlots, awayClubTacticSlots, secondHalfStart + 1, 90);
        SimulateGoals(fixture, homeClubTacticSlots, awayClubTacticSlots, secondHalfStart, 90, LaterMaxStep, OpenPlayGoalChance, countGoals: false);
        AwardPenalties(fixture, homeClubTacticSlots, awayClubTacticSlots, secondHalfStart, 90, countGoals: false);

        if (inExtraTime)
            GenerateExtraTimeEventsFrom(fixture, fromMinute);

        if (hadShootout && fromMinute <= 120)
            GeneratePenaltyEvents(fixture);
    }

    public void PrepareMatch(FixtureModel fixture)
    {
        foreach (var clubId in new[] { fixture.HomeClub.Id, fixture.AwayClub.Id })
            PrepareClub(clubId);
        ChooseAIInstructions(fixture);
    }

    // AI sides set up for the opponent: a favourite attacks, an underdog sits deep, and anyone fit
    // enough presses high. Like a manager, a side compares the two XIs directly, ignoring venue and
    // the user's hidden rating boost. Sides in a league are within 5-8% of each other, so judging by
    // the match-engine share left the plan decided by venue alone (away sides defended, home sides
    // stayed Balanced).
    internal const double AIFavouriteRatio = 1.02;
    internal const double AIUnderdogRatio = 0.98;
    internal const double AIFavouritePressStamina = 70;
    internal const double AIAnyPressStamina = 78;
    internal const double AISoftenChance = 0.2;

    // Call after both line-ups are picked: the choice depends on both XIs. A side that settled
    // its approach the day before (the user's opponent) sticks to that plan.
    public void ChooseAIInstructions(FixtureModel fixture)
    {
        var homeStrength = StrengthAt(fixture, true, 0);
        var awayStrength = StrengthAt(fixture, false, 0);
        if (homeStrength <= 0 || awayStrength <= 0) return;

        foreach (var isHome in new[] { true, false })
        {
            var club = ClubHelper.GetClubById(isHome ? fixture.HomeClub.Id : fixture.AwayClub.Id);
            if (club.Id == State.MyClubId) continue;

            var planned = isHome ? fixture.HomePlannedInstructions : fixture.AwayPlannedInstructions;
            if (planned != null)
            {
                club.Instructions = planned.Clone();
                continue;
            }

            var ratio = isHome ? homeStrength / awayStrength : awayStrength / homeStrength;
            club.Instructions = PickAIInstructions(ratio, AverageOutfieldStamina(fixture, isHome, 0), RandomNumberHelper.NextDouble());
        }
    }

    // The day before the user's match, the AI opponent settles its approach. Line-ups aren't picked
    // yet, so both sides are judged on their best available XI (the best keeper and ten best
    // outfielders fit on match day), not on whoever the user has selected tonight.
    public void PlanOpponentInstructions(FixtureModel fixture)
    {
        var home = ExpectedXi(fixture.HomeClub.Id, fixture.Date);
        var away = ExpectedXi(fixture.AwayClub.Id, fixture.Date);

        double homeStrength = home.Sum(p => p.Rating);
        double awayStrength = away.Sum(p => p.Rating);
        if (homeStrength <= 0 || awayStrength <= 0) return;

        if (fixture.HomeClub.Id != State.MyClubId)
            fixture.HomePlannedInstructions = PickAIInstructions(homeStrength / awayStrength, OutfieldStamina(home), RandomNumberHelper.NextDouble());
        if (fixture.AwayClub.Id != State.MyClubId)
            fixture.AwayPlannedInstructions = PickAIInstructions(awayStrength / homeStrength, OutfieldStamina(away), RandomNumberHelper.NextDouble());
    }

    private List<PlayerModel> ExpectedXi(int clubId, DateOnly matchDate)
    {
        var available = State.Players
            .Where(p => p.Contract?.ClubId == clubId && p.SuspensionMatches == 0 && !p.IsInjured(matchDate))
            .OrderByDescending(p => p.Rating)
            .ToList();
        var keeper = available.FirstOrDefault(IsKeeper);
        var outfield = available.Where(p => !IsKeeper(p)).Take(10);
        return (keeper == null ? outfield : outfield.Prepend(keeper)).ToList();
    }

    private static bool IsKeeper(PlayerModel player) =>
        player.PreferredPosition.Split('/', StringSplitOptions.TrimEntries).Contains("GK");

    private static double OutfieldStamina(IEnumerable<PlayerModel> xi)
    {
        var outfield = xi.Where(p => !IsKeeper(p)).ToList();
        return outfield.Count == 0 ? 0 : outfield.Average(p => p.Stamina);
    }

    // strengthRatio: the side's XI strength divided by the opponent's.
    internal static TeamInstructionsModel PickAIInstructions(double strengthRatio, double averageStamina, double softenRoll)
    {
        var instructions = new TeamInstructionsModel();

        if (strengthRatio >= AIFavouriteRatio)
        {
            instructions.Mentality = Mentality.Attacking;
            instructions.Pressing = averageStamina >= AIFavouritePressStamina ? Pressing.High : Pressing.Medium;
        }
        else if (strengthRatio <= AIUnderdogRatio)
        {
            instructions.Mentality = Mentality.Defensive;
            instructions.Pressing = Pressing.Low;
        }
        else if (averageStamina >= AIAnyPressStamina)
        {
            instructions.Pressing = Pressing.High;
        }

        // Now and then a side is more cautious than its situation suggests, so the AI isn't fully predictable.
        if (softenRoll < AISoftenChance)
            instructions.Mentality = Mentality.Balanced;

        return instructions;
    }

    public void PrepareClub(int clubId)
    {
        if (clubId == State.MyClubId)
            return;

        var club = ClubHelper.GetClubById(clubId);
        TacticHelper.ResetTacticForClub(club);
        TacticHelper.FillEmptyTacticSlotsByClubId(clubId);

        club.PenaltyTakerIds = ClubHelper.GetStartingElevenByClub(clubId)
            .Where(s => s.TacticSlotType != TacticSlotType.GK && s.PlayerId != null)
            .Select(s => PlayerHelper.GetPlayerById(s.PlayerId!.Value))
            .OfType<PlayerModel>()
            .OrderByDescending(p => p.Penalties)
            .Take(AIPenaltyTakers)
            .Select(p => p.Id)
            .ToList();
    }
}
