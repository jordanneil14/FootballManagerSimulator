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
    public void ProcessMatch(Fixture fixture, ICompetition competition)
    {
        //if (fixture.HomeClub.Id == state.MyClubId || fixture.AwayClub.Id == state.MyClubId)
        //{

        //}

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
        var homeClub = clubHelper.GetClubById(fixture.HomeClub.Id);
        var awayClub = clubHelper.GetClubById(fixture.AwayClub.Id);

        var homeClubTacticSlots = clubHelper.GetStartingElevenByClub(fixture.HomeClub.Id);
        var awayClubTacticSlots = clubHelper.GetStartingElevenByClub(fixture.AwayClub.Id);

        var homeClubRating = clubHelper.GetStartingElevenSumRatingForClub(homeClub.Id);
        var awayClubRating = clubHelper.GetStartingElevenSumRatingForClub(awayClub.Id);

        if (homeClub.Id == state.Clubs.First(p => p.Id == state.MyClubId).Id)
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
        var homeClub = clubHelper.GetClubById(fixture.HomeClub.Id);
        var awayClub = clubHelper.GetClubById(fixture.AwayClub.Id);

        var homeClubTacticSlots = clubHelper.GetStartingElevenByClub(fixture.HomeClub.Id);
        var awayClubTacticSlots = clubHelper.GetStartingElevenByClub(fixture.AwayClub.Id);

        var homeClubRating = clubHelper.GetStartingElevenSumRatingForClub(homeClub.Id);
        var awayClubRating = clubHelper.GetStartingElevenSumRatingForClub(awayClub.Id);

        if (homeClub.Id == state.Clubs.First(p => p.Id == state.MyClubId).Id)
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
            var player = playerHelper.GetPlayerById(slot.PlayerId.Value);
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
        var homeClub = clubHelper.GetClubById(fixture.HomeClub.Id);
        var awayClub = clubHelper.GetClubById(fixture.AwayClub.Id);

        var homeClubTacticSlots = clubHelper.GetStartingElevenByClub(fixture.HomeClub.Id);
        var awayClubTacticSlots = clubHelper.GetStartingElevenByClub(fixture.AwayClub.Id);

        var homeClubRating = clubHelper.GetStartingElevenSumRatingForClub(homeClub.Id);
        var awayClubRating = clubHelper.GetStartingElevenSumRatingForClub(awayClub.Id);

        if (homeClub.Id == state.Clubs.First(p => p.Id == state.MyClubId).Id)
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
        if (fixture.HomeClub.Id != state.Clubs.First(p => p.Id == state.MyClubId).Id)
        {
            tacticHelper.ResetTacticForClub(fixture.HomeClub);
            tacticHelper.FillEmptyTacticSlotsByClubId(fixture.HomeClub.Id);
        }

        if (fixture.AwayClub.Id != state.Clubs.First(p => p.Id == state.MyClubId).Id)
        {
            tacticHelper.ResetTacticForClub(fixture.AwayClub);
            tacticHelper.FillEmptyTacticSlotsByClubId(fixture.AwayClub.Id);
        }
    }
}
