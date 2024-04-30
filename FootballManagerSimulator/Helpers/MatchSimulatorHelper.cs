using FootballManagerSimulator.Interfaces;
using FootballManagerSimulator.Structures;

namespace FootballManagerSimulator.Helpers;

public class MatchSimulatorHelper : IMatchSimulatorHelper
{
    private readonly IClubHelper ClubHelper;
    private readonly ITacticHelper TacticHelper;
    private readonly IState State;

    public MatchSimulatorHelper(
        IClubHelper clubHelper,
        ITacticHelper tacticHelper,
        IState state)
    {
        ClubHelper = clubHelper;
        TacticHelper = tacticHelper;
        State = state;
    }

    public void ProcessMatch(Fixture fixture)
    {
        if (fixture.Minute == 0)
        {
            SimulateFirstHalf(fixture);
            return;
        }

        if (fixture.Minute == 45)
        {
            SimulateSecondHalf(fixture);
            ConcludeFixture(fixture);
            return;
        }    
    }

    private void SimulateFirstHalf(Fixture fixture)
    {
        var homeClub = ClubHelper.GetClubById(fixture.HomeClub.Id);
        var awayClub = ClubHelper.GetClubById(fixture.AwayClub.Id);

        var homeClubRating = ClubHelper.GetStartingElevenSumRatingForClub(homeClub);
        var awayClubRating = ClubHelper.GetStartingElevenSumRatingForClub(awayClub);

        if (homeClub.Id == State.MyClub.Id)
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
                fixture.GoalsHome += 1;
            }
            else if (randomNumber > homeClubRating)
            {
                fixture.GoalsAway += 1;
            }
        }

        fixture.Minute = 45;
    }

    private void SimulateSecondHalf(Fixture fixture)
    {
        var homeClub = ClubHelper.GetClubById(fixture.HomeClub.Id);
        var awayClub = ClubHelper.GetClubById(fixture.AwayClub.Id);

        var homeClubRating = ClubHelper.GetStartingElevenSumRatingForClub(homeClub);
        var awayClubRating = ClubHelper.GetStartingElevenSumRatingForClub(awayClub);

        if (homeClub.Id == State.MyClub.Id)
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
                fixture.GoalsHome += 1;
            }
            else if (randomNumber > homeClubRating)
            {
                fixture.GoalsAway += 1;
            }
        }

        fixture.Minute = 90;
    }

    private static void ConcludeFixture(Fixture fixture)
    {
        fixture.Concluded = true;
    }

    public void PrepareMatch(Fixture fixture)
    {
        TacticHelper.ResetTacticForClub(fixture.HomeClub);
        TacticHelper.FillEmptyTacticSlotsByClubId(fixture.HomeClub.Id);
        TacticHelper.ResetTacticForClub(fixture.AwayClub);
        TacticHelper.FillEmptyTacticSlotsByClubId(fixture.AwayClub.Id);
    }
}
