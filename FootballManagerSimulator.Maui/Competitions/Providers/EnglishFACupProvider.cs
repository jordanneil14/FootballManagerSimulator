using FootballManagerSimulator.Maui.Competitions.Services;
using FootballManagerSimulator.Maui.Enums;
using FootballManagerSimulator.Maui.Events;
using FootballManagerSimulator.Maui.Helpers;
using FootballManagerSimulator.Maui.Interfaces;
using FootballManagerSimulator.Maui.Models;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace FootballManagerSimulator.Maui.Competitions.Providers;

public class EnglishFACupProvider(
    IOptions<SettingsModel> settings,
    IState state,
    EnglishFACupService englishFACupService,
    IServiceProvider serviceProvider,
    IEventManager eventManager) : ICompetitionProvider
{
    private readonly SettingsModel Settings = settings.Value;
    private readonly IState State = state;
    private readonly EnglishFACupService EnglishFACupService = englishFACupService;
    private readonly IServiceProvider ServiceProvider = serviceProvider;
    private readonly IEventManager EventManager = eventManager;

    public ICompetitionService CompetitionService => EnglishFACupService;

    public CompetitionType Type => CompetitionType.EnglishFACup;

    public ICompetition CreateCompetition(CompetitionModel competition)
    {
        var englishLeagueIds = EnglishLeagueIds(_ => true);

        var cup = new CupModel
        {
            Id = competition.Id,
            Name = competition.Name,
            Type = CompetitionType.EnglishFACup,
            CountryId = competition.CountryId,
            Year = State.Date.Year,
            Clubs = State.Clubs.Where(p => englishLeagueIds.Contains(p.LeagueId)).ToList(),
            DrawSettings = []
        };

        // Must be created after the League Cup so its dates can be avoided.
        var leagueCupDates = State.Competitions
            .Where(p => p.Type == CompetitionType.Cup)
            .SelectMany(p => p.DrawSettings)
            .Select(p => p.FixtureDate)
            .ToHashSet();

        for (var round = 1; round <= FACupRules.Rounds; round++)
        {
            var fixtureDate = FACupRules.PlannedDate(round, cup.Year);
            while (leagueCupDates.Contains(fixtureDate))
                fixtureDate = fixtureDate.AddDays(7);

            var drawDate = round == 1
                ? fixtureDate.AddDays(-FACupRules.RoundOneDrawDaysBefore)
                : cup.DrawSettings[^1].FixtureDate.AddDays(1);

            cup.DrawSettings.Add(new DrawSettingsModel { Round = round, FixtureDate = fixtureDate, DrawDate = drawDate });

            var @event = ActivatorUtilities.CreateInstance<EnglishFACupFixtureDrawEvent>(ServiceProvider, drawDate, false);
            EventManager.ValidateAndAddEvent(@event);
        }

        return cup;
    }

    // Round 1 takes Divisions 3 and 4 plus non-league clubs drawn at random (always including mine);
    // Divisions 1 and 2 join in round 3. Decided at each draw from the clubs' leagues at the time.
    public void PopulateIntroducedClubs(CupModel cup)
    {
        var drawSettings = cup.DrawSettings.FirstOrDefault(p => p.Round == cup.Round);
        if (drawSettings == null)
            return;

        if (cup.Round == 1)
        {
            var byRight = ClubsInRanks(cup, FACupRules.RoundOneRanks);
            var nonLeague = ClubsInRanks(cup, FACupRules.NonLeagueRanks);
            var places = Math.Max(0, FACupRules.RoundOneClubs - byRight.Count);

            var chosen = nonLeague.Where(p => p.Id == State.MyClubId).ToList();
            chosen.AddRange(nonLeague
                .Where(p => p.Id != State.MyClubId)
                .OrderBy(_ => RandomNumberHelper.Next())
                .Take(places - chosen.Count));

            drawSettings.IntroducedClubIds = byRight.Concat(chosen).Select(p => p.Id).ToList();
        }
        else if (cup.Round == FACupRules.TopFlightEntryRound)
        {
            drawSettings.IntroducedClubIds = ClubsInRanks(cup, FACupRules.RoundThreeRanks).Select(p => p.Id).ToList();
        }
    }

    private List<ClubModel> ClubsInRanks(CupModel cup, int[] ranks)
    {
        var leagueIds = EnglishLeagueIds(p => ranks.Contains(p.Rank));
        return cup.Clubs.Where(p => leagueIds.Contains(p.LeagueId)).ToList();
    }

    private HashSet<int> EnglishLeagueIds(Func<CompetitionModel, bool> predicate) => Settings.Competitions
        .Where(p => p.IsLeague && p.CountryId == 1 && predicate(p))
        .Select(p => p.Id)
        .ToHashSet();
}
