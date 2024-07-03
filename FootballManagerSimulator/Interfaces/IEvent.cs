using FootballManagerSimulator.Enums;

namespace FootballManagerSimulator.Interfaces;

public interface IEvent
{
    EventType Type { get; }
    DateOnly CompletionDate { get; }
    DateOnly StartDate { get; }
}
