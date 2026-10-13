using FootballManagerSimulator.Enums;

namespace FootballManagerSimulator.Exceptions;

public class ProcessException(
    ScreenType screenType) : Exception()
{
    public ScreenType ScreenType { get { return screenType; } }
}