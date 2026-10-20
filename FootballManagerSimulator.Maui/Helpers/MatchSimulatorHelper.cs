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
        fixture.GoalsAway += 1;
        var competition = State.Competitions.First(p => p.Fixtures.Contains(fixture));
        EndFixture(fixture, competition);
    }

    private void SimulateExtraTime(FixtureModel fixture)
    {
        var homeClub = ClubHelper.GetClubById(fixture.HomeClub.Id);
        var awayClub = ClubHelper.GetClubById(fixture.AwayClub.Id);

        var homeClubTacticSlots = ClubHelper.GetStartingElevenByClub(fixture.HomeClub.Id);
        var awayClubTacticSlots = ClubHelper.GetStartingElevenByClub(fixture.AwayClub.Id);

        var homeClubRating = ClubHelper.GetStartingElevenSumRatingForClub(homeClub.Id);
        var awayClubRating = ClubHelper.GetStartingElevenSumRatingForClub(awayClub.Id);

        if (homeClub.Id == State.Clubs.First(p => p.Id == State.MyClubId).Id)
            homeClubRating = (int)(homeClubRating * 1.1);
        else
            awayClubRating = (int)(awayClubRating * 1.1);

        awayClubRating = (int)(awayClubRating * .8);

        var max = homeClubRating + awayClubRating;
        while (fixture.Minute <= 90)
        {
            var randomNumber = RandomNumberHelper.Next(0, max);
            var isGoal = RandomNumberHelper.Next(1, 8) == 3;
            fixture.Minute += RandomNumberHelper.Next(1, 10);
            if (!isGoal) continue;

            if (randomNumber <= homeClubRating)
            {
                fixture.HomeScorers.Add(new GoalModel
                {
                    Minute = fixture.Minute,
                    PlayerId = GetGoalScorer(homeClubTacticSlots)
                });
                fixture.GoalsHome += 1;
            }
            else if (randomNumber > homeClubRating)
            {
                fixture.AwayScorers.Add(new GoalModel
                {
                    Minute = fixture.Minute,
                    PlayerId = GetGoalScorer(awayClubTacticSlots)
                });
                fixture.GoalsAway += 1;
            }
        }


        fixture.Minute = 120;

        if (fixture.GoalsHome != fixture.GoalsAway)
        {
            var competition = State.Competitions.First(p => p.Fixtures.Contains(fixture));
            EndFixture(fixture, competition);
        }
    }

    private void SimulateFirstHalf(FixtureModel fixture)
    {
        var homeClub = ClubHelper.GetClubById(fixture.HomeClub.Id);
        var awayClub = ClubHelper.GetClubById(fixture.AwayClub.Id);

        var homeClubTacticSlots = ClubHelper.GetStartingElevenByClub(fixture.HomeClub.Id);
        var awayClubTacticSlots = ClubHelper.GetStartingElevenByClub(fixture.AwayClub.Id);

        var homeClubRating = ClubHelper.GetStartingElevenSumRatingForClub(homeClub.Id);
        var awayClubRating = ClubHelper.GetStartingElevenSumRatingForClub(awayClub.Id);

        if (homeClub.Id == State.Clubs.First(p => p.Id == State.MyClubId).Id)
            homeClubRating = (int)(homeClubRating * 1.1);
        else
            awayClubRating = (int)(awayClubRating * 1.1);

        awayClubRating = (int)(awayClubRating * .8);

        var max = homeClubRating + awayClubRating;

        fixture.GoalsHome = 0;
        fixture.GoalsAway = 0;

        while (fixture.Minute <= 45)
        {
            var randomNumber = RandomNumberHelper.Next(0, max);
            var isGoal = RandomNumberHelper.Next(1, 8) == 3;
            fixture.Minute += RandomNumberHelper.Next(1, 11);
            if (!isGoal) continue;

            if (randomNumber <= homeClubRating)
            {
                fixture.HomeScorers.Add(new GoalModel
                {
                    Minute = fixture.Minute,
                    PlayerId = GetGoalScorer(homeClubTacticSlots)
                });
                fixture.GoalsHome += 1;
            }
            else if (randomNumber > homeClubRating)
            {
                fixture.AwayScorers.Add(new GoalModel
                {
                    Minute = fixture.Minute,
                    PlayerId = GetGoalScorer(awayClubTacticSlots)
                });
                fixture.GoalsAway += 1;
            }
        }

        SimulateCards(fixture, homeClubTacticSlots, awayClubTacticSlots, 1, 45);

        fixture.Minute = 45;
    }

    private int GetGoalScorer(IEnumerable<TacticSlotModel> tacticSlots)
    {
        var playerRatingModels = new List<PlayerRatingModel>();
        foreach (var slot in tacticSlots)
        {
            var player = PlayerHelper.GetPlayerById(slot.PlayerId.Value);
            playerRatingModels.Add(new PlayerRatingModel
            {
                PlayerId = slot.PlayerId.Value,
                Rating = playerRatingModels.Sum(x => x.Rating) + player.ScoringProbability
            });
        }

        var sum = playerRatingModels.Max(p => p.Rating);
        var randomNumber = RandomNumberHelper.Next(1, (int)sum);

        return playerRatingModels
            .Where(p => randomNumber <= p.Rating)
            .First()
            .PlayerId;
    }

    private void SimulateSecondHalf(FixtureModel fixture, ICompetition competition)
    {
        var homeClub = ClubHelper.GetClubById(fixture.HomeClub.Id);
        var awayClub = ClubHelper.GetClubById(fixture.AwayClub.Id);

        var homeClubTacticSlots = ClubHelper.GetStartingElevenByClub(fixture.HomeClub.Id);
        var awayClubTacticSlots = ClubHelper.GetStartingElevenByClub(fixture.AwayClub.Id);

        var homeClubRating = ClubHelper.GetStartingElevenSumRatingForClub(homeClub.Id);
        var awayClubRating = ClubHelper.GetStartingElevenSumRatingForClub(awayClub.Id);

        if (homeClub.Id == State.Clubs.First(p => p.Id == State.MyClubId).Id)
            homeClubRating = (int)(homeClubRating * 1.1);
        else
            awayClubRating = (int)(awayClubRating * 1.1);

        awayClubRating = (int)(awayClubRating * .8);

        var max = homeClubRating + awayClubRating;
        while (fixture.Minute <= 90)
        {
            var randomNumber = RandomNumberHelper.Next(0, max);
            var isGoal = RandomNumberHelper.Next(1, 8) == 3;
            fixture.Minute += RandomNumberHelper.Next(1, 10);
            if (!isGoal) continue;

            if (randomNumber <= homeClubRating)
            {
                fixture.HomeScorers.Add(new GoalModel
                {
                    Minute = fixture.Minute,
                    PlayerId = GetGoalScorer(homeClubTacticSlots)
                });
                fixture.GoalsHome += 1;
            }
            else if (randomNumber > homeClubRating)
            {
                fixture.AwayScorers.Add(new GoalModel
                {
                    Minute = fixture.Minute,
                    PlayerId = GetGoalScorer(awayClubTacticSlots)
                });
                fixture.GoalsAway += 1;
            }
        }

        SimulateCards(fixture, homeClubTacticSlots, awayClubTacticSlots, 46, 90);

        fixture.Minute = 90;

        if ((competition.Type != CompetitionType.Cup && competition.Type != CompetitionType.EnglishCommunityShield) || fixture.GoalsHome != fixture.GoalsAway)
            EndFixture(fixture, competition);
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
            var playerIds = isHome ? homePlayerIds : awayPlayerIds;
            var cards = isHome ? fixture.HomeCards : fixture.AwayCards;

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

        var (homeRating, awayRating) = GetAdjustedRatings(fixture);
        var max = homeRating + awayRating;

        fixture.GoalsHome = 0;
        fixture.GoalsAway = 0;

        var minute = 0;
        while (minute <= 45)
        {
            minute += RandomNumberHelper.Next(1, 11);
            if (minute > 45) break;
            if (RandomNumberHelper.Next(1, 8) != 3) continue;
            AddGoal(fixture, minute, max, homeRating, homeClubTacticSlots, awayClubTacticSlots);
        }

        SimulateCards(fixture, homeClubTacticSlots, awayClubTacticSlots, 1, 45);

        minute = 45;
        while (minute <= 90)
        {
            minute += RandomNumberHelper.Next(1, 10);
            if (minute > 90) break;
            if (RandomNumberHelper.Next(1, 8) != 3) continue;
            AddGoal(fixture, minute, max, homeRating, homeClubTacticSlots, awayClubTacticSlots);
        }

        SimulateCards(fixture, homeClubTacticSlots, awayClubTacticSlots, 46, 90);
    }

    public void GenerateExtraTimeEvents(FixtureModel fixture)
    {
        var homeClubTacticSlots = ClubHelper.GetStartingElevenByClub(fixture.HomeClub.Id);
        var awayClubTacticSlots = ClubHelper.GetStartingElevenByClub(fixture.AwayClub.Id);

        var (homeRating, awayRating) = GetAdjustedRatings(fixture);
        var max = homeRating + awayRating;

        var minute = 90;
        while (minute <= 120)
        {
            minute += RandomNumberHelper.Next(1, 10);
            if (minute > 120) break;
            if (RandomNumberHelper.Next(1, 8) != 3) continue;
            AddGoal(fixture, minute, max, homeRating, homeClubTacticSlots, awayClubTacticSlots);
        }
    }

    public void GeneratePenaltyEvents(FixtureModel fixture)
    {
        fixture.AwayScorers.Add(new GoalModel
        {
            Minute = 121,
            PlayerId = GetGoalScorer(ClubHelper.GetStartingElevenByClub(fixture.AwayClub.Id))
        });
    }

    public void FinaliseMatch(FixtureModel fixture, ICompetition competition)
    {
        EndFixture(fixture, competition);
    }

    private (int homeRating, int awayRating) GetAdjustedRatings(FixtureModel fixture)
    {
        var homeRating = ClubHelper.GetStartingElevenSumRatingForClub(fixture.HomeClub.Id);
        var awayRating = ClubHelper.GetStartingElevenSumRatingForClub(fixture.AwayClub.Id);

        if (fixture.HomeClub.Id == State.Clubs.First(p => p.Id == State.MyClubId).Id)
            homeRating = (int)(homeRating * 1.1);
        else
            awayRating = (int)(awayRating * 1.1);

        awayRating = (int)(awayRating * .8);

        return (homeRating, awayRating);
    }

    private void AddGoal(FixtureModel fixture, int minute, int max, int homeRating,
        IEnumerable<TacticSlotModel> homeSlots, IEnumerable<TacticSlotModel> awaySlots)
    {
        var randomNumber = RandomNumberHelper.Next(0, max);
        if (randomNumber <= homeRating)
        {
            fixture.HomeScorers.Add(new GoalModel
            {
                Minute = minute,
                PlayerId = GetGoalScorer(homeSlots)
            });
        }
        else
        {
            fixture.AwayScorers.Add(new GoalModel
            {
                Minute = minute,
                PlayerId = GetGoalScorer(awaySlots)
            });
        }
    }

    public void ReGenerateFromMinute(FixtureModel fixture, int fromMinute)
    {
        fixture.HomeScorers.RemoveAll(g => g.Minute > fromMinute);
        fixture.AwayScorers.RemoveAll(g => g.Minute > fromMinute);
        fixture.HomeCards.RemoveAll(c => c.Minute > fromMinute);
        fixture.AwayCards.RemoveAll(c => c.Minute > fromMinute);

        var homeClubTacticSlots = ClubHelper.GetStartingElevenByClub(fixture.HomeClub.Id);
        var awayClubTacticSlots = ClubHelper.GetStartingElevenByClub(fixture.AwayClub.Id);

        var (homeRating, awayRating) = GetAdjustedRatings(fixture);
        var max = homeRating + awayRating;

        if (fromMinute < 45)
        {
            var minute = fromMinute;
            while (minute <= 45)
            {
                minute += RandomNumberHelper.Next(1, 11);
                if (minute > 45) break;
                if (RandomNumberHelper.Next(1, 8) != 3) continue;
                AddGoal(fixture, minute, max, homeRating, homeClubTacticSlots, awayClubTacticSlots);
            }
            SimulateCards(fixture, homeClubTacticSlots, awayClubTacticSlots, fromMinute + 1, 45);
        }

        var secondHalfStart = Math.Max(fromMinute, 45);
        var minute2 = secondHalfStart;
        while (minute2 <= 90)
        {
            minute2 += RandomNumberHelper.Next(1, 10);
            if (minute2 > 90) break;
            if (RandomNumberHelper.Next(1, 8) != 3) continue;
            AddGoal(fixture, minute2, max, homeRating, homeClubTacticSlots, awayClubTacticSlots);
        }
        SimulateCards(fixture, homeClubTacticSlots, awayClubTacticSlots, secondHalfStart + 1, 90);
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
