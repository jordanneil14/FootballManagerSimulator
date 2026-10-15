using FootballManagerSimulator.Maui.Enums;
using FootballManagerSimulator.Maui.Events;
using FootballManagerSimulator.Maui.Exceptions;
using FootballManagerSimulator.Maui.Interfaces;
using FootballManagerSimulator.Maui.Models;

namespace FootballManagerSimulator.Maui.Helpers;

public class ProcessHelper(
    IState state,
    IWeatherHelper weatherHelper,
    ITransferListHelper transferListHelper,
    IEnumerable<ICompetitionProvider> competitionFactories,
    IEventManager eventManager) : IProcessHelper
{
    private readonly IState State = state;
    private readonly IWeatherHelper WeatherHelper = weatherHelper;
    private readonly ITransferListHelper TransferListHelper = transferListHelper;
    private readonly IEnumerable<ICompetitionProvider> CompetitionFactories = competitionFactories;
    private readonly IEventManager EventManager = eventManager;

    // Advances the simulation by one day. If the user's club (or any club) has an
    // unplayed fixture scheduled for the current day, a ProcessException is thrown so
    // the presentation layer can route the user to today's fixtures first. The date is
    // only advanced once there are no outstanding fixtures for the current day.
    public void Process()
    {
        ValidateProcess();
        State.Date = State.Date.AddDays(1);
        State.Weather = WeatherHelper.GetTodaysWeather();

        if (State.Date.DayOfWeek == DayOfWeek.Monday)
            TransferListHelper.UpdateTransferList();

        TransferListHelper.ProcessAITransfers();

        foreach (var comp in State.Competitions)
        {
            var fixture = comp.Fixtures
                .Where(p => p.Date >= State.Date && (p.HomeClub.Id == State.Clubs.First(p => p.Id == State.MyClubId).Id || p.AwayClub.Id == State.Clubs.First(p => p.Id == State.MyClubId).Id))
                .OrderBy(p => p.Round)
                .FirstOrDefault();

            if (fixture != null && fixture.Date.DayNumber == State.Date.DayNumber + 1)
                CompetitionFactories.First(p => p.Type == comp.Type).CompetitionService.GeneratePreMatchReportForFixture(fixture);
        }

        EventManager.ExecuteEvents();
    }

    private void ValidateProcess()
    {
        var existsOutstandingFixtures = State.Competitions
            .SelectMany(p => p.Fixtures)
            .Any(p => !p.Concluded && p.Date == State.Date);

        if (existsOutstandingFixtures)
            throw new ProcessException(ScreenType.Fixture);
    }
}
