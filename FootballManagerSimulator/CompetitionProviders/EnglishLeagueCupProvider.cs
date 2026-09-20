using FootballManagerSimulator.Enums;
using FootballManagerSimulator.Factories;
using FootballManagerSimulator.Interfaces;
using FootballManagerSimulator.Models;
using FootballManagerSimulator.Services;
using Microsoft.Extensions.Options;

namespace FootballManagerSimulator.CompetitionProviders;

public class EnglishLeagueCupProvider(
    IOptions<Settings> settings,
    IState state,
    CupFixtureDrawFactory cupFixtureDrawFactory,
    EnglishLeagueCupService englishLeagueCupService) : ICompetitionProvider
{
    private readonly Settings Settings = settings.Value;
    private readonly EnglishLeagueCupService EnglishLeagueCupService = englishLeagueCupService;
    public ICompetitionService CompetitionService => EnglishLeagueCupService;
    private readonly IState State = state;
    private readonly CupFixtureDrawFactory CupFixtureDrawFactory = cupFixtureDrawFactory;

    private readonly IEnumerable<string> LeaguesInvolved = [ "Premier League", "EFL Championship", "EFL League One", "EFL League Two" ];

    public CompetitionType Type => CompetitionType.Cup;

    public ICompetition CreateCompetition(CompetitionModel competition)
    {
        var leagues = Settings.Competitions.Where(p => LeaguesInvolved.Contains(p.Name));
        var leagueIds = leagues.Select(p => p.Id);

        var clubs = Settings.Clubs
            .Where(p => leagueIds.Contains(p.LeagueId))
            .ToList();

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

        foreach (var drawDate in cup.DrawDates)
        {
            var cupDrawFixtureEvent = CupFixtureDrawFactory;
            cupDrawFixtureEvent.Data.DrawDate = new DateTime(drawDate.DrawDate.Year, drawDate.DrawDate.Month, drawDate.DrawDate.Day);
            cupDrawFixtureEvent.Data.FixtureDate = new DateTime(drawDate.FixtureDate.Year, drawDate.FixtureDate.Month, drawDate.FixtureDate.Day);
            cupDrawFixtureEvent.Data.Round = drawDate.Round;
            cupDrawFixtureEvent.Data.CompetitionId = competition.Id;
            cupDrawFixtureEvent.CreateEvent();
        }

        return cup;
    }
}
