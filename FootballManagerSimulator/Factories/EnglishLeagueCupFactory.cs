using FootballManagerSimulator.Interfaces;
using FootballManagerSimulator.Models;
using FootballManagerSimulator.Structures;
using Microsoft.Extensions.Options;

namespace FootballManagerSimulator.Factories;

public class EnglishLeagueCupFactory(
    IOptions<Settings> settings,
    INotificationFactory notificationFactory,
    IState state) : ICompetitionFactory
{
    private readonly Settings Settings = settings.Value;

    public string Type => "Cup";

    public ICompetition CreateCompetition(Settings.CompetitionModel competition)
    {
        var clubs = Settings.Clubs
            .Where(p => new List<int> { 1, 2, 3, 4 }.Contains(p.LeagueId))
            .Select(p => new Club
            {
                Id = p.Id,
                Name = p.Name,
                LeagueId = p.LeagueId
            });

        var cup = new Cup
        {
            Id = competition.Id,
            Name = competition.Name,
            Clubs = clubs.ToList(),
            DrawDates = competition.DrawDates.Select(p => new DrawDateModel
            {
                Round = p.Round,
                DrawDate = p.DrawDate,
                FixtureDate = p.FixtureDate,
                IncludedClubs = p.IncludedClubs
            }).ToList()
        };

        return cup;
    }

    public void GenerateNextRoundOfFixtures(ICompetition competition, DateOnly fixtureDate)
    {
        var cup = (Cup)competition;

        var s = cup.DrawDates.First(p => p.FixtureDate == fixtureDate);

        cup.Round = cup.Round.GetValueOrDefault() + 1;
        if (cup.Round == 1)
        {
            var clubs = cup.Clubs
                .Where(p => new List<int> { 2, 3, 4 }.Contains(p.LeagueId))
                .Select(p => new Club()
                {
                    Id = p.Id,
                    Name = p.Name
                }).ToList();
            cup.Fixtures = GenerateFixtures(clubs.ToList(), fixtureDate, cup.Round.Value);
            return;
        }

        var lastRoundOfFixtures = cup.Fixtures.Where(p => p.Round == cup.Round - 1);

        var winningClubs = lastRoundOfFixtures.Select(p => p.ClubWon);
        var includedClubs = s.IncludedClubs == null ? Enumerable.Empty<Club>() : cup.Clubs.Where(p => s.IncludedClubs.Contains(p.Id));
        var nextRoundClubs = winningClubs.Concat(includedClubs);

        cup.Fixtures.AddRange(GenerateFixtures(nextRoundClubs.ToList(), fixtureDate, cup.Round.Value));
    }

    private List<Fixture> GenerateFixtures(List<Club> clubs, DateOnly date, int round)
    {
        var fixtures = new List<Fixture>();
        for (int i = 0; i < clubs.Count; i += 2)
        {
            fixtures.Add(new Fixture()
            {
                HomeClub = clubs.ElementAt(i),
                AwayClub = clubs.ElementAt(i + 1),
                Date = date,
                Round = round
            });
        }
        return fixtures;
    }

    public void GeneratePreMatchReportForFixture(Fixture fixture)
    {
        notificationFactory.AddNotification(
                   state.Date,
                   "Club Analyst",
                   "Pre-Match Report",
                   "English league Cup match incoming");
    }
}
