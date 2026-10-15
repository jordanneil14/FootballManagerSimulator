namespace FootballManagerSimulator.Maui.Interfaces;

public interface IEventManager
{
    void ValidateAndAddEvent(IEvent @event);
    void ExecuteEvents();
}
