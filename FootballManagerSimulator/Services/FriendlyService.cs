using FootballManagerSimulator.Interfaces;
using FootballManagerSimulator.Models;

namespace FootballManagerSimulator.Services;

public class FriendlyService(
    INotificationFactory notificationFactory, 
    IState state) : ICompetitionService
{
    private readonly INotificationFactory NotificationFactory = notificationFactory;
    private readonly IState State = state;

    public void GenerateNextRoundOfFixtures(ICompetition competition)
    {
        var friendly = (FriendlyModel)competition;
        friendly.Round = friendly.Round.GetValueOrDefault() + 1;

        var drawDate = friendly.DrawSettings.First(p => p.Round == friendly.Round);

        var fixtures = new List<FixtureModel>();
        var randomlySortedClubs = competition.Clubs.OrderBy(p => Guid.NewGuid()).ToList();

        for (var i = 0; i < randomlySortedClubs.Count; i += 2)
        {
            fixtures.Add(new FixtureModel()
            {
                HomeClub = randomlySortedClubs.ElementAt(i),
                AwayClub = randomlySortedClubs.ElementAt(i + 1),
                Date = drawDate.FixtureDate,
                Round = drawDate.Round,
                KickOffTime = new TimeOnly(15, 00)
            });
        }

        competition.Fixtures.AddRange(fixtures);

        var myClubFixture = fixtures.First(p => p.HomeClub.Id == State.MyClubId || p.AwayClub.Id == State.MyClubId);
        if (myClubFixture != null)
        {
            var oppositionClubName = myClubFixture.HomeClub.Id == State.MyClubId ? myClubFixture.AwayClub.Name : myClubFixture.HomeClub.Name;
            var message = $"A friendly has been arranged against {oppositionClubName} on {myClubFixture.Date}";

            NotificationFactory.AddNotificationNow(
                "Chairman",
                "Friendly Arranged",
                message);
        }
    }

    public void GeneratePreMatchReportForFixture(FixtureModel fixture)
    {
        NotificationFactory.AddNotification(
            State.Date,
            "Club Analyst",
            "Pre-Match Report",
            "Friendly match incoming");
    }
}
