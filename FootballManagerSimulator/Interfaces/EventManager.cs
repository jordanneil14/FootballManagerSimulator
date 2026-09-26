namespace FootballManagerSimulator.Interfaces;

public interface IEventManager
{
    void ValidateAndAddEvent(IEvent gameEvent);
    void ExecuteEvents();
}
