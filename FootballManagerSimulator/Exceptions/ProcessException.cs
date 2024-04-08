using FootballManagerSimulator.Enums;

namespace FootballManagerSimulator.Exceptions;

public class ProcessException : Exception
{
    private ScreenType _screenType;
    public ScreenType ScreenType { get { return _screenType; } }

    public ProcessException(
        ScreenType screenType) : base()
    {
        _screenType = screenType;
    }
}