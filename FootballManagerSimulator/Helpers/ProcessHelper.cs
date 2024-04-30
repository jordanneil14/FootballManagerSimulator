using FootballManagerSimulator.Enums;
using FootballManagerSimulator.Exceptions;
using FootballManagerSimulator.Interfaces;

namespace FootballManagerSimulator.Helpers;

public class ProcessHelper(
    IState state,
    IWeatherHelper weatherHelper,
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
