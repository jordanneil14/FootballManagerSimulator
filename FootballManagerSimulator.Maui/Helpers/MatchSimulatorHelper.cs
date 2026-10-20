using FootballManagerSimulator.Maui.Enums;
using FootballManagerSimulator.Maui.Interfaces;
using FootballManagerSimulator.Maui.Models;

namespace FootballManagerSimulator.Maui.Helpers;

public class MatchSimulatorHelper(
    IClubHelper clubHelper,
    ITacticHelper tacticHelper,
    IPlayerHelper playerHelper,
    IState state) : IMatchSimulatorHelper
{
    private readonly IClubHelper ClubHelper = clubHelper;
    private readonly ITacticHelper TacticHelper = tacticHelper;
    private readonly IPlayerHelper PlayerHelper = playerHelper;
    private readonly IState State = state;

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
        if (RandomNumberHelper.Next(0, homeRating + awayRating) < homeRating)
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

        while (fixture.Minute <= 90)
        {
            var isGoal = RandomNumberHelper.Next(1, 8) == 3;
            fixture.Minute += RandomNumberHelper.Next(1, 10);
            if (!isGoal) continue;

            CountGoal(fixture, AddGoal(fixture, fixture.Minute, homeClubTacticSlots, awayClubTacticSlots));
        }

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

    private void SimulateFirstHalf(FixtureModel fixture)
    {
        var homeClubTacticSlots = ClubHelper.GetStartingElevenByClub(fixture.HomeClub.Id);
        var awayClubTacticSlots = ClubHelper.GetStartingElevenByClub(fixture.AwayClub.Id);

        fixture.GoalsHome = 0;
        fixture.GoalsAway = 0;

        SimulateCards(fixture, homeClubTacticSlots, awayClubTacticSlots, 1, 45);

        while (fixture.Minute <= 45)
        {
            var isGoal = RandomNumberHelper.Next(1, 8) == 3;
            fixture.Minute += RandomNumberHelper.Next(1, 11);
            if (!isGoal) continue;

            CountGoal(fixture, AddGoal(fixture, fixture.Minute, homeClubTacticSlots, awayClubTacticSlots));
        }

        fixture.Minute = 45;
    }

    private int GetGoalScorer(IEnumerable<TacticSlotModel> tacticSlots)
    {
        var playerRatingModels = new List<PlayerRatingModel>();
        var cumulative = 0d;
        foreach (var slot in tacticSlots)
        {
            var player = PlayerHelper.GetPlayerById(slot.PlayerId.Value);
            cumulative += player.ScoringProbability;
            playerRatingModels.Add(new PlayerRatingModel
            {
                PlayerId = slot.PlayerId.Value,
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

    private void SimulateSecondHalf(FixtureModel fixture, ICompetition competition)
    {
        var homeClubTacticSlots = ClubHelper.GetStartingElevenByClub(fixture.HomeClub.Id);
        var awayClubTacticSlots = ClubHelper.GetStartingElevenByClub(fixture.AwayClub.Id);

        SimulateCards(fixture, homeClubTacticSlots, awayClubTacticSlots, 46, 90);

        while (fixture.Minute <= 90)
        {
            var isGoal = RandomNumberHelper.Next(1, 8) == 3;
            fixture.Minute += RandomNumberHelper.Next(1, 10);
            if (!isGoal) continue;

            CountGoal(fixture, AddGoal(fixture, fixture.Minute, homeClubTacticSlots, awayClubTacticSlots));
        }

        fixture.Minute = 90;

        if (fixture.FixtureType == FixtureType.PlayoffSecondLeg)
        {
            PopulateFirstLegScores(fixture, competition);
            var homeAgg = (fixture.GoalsHome ?? 0) + (fixture.FirstLegGoalsAway ?? 0);
            var awayAgg = (fixture.GoalsAway ?? 0) + (fixture.FirstLegGoalsHome ?? 0);
            if (homeAgg != awayAgg)
                EndFixture(fixture, competition);
        }
        else if (fixture.FixtureType == FixtureType.PlayoffFinal ||
                 (fixture.FixtureType == FixtureType.Playoff && competition.Type == CompetitionType.EnglishNationalLeague))
        {
            if (fixture.GoalsHome != fixture.GoalsAway)
                EndFixture(fixture, competition);
        }
        else if ((competition.Type != CompetitionType.Cup && competition.Type != CompetitionType.EnglishCommunityShield) || fixture.GoalsHome != fixture.GoalsAway)
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

        for (var minute = minuteFrom; minute <= minuteTo; minute += RandomNumberHelper.Next(5, 15))
        {
            if (RandomNumberHelper.Next(1, 12) != 1)
                continue;

            var isHome = RandomNumberHelper.Next(0, 2) == 0;
            var cards = isHome ? fixture.HomeCards : fixture.AwayCards;
            var sentOff = GetSentOffPlayerIds(cards, minute + 1);
            var playerIds = (isHome ? homePlayerIds : awayPlayerIds).Where(id => !sentOff.Contains(id)).ToList();

            if (!playerIds.Any()) continue;

            var playerId = playerIds[RandomNumberHelper.Next(0, playerIds.Count)];

            var isStraightRed = RandomNumberHelper.Next(1, 12) == 1;

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
            player.SuspensionMatches--;
    }

    private void EndFixture(FixtureModel fixture, ICompetition competition)
    {
        fixture.Concluded = true;

        if (fixture.GoalsHome > fixture.GoalsAway)
            fixture.ClubWon = fixture.HomeClub;
        else if (fixture.GoalsAway > fixture.GoalsHome)
            fixture.ClubWon = fixture.AwayClub;
        else
            fixture.ClubWon = null;

        ReduceSuspensions(fixture, competition);
        ApplySuspensions(fixture, competition);
    }

    public void PreGenerateMatch(FixtureModel fixture)
    {
        var homeClubTacticSlots = ClubHelper.GetStartingElevenByClub(fixture.HomeClub.Id);
        var awayClubTacticSlots = ClubHelper.GetStartingElevenByClub(fixture.AwayClub.Id);

        fixture.GoalsHome = 0;
        fixture.GoalsAway = 0;

        SimulateCards(fixture, homeClubTacticSlots, awayClubTacticSlots, 1, 45);

        var minute = 0;
        while (minute <= 45)
        {
            minute += RandomNumberHelper.Next(1, 11);
            if (minute > 45) break;
            if (RandomNumberHelper.Next(1, 8) != 3) continue;
            AddGoal(fixture, minute, homeClubTacticSlots, awayClubTacticSlots);
        }

        SimulateCards(fixture, homeClubTacticSlots, awayClubTacticSlots, 46, 90);

        minute = 45;
        while (minute <= 90)
        {
            minute += RandomNumberHelper.Next(1, 10);
            if (minute > 90) break;
            if (RandomNumberHelper.Next(1, 8) != 3) continue;
            AddGoal(fixture, minute, homeClubTacticSlots, awayClubTacticSlots);
        }
    }

    public void GenerateExtraTimeEvents(FixtureModel fixture) => GenerateExtraTimeEventsFrom(fixture, 90);

    private void GenerateExtraTimeEventsFrom(FixtureModel fixture, int fromMinute)
    {
        var homeClubTacticSlots = ClubHelper.GetStartingElevenByClub(fixture.HomeClub.Id);
        var awayClubTacticSlots = ClubHelper.GetStartingElevenByClub(fixture.AwayClub.Id);

        var minute = fromMinute;
        while (minute <= 120)
        {
            minute += RandomNumberHelper.Next(1, 10);
            if (minute > 120) break;
            if (RandomNumberHelper.Next(1, 8) != 3) continue;
            AddGoal(fixture, minute, homeClubTacticSlots, awayClubTacticSlots);
        }
    }

    public void GeneratePenaltyEvents(FixtureModel fixture)
    {
        AddGoal(fixture, 121,
            ClubHelper.GetStartingElevenByClub(fixture.HomeClub.Id),
            ClubHelper.GetStartingElevenByClub(fixture.AwayClub.Id));
    }

    public void FinaliseMatch(FixtureModel fixture, ICompetition competition)
    {
        EndFixture(fixture, competition);
    }

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

    private (int homeRating, int awayRating) GetAdjustedRatings(FixtureModel fixture)
    {
        var homeRating = ClubHelper.GetStartingElevenSumRatingForClub(fixture.HomeClub.Id);
        var awayRating = ClubHelper.GetStartingElevenSumRatingForClub(fixture.AwayClub.Id);

        if (fixture.HomeClub.Id == State.MyClubId)
            homeRating = (int)(homeRating * 1.1);
        else if (fixture.AwayClub.Id == State.MyClubId)
            awayRating = (int)(awayRating * 1.1);

        awayRating = (int)(awayRating * .8);

        return (homeRating, awayRating);
    }

    // Each player sent off before the given minute costs the team 20% of its rating.
    private (int homeRating, int awayRating) GetRatingsAtMinute(FixtureModel fixture, int minute)
    {
        var (homeRating, awayRating) = GetAdjustedRatings(fixture);
        var homeReds = GetSentOffPlayerIds(fixture.HomeCards, minute).Count;
        var awayReds = GetSentOffPlayerIds(fixture.AwayCards, minute).Count;

        return ((int)(homeRating * Math.Pow(0.8, homeReds)), (int)(awayRating * Math.Pow(0.8, awayReds)));
    }

    private static HashSet<int> GetSentOffPlayerIds(IEnumerable<CardModel> cards, int minute)
    {
        return cards.Where(c => c.IsRed && c.Minute < minute).Select(c => c.PlayerId).ToHashSet();
    }

    // Returns true if the goal went to the home side, false for the away side, or null if
    // neither side has a player who could score.
    private bool? AddGoal(FixtureModel fixture, int minute,
        IEnumerable<TacticSlotModel> homeSlots, IEnumerable<TacticSlotModel> awaySlots)
    {
        var homeFilled = homeSlots.Where(s => s.PlayerId != null).ToList();
        var awayFilled = awaySlots.Where(s => s.PlayerId != null).ToList();

        var (homeRating, awayRating) = GetRatingsAtMinute(fixture, minute);
        var isHome = RandomNumberHelper.Next(0, homeRating + awayRating) < homeRating;

        if (isHome && !homeFilled.Any()) isHome = false;
        else if (!isHome && !awayFilled.Any()) isHome = true;

        var slots = isHome ? homeFilled : awayFilled;
        if (!slots.Any()) return null;

        var sentOff = GetSentOffPlayerIds(isHome ? fixture.HomeCards : fixture.AwayCards, minute);
        var onPitch = slots.Where(s => !sentOff.Contains(s.PlayerId!.Value)).ToList();

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
        var hadPenalty = fixture.HomeScorers.Concat(fixture.AwayScorers).Any(g => g.Minute > 120);

        fixture.HomeScorers.RemoveAll(g => g.Minute > fromMinute);
        fixture.AwayScorers.RemoveAll(g => g.Minute > fromMinute);
        fixture.HomeCards.RemoveAll(c => c.Minute > fromMinute);
        fixture.AwayCards.RemoveAll(c => c.Minute > fromMinute);

        var homeClubTacticSlots = ClubHelper.GetStartingElevenByClub(fixture.HomeClub.Id);
        var awayClubTacticSlots = ClubHelper.GetStartingElevenByClub(fixture.AwayClub.Id);

        if (fromMinute < 45)
        {
            SimulateCards(fixture, homeClubTacticSlots, awayClubTacticSlots, fromMinute + 1, 45);
            var minute = fromMinute;
            while (minute <= 45)
            {
                minute += RandomNumberHelper.Next(1, 11);
                if (minute > 45) break;
                if (RandomNumberHelper.Next(1, 8) != 3) continue;
                AddGoal(fixture, minute, homeClubTacticSlots, awayClubTacticSlots);
            }
        }

        var secondHalfStart = Math.Max(fromMinute, 45);
        SimulateCards(fixture, homeClubTacticSlots, awayClubTacticSlots, secondHalfStart + 1, 90);
        var minute2 = secondHalfStart;
        while (minute2 <= 90)
        {
            minute2 += RandomNumberHelper.Next(1, 10);
            if (minute2 > 90) break;
            if (RandomNumberHelper.Next(1, 8) != 3) continue;
            AddGoal(fixture, minute2, homeClubTacticSlots, awayClubTacticSlots);
        }

        if (inExtraTime)
            GenerateExtraTimeEventsFrom(fixture, fromMinute);

        if (hadPenalty && fromMinute <= 120)
            GeneratePenaltyEvents(fixture);
    }

    public void PrepareMatch(FixtureModel fixture)
    {
        if (fixture.HomeClub.Id != State.Clubs.First(p => p.Id == State.MyClubId).Id)
        {
            TacticHelper.ResetTacticForClub(fixture.HomeClub);
            TacticHelper.FillEmptyTacticSlotsByClubId(fixture.HomeClub.Id);
        }

        if (fixture.AwayClub.Id != State.Clubs.First(p => p.Id == State.MyClubId).Id)
        {
            TacticHelper.ResetTacticForClub(fixture.AwayClub);
            TacticHelper.FillEmptyTacticSlotsByClubId(fixture.AwayClub.Id);
        }
    }
}
