using FootballManagerSimulator.Interfaces;
using FootballManagerSimulator.Structures;

namespace FootballManagerSimulator.Helpers;

public class MatchSimulator : IMatchSimulator
{
    private static readonly Random Random = new();

    private readonly IUtils Utils;

    public MatchSimulator(IUtils utils)
    {
        Utils = utils;
    }

    public void SimulateFirstHalf(Fixture fixture)
    {
        var homeClub = Utils.GetClub(fixture.HomeClub.ID);
        var awayClub = Utils.GetClub(fixture.AwayClub.ID);

        var homeClubRating = Utils.GetStartingElevenSumRatingForClub(homeClub);
        var awayClubRating = Utils.GetStartingElevenSumRatingForClub(awayClub);

        var max = homeClubRating + awayClubRating;
        var minute = 0;

        fixture.GoalsHome = 0;
        fixture.GoalsAway = 0;

        while (minute <= 45)
        {
            var randomNumber = Random.Next(0, max);
            minute += Random.Next(1, 12);
            if (randomNumber > homeClubRating && Random.Next(1,4) == 3)
            {
                fixture.GoalsHome += 1;
            }
            else if (randomNumber < homeClubRating && Random.Next(1,4) == 3)
            {
                fixture.GoalsAway += 1;
            }
        }
    }

    public void SimulateSecondHalf(Fixture fixture)
    {
        var homeClub = Utils.GetClub(fixture.HomeClub.ID);
        var awayClub = Utils.GetClub(fixture.AwayClub.ID);

        var homeClubRating = Utils.GetStartingElevenSumRatingForClub(homeClub);
        var awayClubRating = Utils.GetStartingElevenSumRatingForClub(awayClub);

        var max = homeClubRating + awayClubRating;
        var minute = 0;
        while (minute <= 45)
        {
            var randomNumber = Random.Next(0, max);
            minute += Random.Next(1, 12);
            if (randomNumber > homeClubRating && Random.Next(1, 4) == 3)
            {
                fixture.GoalsHome += 1;
            }
            else if (randomNumber < homeClubRating && Random.Next(1, 4) == 3)
            {
                fixture.GoalsAway += 1;
            }
        }
    }
}
