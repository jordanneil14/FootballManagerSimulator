using FootballManagerSimulator.Maui.Enums;

namespace FootballManagerSimulator.Maui.Exceptions;

public class ProcessException(
    ScreenType screenType) : Exception()
{
    public ScreenType ScreenType { get { return screenType; } }
}