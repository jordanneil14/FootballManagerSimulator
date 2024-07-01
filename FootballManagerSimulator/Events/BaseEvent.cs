using FootballManagerSimulator.Enums;
using FootballManagerSimulator.Interfaces;

namespace FootballManagerSimulator.Events;

public abstract class BaseEvent(IState state) : IEvent
{
    public DateOnly StartDate { get; set; }
    public abstract DateOnly CompletionDate { get; }
    public abstract EventType Event { get; }
    public abstract void Complete();

    public decimal GetProgressPercentage()
    {
        var lengthInDays = CompletionDate.DayNumber - StartDate.DayNumber;
        var daysRemaining = CompletionDate.DayNumber - state.Date.DayNumber;
        var percentage = ((decimal)(lengthInDays - daysRemaining) / (decimal)lengthInDays) * 100;
        return Math.Min(100, percentage);
    }

    public void Initialise()
    {
        StartDate = state.Date;
        state.Events.Add(this);
    }

    public abstract void Start();
}
