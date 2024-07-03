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
    public void Process()
    {
        try
        {
            ValidateProcess();
            state.Date = state.Date.AddDays(1);
            state.Weather = weatherHelper.GetTodaysWeather();

            if (state.Date.DayOfWeek == DayOfWeek.Monday)
                transferListHelper.UpdateTransferList();

            transferListHelper.ProcessAITransfers();

            foreach (var comp in state.Competitions)
            {
                var fixture = comp.Fixtures
                    .Where(p => p.Date >= state.Date && (p.HomeClub.Id == state.Clubs.First(p => p.Id == state.MyClubId).Id || p.AwayClub.Id == state.Clubs.First(p => p.Id == state.MyClubId).Id))
                    .OrderBy(p => p.Round)
                    .FirstOrDefault();

                if (fixture != null && fixture.Date.DayNumber == state.Date.DayNumber + 1)
                    competitionFactories.First(p => p.Type == comp.Type).GeneratePreMatchReportForFixture(fixture);
            }

            var completedEvents = state.Events.Where(p => p.CompletionDate == state.Date).ToList();
            foreach(var completedEvent in completedEvents)
            {
                var eventFactory = eventFactories.First(p => p.Type == completedEvent.Type);
                eventFactory.CompleteEvent(completedEvent);
            }
        }
        catch (ProcessException ex)
        {
            state.ScreenStack.Push(new Screen
            {
                Type = ex.ScreenType
            });
        }
    }

    private void ValidateProcess()
    {
        var existsOutstandingFixtures = state.Competitions
            .SelectMany(p => p.Fixtures)
            .Any(p => !p.Concluded && p.Date == state.Date);

        if (existsOutstandingFixtures)
            throw new ProcessException(ScreenType.Fixture);
    }
}
