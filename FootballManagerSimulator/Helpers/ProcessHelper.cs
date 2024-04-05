using FootballManagerSimulator.Enums;
using FootballManagerSimulator.Interfaces;

namespace FootballManagerSimulator.Helpers;

public class ProcessHelper : IProcessHelper
{
    private readonly IState State;

    public ProcessHelper(
        IState state)
    {
        State = state;
    }

    public void Process()
    {
        try
        {
            ValidateProcess();
            State.Date = State.Date.AddDays(1);
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

public class ProcessException : Exception
{
    private ScreenType _screenType;
    public ScreenType ScreenType { get { return _screenType; } }

    public ProcessException(ScreenType screenType) : base()
    {
        _screenType = screenType;
    }
}
