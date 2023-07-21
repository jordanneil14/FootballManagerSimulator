using FootballManagerSimulator.Enums;

namespace FootballManagerSimulator.Interfaces;

public interface IBaseScreen
{
    ScreenType Screen { get; }
    void RenderScreen();
    void HandleInput(string input);
}
