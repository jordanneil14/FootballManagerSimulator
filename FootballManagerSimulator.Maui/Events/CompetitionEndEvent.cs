using FootballManagerSimulator.Maui.Enums;
using FootballManagerSimulator.Maui.Interfaces;
using FootballManagerSimulator.Maui.Models;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace FootballManagerSimulator.Maui.Events;

// Runs the day after a competition's final match: announces who went up and down in a
// league, or who won a cup. Clubs only actually change league at the end of the season.
public class CompetitionEndEvent(
    IState state,
    INotificationFactory notificationFactory,
    IOptions<SettingsModel> settings,
    DateOnly triggerDate,
    bool isCompleted,
    int competitionId) : Event(triggerDate, isCompleted)
{
    private readonly IState State = state;
    private readonly INotificationFactory NotificationFactory = notificationFactory;
    private readonly SettingsModel Settings = settings.Value;
    public int CompetitionId { get; } = competitionId;

    public override void Execute()
    {
        var competition = State.Competitions.FirstOrDefault(p => p.Id == CompetitionId);

        if (competition is LeagueModel league)
            AnnouncePromotionAndRelegation(league);
        else if (competition != null)
            AnnounceCupWinner(competition);
    }

    private void AnnouncePromotionAndRelegation(LeagueModel league)
    {
        var countryLeagues = State.Competitions
            .OfType<LeagueModel>()
            .Where(p => p.CountryId == league.CountryId)
            .ToList();

        var leagueAbove = countryLeagues.Where(p => p.Rank < league.Rank).MaxBy(p => p.Rank);
        var leagueBelow = countryLeagues.Where(p => p.Rank > league.Rank).MinBy(p => p.Rank);

        var promoted = leagueAbove != null ? league.PromotedClubs() : [];
        var relegated = leagueBelow != null ? league.RelegatedClubs(leagueBelow) : [];

        if (promoted.Count == 0 && relegated.Count == 0)
            return;

        var sections = new List<string>();
        if (promoted.Count > 0)
            sections.Add($"Promoted:\n\t{string.Join("\n\t", promoted.Select(p => p.Name))}");
        if (relegated.Count > 0)
            sections.Add($"Relegated:\n\t{string.Join("\n\t", relegated.Select(p => p.Name))}");

        var leagueName = LeagueHeading(league);
        NotificationFactory.AddNotification(
            State.Date,
            "Press Officer",
            $"{leagueName} Promotion & Relegation",
            $"The {leagueName} season is over.\n{string.Join("\n", sections)}");

        if (leagueAbove != null && promoted.Any(p => p.Id == State.MyClubId))
            CongratulateOnPromotion(league, leagueAbove);
    }

    private void CongratulateOnPromotion(LeagueModel league, LeagueModel leagueAbove)
    {
        State.BoardConfidence = BoardConfidenceRules.Apply(State.BoardConfidence, BoardConfidenceRules.Promotion);

        var myClub = State.Clubs.First(p => p.Id == State.MyClubId);
        NotificationFactory.AddNotification(
            State.Date,
            "Chairman",
            "Promotion",
            $"Congratulations on winning promotion from {league.Name}! Everyone at {myClub.Name} is delighted, and we look forward to competing in {leagueAbove.Name} next season. The board's confidence in you has risen to {State.BoardConfidence}/100.");
    }

    private void AnnounceCupWinner(ICompetition cup)
    {
        var final = cup.Fixtures
            .Where(p => p.Concluded && p.ClubWon != null)
            .MaxBy(p => p.Round);

        if (final?.ClubWon is not { } winner)
            return;

        var runnerUp = winner.Id == final.HomeClub.Id ? final.AwayClub : final.HomeClub;
        NotificationFactory.AddNotification(
            State.Date,
            "Press Officer",
            $"{cup.Name} Winners",
            $"{winner.Name} have won the {cup.Name}, beating {runnerUp.Name} in the final.");
    }

    private string LeagueHeading(LeagueModel league)
    {
        var country = Settings.Countries.FirstOrDefault(p => p.Id == league.CountryId);
        return country != null ? $"{country.Name} - {league.Name}" : league.Name;
    }

    public override (bool success, string errorMessage) ValidateAdd()
    {
        return (true, "");
    }

    // Schedules the event for every competition whose last match is already known. Leagues with
    // play-offs are left out: their play-off final event schedules it once the final is arranged.
    public static void ScheduleForCompetitions(IState state, IEventManager eventManager, IServiceProvider serviceProvider)
    {
        foreach (var competition in state.Competitions)
        {
            if (competition.Type == CompetitionType.Friendly || competition is LeagueModel { HasPlayoffs: true })
                continue;

            var finalMatchDate = competition is LeagueModel
                ? competition.Fixtures.Select(p => (DateOnly?)p.Date).Max()
                : competition.DrawSettings.MaxBy(p => p.Round)?.FixtureDate;

            if (finalMatchDate is { } date)
                Schedule(eventManager, serviceProvider, date, competition.Id);
        }
    }

    public static void Schedule(IEventManager eventManager, IServiceProvider serviceProvider, DateOnly finalMatchDate, int competitionId)
    {
        var @event = ActivatorUtilities.CreateInstance<CompetitionEndEvent>(serviceProvider, finalMatchDate.AddDays(1), false, competitionId);
        eventManager.ValidateAndAddEvent(@event);
    }
}
