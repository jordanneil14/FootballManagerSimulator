using FootballManagerSimulator.Enums;
using FootballManagerSimulator.Exceptions;
using FootballManagerSimulator.Interfaces;

namespace FootballManagerSimulator.Helpers;

public class ProcessHelper(
    IState state,
    IWeatherHelper weatherHelper,
    INotificationHelper notificationHelper,
    ITransferListHelper transferListHelper) : IProcessHelper
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

            var nextFixture = state.Competitions.SelectMany(p => p.Fixtures)
                .Where(p => p.Date >= state.Date && (p.HomeClub.Id == state.MyClub.Id || p.AwayClub.Id == state.MyClub.Id))
                .OrderBy(p => p.Round)
                .FirstOrDefault();

            if (nextFixture != null && nextFixture.Date.DayNumber == state.Date.DayNumber + 1)
                notificationHelper.GeneratePreMatchReportForFixture(nextFixture);

        } catch (ProcessException ex)
        {
            state.ScreenStack.Push(new Structures.Screen
            {
                Type = ex.ScreenType
            });
        }
    }

    private void ValidateProcess()
    {
        var existsOutstandingFixtures = state.TodaysFixtures
            .SelectMany(p => p.Fixtures)
            .Any(p => !p.Concluded);

        if (existsOutstandingFixtures)
            throw new ProcessException(ScreenType.Fixture);
    }
}
