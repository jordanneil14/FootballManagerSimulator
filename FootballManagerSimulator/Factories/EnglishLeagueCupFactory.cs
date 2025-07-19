using FootballManagerSimulator.Enums;
using FootballManagerSimulator.Interfaces;
using FootballManagerSimulator.Models;
using Microsoft.Extensions.Options;

namespace FootballManagerSimulator.Factories;

public class EnglishLeagueCupFactory(
    IOptions<Settings> settings,
    INotificationFactory notificationFactory,
    IState state) : ICompetitionFactory
{
    private readonly Settings Settings = settings.Value;

    private readonly List<string> LeaguesInvolved = [ "Premier League", "EFL Championship", "EFL League One", "EFL League Two" ];
    private readonly List<string> RoundOneLeaguesInvolved = [ "EFL Championship", "EFL League One", "EFL League One" ];

    public CompetitionType Type => CompetitionType.Cup;

    public ICompetition CreateCompetition(CompetitionModel competition)
    {
        var leagues = Settings.Competitions.Where(p => LeaguesInvolved.Contains(p.Name));
        var leagueIds = leagues.Select(p => p.Id);

        var clubs = Settings.Clubs
            .Where(p => leagueIds.Contains(p.LeagueId));

        var cup = new Cup
        {
            Id = competition.Id,
            Name = competition.Name,
            Clubs = clubs,
            DrawDates = competition.DrawDates.Select(p => new DrawDateModel
            {
                Round = p.Round,
                DrawDate = p.DrawDate,
                FixtureDate = p.FixtureDate,
                IntroducedClubIds = p.IncludedClubs
            }).ToList()
        };

        return cup;
    }

    public void GenerateNextRoundOfFixtures(ICompetition competition)
    {
        var cup = (Cup)competition;

        var drawDate = cup.DrawDates.First(p => p.Round == cup.Round);

        if (cup.Round == 1)
        {
            var leagues = Settings.Competitions.Where(p => RoundOneLeaguesInvolved.Contains(p.Name));
            var leagueIds = leagues.Select(p => p.Id);

            var leagueIdsInvolved = Settings.Competitions
                .Where(p => p.CountryId == cup.CountryId && leagueIds.Contains(p.Rank) && p.Type == "League")
                .Select(p => p.Id);

            var clubs = cup.Clubs.Where(p => leagueIdsInvolved.Contains(p.LeagueId));

            cup.Fixtures = GenerateFixtures(clubs, drawDate.FixtureDate, cup.Round);
            return;
        }

        IEnumerable<Fixture> lastRoundOfFixtures = cup.Fixtures.Where(p => p.Round == cup.Round - 1);

        var winningClubs = lastRoundOfFixtures.Select(p => p.ClubWon).Cast<Club>();
        var includedClubs = drawDate.IntroducedClubIds == null ? [] : cup.Clubs.Where(p => drawDate.IntroducedClubIds.Contains(p.Id));
        var nextRoundClubs = winningClubs.Concat(includedClubs);

        cup.Fixtures.AddRange(GenerateFixtures(nextRoundClubs, drawDate.FixtureDate, cup.Round));
    }

    private List<Fixture> GenerateFixtures(IEnumerable<Club> clubs, DateOnly date, int round)
    {
        var fixtures = new List<Fixture>();
        for (var i = 0; i < clubs.Count(); i += 2)
        {
            fixtures.Add(new Fixture()
            {
                HomeClub = clubs.ElementAt(i),
                AwayClub = clubs.ElementAt(i + 1),
                Date = date,
                Round = round,
                KickOffTime = new TimeOnly(19, 45)
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
