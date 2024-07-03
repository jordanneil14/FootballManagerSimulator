using FootballManagerSimulator.Enums;

namespace FootballManagerSimulator.Interfaces;

public interface IEventFactory
{
    EventType Type { get; }
    void CreateEvent();
    void CompleteEvent(IEvent @event);
    dynamic Data { get; set; }
}











