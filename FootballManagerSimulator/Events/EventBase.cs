using FootballManagerSimulator.Enums;
using FootballManagerSimulator.Interfaces;

namespace FootballManagerSimulator.Events;

public abstract class EventBase : IEvent
{
    public abstract EventType Type { get; }

    public abstract DateOnly CompletionDate { get; }

    public abstract DateOnly StartDate { get; }
}
