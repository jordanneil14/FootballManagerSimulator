using FootballManagerSimulator.Enums;
using FootballManagerSimulator.Exceptions;
using FootballManagerSimulator.Interfaces;
using FootballManagerSimulator.Models;

namespace FootballManagerSimulator.Helpers;

public class ProcessHelper(
    IState state,
    IWeatherHelper weatherHelper,
    ITransferListHelper transferListHelper,
    IEnumerable<ICompetitionFactory> competitionFactories,
    IEnumerable<IEventFactory> eventFactories) : IProcessHelper
{
    private readonly IState State = state;
    private readonly IWeatherHelper WeatherHelper = weatherHelper;
    private readonly ITransferListHelper TransferListHelper = transferListHelper;
    private readonly IEnumerable<ICompetitionFactory> CompetitionFactories = competitionFactories;
    private readonly IEnumerable<IEventFactory> EventFactories = eventFactories;

    public void Process()
    {
        try
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
                    CompetitionFactories.First(p => p.Type == comp.Type).GeneratePreMatchReportForFixture(fixture);
            }

            var completedEvents = State.Events.Where(p => p.CompletionDate == State.Date).ToList();
            foreach (var completedEvent in completedEvents)
            {
                var eventFactory = EventFactories.First(p => p.Type == completedEvent.Type);
                eventFactory.CompleteEvent(completedEvent);
            }
        }
        catch (ProcessException ex)
        {
            State.ScreenStack.Push(new Screen
            {
                Type = ex.ScreenType
            });
        }
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
