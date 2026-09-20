using FootballManagerSimulator.Interfaces;
using FootballManagerSimulator.Models;

namespace FootballManagerSimulator.Services;

public class FriendlyService : ICompetitionService
{
    public void GenerateNextRoundOfFixtures(ICompetition competition)
    {
        var friendly = (Friendly)competition;
        friendly.Round = friendly.Round.GetValueOrDefault() + 1;

        var drawDate = friendly.DrawDates.First(p => p.Round == friendly.Round);

        var fixtures = new List<Fixture>();
        var randomlySortedClubs = competition.Clubs.OrderBy(p => Guid.NewGuid()).ToList();

        for (var i = 0; i < randomlySortedClubs.Count; i += 2)
        {
            fixtures.Add(new Fixture()
            {
                HomeClub = randomlySortedClubs.ElementAt(i),
                AwayClub = randomlySortedClubs.ElementAt(i + 1),
                Date = drawDate.FixtureDate,
                Round = drawDate.Round,
                KickOffTime = new TimeOnly(15, 00)
            });
        }

        competition.Fixtures.AddRange(fixtures);
    }

    public void GeneratePreMatchReportForFixture(Fixture fixture)
    {
        //NotificationFactory.AddNotification(
        //    State.Date,
        //    "Club Analyst",
        //    "Pre-Match Report",
        //    "Friendly match incoming");
    }
}
