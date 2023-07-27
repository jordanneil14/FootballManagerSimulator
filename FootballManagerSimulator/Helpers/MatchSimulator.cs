using FootballManagerSimulator.Interfaces;
using FootballManagerSimulator.Structures;
using System;
namespace FootballManagerSimulator.Helpers;

public class MatchSimulator : IMatchSimulator
{
    private static readonly Random Random = new();

    public void SimulateFirstHalf(Fixture fixture)
    {
        var homeTeamRating = fixture.HomeTeam.FirstTeamTotalRating;
        var awayTeamRating = fixture.AwayTeam.FirstTeamTotalRating;

        var max = homeTeamRating + awayTeamRating;
        var minute = 0;

        fixture.GoalsHome = 0;
        fixture.GoalsAway = 0;
        while (minute <= 45)
        {
            var randomNumber = Random.Next(0, max);
            minute += Random.Next(1, 12);
            if (randomNumber > homeTeamRating && Random.Next(1,4) == 3)
            {
                fixture.GoalsHome += 1;
            }
            else if (randomNumber < homeTeamRating && Random.Next(1,4) == 3)
            {
                fixture.GoalsAway += 1;
            }
        }
    }

    public void SimulateSecondHalf(Fixture fixture)
    {
        var homeTeamRating = fixture.HomeTeam.FirstTeamTotalRating;
        var awayTeamRating = fixture.AwayTeam.FirstTeamTotalRating;

        var max = homeTeamRating + awayTeamRating;
        var minute = 0;
        while (minute <= 45)
        {
            var randomNumber = Random.Next(0, max);
            minute += Random.Next(1, 12);
            if (randomNumber > homeTeamRating && Random.Next(1, 4) == 3)
            {
                fixture.GoalsHome += 1;
            }
            else if (randomNumber < homeTeamRating && Random.Next(1, 4) == 3)
            {
                fixture.GoalsAway += 1;
            }
        }
    }
}
