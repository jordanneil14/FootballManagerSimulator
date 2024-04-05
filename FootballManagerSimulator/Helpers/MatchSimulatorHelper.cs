using FootballManagerSimulator.Interfaces;
using FootballManagerSimulator.Structures;

namespace FootballManagerSimulator.Helpers;

public class MatchSimulatorHelper : IMatchSimulatorHelper
{
    private static readonly Random Random = new();

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

        awayClubRating = (int)(awayClubRating * .94);

        var max = homeClubRating + awayClubRating;

        fixture.GoalsHome = 0;
        fixture.GoalsAway = 0;

        while (fixture.Minute <= 45)
        {
            var randomNumber = Random.Next(0, max);
            fixture.Minute += Random.Next(1, 12);
            if (randomNumber <= homeClubRating && Random.Next(1,6) == 3)
            {
                fixture.GoalsHome += 1;
            }
            else if (randomNumber > homeClubRating && Random.Next(1,6) == 3)
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

        awayClubRating = (int)(awayClubRating * .94);

        var max = homeClubRating + awayClubRating;
        var minute = 45;
        while (minute <= 90)
        {
            var randomNumber = Random.Next(0, max);
            minute += Random.Next(1, 12);
            if (randomNumber <= homeClubRating && Random.Next(1, 5) == 3)
            {
                fixture.GoalsHome += 1;
            }
            else if (randomNumber > homeClubRating && Random.Next(1, 5) == 3)
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
        if (fixture.HomeClub.Id != State.MyClub.Id)
        {
            TacticHelper.ResetTacticForClub(fixture.HomeClub);
            TacticHelper.FillEmptyTacticSlotsByClub(fixture.HomeClub);
        }

        if (fixture.AwayClub.Id != State.MyClub.Id)
        {
            TacticHelper.ResetTacticForClub(fixture.AwayClub);
            TacticHelper.FillEmptyTacticSlotsByClub(fixture.AwayClub);
        }
    }
}
