using FootballManagerSimulator.Enums;
using FootballManagerSimulator.Interfaces;
using FootballManagerSimulator.Models;
using Microsoft.Extensions.Options;

namespace FootballManagerSimulator.Factories;

public class FriendlyFactory(
    IOptions<Settings> settings,
    INotificationFactory notificationFactory,
    IState state) : ICompetitionFactory
{
    private readonly Settings Settings = settings.Value;

    public CompetitionType Type => CompetitionType.Friendly;

    public ICompetition CreateCompetition(CompetitionModel competition)
    {
        var friendy = new Friendly
        {
            Id = competition.Id,
            Name = competition.Name,
            Clubs = Settings.Clubs.Select(p => new Club
            {
                Id = p.Id,
                Name = p.Name,
                LeagueId = p.LeagueId
            }).ToList(),
            DrawDates = competition.DrawDates.Select(p => new DrawDateModel
            {
                Round = p.Round,
                FixtureDate = p.FixtureDate
            }).ToList()
        };

        return friendy;
    }

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
        notificationFactory.AddNotification(
            state.Date,
            "Club Analyst",
            "Pre-Match Report",
            "Friendly match incoming");
    }
}
