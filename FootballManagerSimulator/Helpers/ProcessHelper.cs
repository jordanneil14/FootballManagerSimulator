using FootballManagerSimulator.Enums;
using FootballManagerSimulator.Exceptions;
using FootballManagerSimulator.Interfaces;

namespace FootballManagerSimulator.Helpers;

public class ProcessHelper : IProcessHelper
{
    private readonly IState State;
    private readonly IWeatherHelper WeatherHelper;
    private readonly ITransferListHelper TransferListHelper;

    public ProcessHelper(
        IState state,
        IWeatherHelper weatherHelper,
        ITransferListHelper transferListHelper)
    {
        State = state;
        WeatherHelper = weatherHelper;
        TransferListHelper = transferListHelper;
    }

    public void Process()
    {
        try
        {
            ValidateProcess();
            State.Date = State.Date.AddDays(1);
            State.Weather = WeatherHelper.GetTodaysWeather();

            if (State.Date.DayOfWeek == DayOfWeek.Monday)
                TransferListHelper.UpdateTransferList();

        } catch (ProcessException ex)
        {
            State.ScreenStack.Push(new Structures.Screen
            {
                Type = ex.ScreenType
            });
        }
    }

    private void ValidateProcess()
    {
        var existsOutstandingFixtures = State.TodaysFixtures
            .SelectMany(p => p.Fixtures)
            .Any(p => !p.Concluded);

        if (existsOutstandingFixtures)
            throw new ProcessException(ScreenType.Fixture);
    }
}
