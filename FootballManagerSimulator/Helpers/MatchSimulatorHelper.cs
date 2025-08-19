using FootballManagerSimulator.Enums;
using FootballManagerSimulator.Interfaces;
using FootballManagerSimulator.Models;

namespace FootballManagerSimulator.Helpers;

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

    public void ProcessMatch(Fixture fixture, ICompetition competition)
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

    private void SimulatePenalties(Fixture fixture)
    {
        fixture.GoalsAway += 1;
        EndFixture(fixture);
    }

    private void SimulateExtraTime(Fixture fixture)
    {
        var homeClub = ClubHelper.GetClubById(fixture.HomeClub.Id);
        var awayClub = ClubHelper.GetClubById(fixture.AwayClub.Id);

        var homeClubTacticSlots = ClubHelper.GetStartingElevenByClub(fixture.HomeClub.Id);
        var awayClubTacticSlots = ClubHelper.GetStartingElevenByClub(fixture.AwayClub.Id);

        var homeClubRating = ClubHelper.GetStartingElevenSumRatingForClub(homeClub.Id);
        var awayClubRating = ClubHelper.GetStartingElevenSumRatingForClub(awayClub.Id);

        if (homeClub.Id == State.Clubs.First(p => p.Id == State.MyClubId).Id)
            homeClubRating = (int)(homeClubRating * 1.3);
        else
            awayClubRating = (int)(awayClubRating * 1.3);

        awayClubRating = (int)(awayClubRating * .8);

        var max = homeClubRating + awayClubRating;
        while (fixture.Minute <= 90)
        {
            var randomNumber = RandomNumberHelper.Next(0, max);
            var isGoal = RandomNumberHelper.Next(1, 6) == 3;
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
            EndFixture(fixture);
    }

    private void SimulateFirstHalf(Fixture fixture)
    {
        var homeClub = ClubHelper.GetClubById(fixture.HomeClub.Id);
        var awayClub = ClubHelper.GetClubById(fixture.AwayClub.Id);

        var homeClubTacticSlots = ClubHelper.GetStartingElevenByClub(fixture.HomeClub.Id);
        var awayClubTacticSlots = ClubHelper.GetStartingElevenByClub(fixture.AwayClub.Id);

        var homeClubRating = ClubHelper.GetStartingElevenSumRatingForClub(homeClub.Id);
        var awayClubRating = ClubHelper.GetStartingElevenSumRatingForClub(awayClub.Id);

        if (homeClub.Id == State.Clubs.First(p => p.Id == State.MyClubId).Id)
            homeClubRating = (int)(homeClubRating * 1.3);
        else
            awayClubRating = (int)(awayClubRating * 1.3);

        awayClubRating = (int)(awayClubRating * .8);

        var max = homeClubRating + awayClubRating;

        fixture.GoalsHome = 0;
        fixture.GoalsAway = 0;

        while (fixture.Minute <= 45)
        {
            var randomNumber = RandomNumberHelper.Next(0, max);
            var isGoal = RandomNumberHelper.Next(1, 6) == 3;
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

        fixture.Minute = 45;
    }

    private int GetGoalScorer(IEnumerable<TacticSlot> tacticSlots)
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

    private void SimulateSecondHalf(Fixture fixture, ICompetition competition)
    {
        var homeClub = ClubHelper.GetClubById(fixture.HomeClub.Id);
        var awayClub = ClubHelper.GetClubById(fixture.AwayClub.Id);

        var homeClubTacticSlots = ClubHelper.GetStartingElevenByClub(fixture.HomeClub.Id);
        var awayClubTacticSlots = ClubHelper.GetStartingElevenByClub(fixture.AwayClub.Id);

        var homeClubRating = ClubHelper.GetStartingElevenSumRatingForClub(homeClub.Id);
        var awayClubRating = ClubHelper.GetStartingElevenSumRatingForClub(awayClub.Id);

        if (homeClub.Id == State.Clubs.First(p => p.Id == State.MyClubId).Id)
            homeClubRating = (int)(homeClubRating * 1.3);
        else
            awayClubRating = (int)(awayClubRating * 1.3);

        awayClubRating = (int)(awayClubRating * .8);

        var max = homeClubRating + awayClubRating;
        while (fixture.Minute <= 90)
        {
            var randomNumber = RandomNumberHelper.Next(0, max);
            var isGoal = RandomNumberHelper.Next(1, 6) == 3;
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

        fixture.Minute = 90;

        if (competition.Type != CompetitionType.Cup)
            EndFixture(fixture);

        if (fixture.GoalsHome == fixture.GoalsAway)
            EndFixture(fixture);
    }

    public void ConcludeFixture(Fixture fixture, ICompetition competition)
    {
        while (!fixture.Concluded)
        {
            ProcessMatch(fixture, competition);
        }
    }

    private static void EndFixture(Fixture fixture)
    {
        fixture.Concluded = true;

        if (fixture.GoalsAway > fixture.GoalsHome)
            fixture.ClubWon = fixture.AwayClub;
        else
            fixture.ClubWon = fixture.HomeClub;

    }

    public void PrepareMatch(Fixture fixture)
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
