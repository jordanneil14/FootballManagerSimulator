using FootballManagerSimulator.Enums;
using FootballManagerSimulator.Interfaces;

namespace FootballManagerSimulator.Events;

public abstract class EventBase(IState state) : IEvent
{
    public abstract EventType Type { get; }

    public abstract DateOnly CompletionDate { get;}

    private readonly DateOnly startDate = state.Date;
    public DateOnly StartDate => startDate;
}
