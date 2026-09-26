using FootballManagerSimulator.Interfaces;

namespace FootballManagerSimulator.Events;

public abstract class Event(DateOnly triggerDate) : IEvent
{
	public DateOnly TriggerDate { get; } = triggerDate;
	public bool IsCompleted { get; set; }

	public abstract void Execute();

	public abstract (bool success, string errorMessage) ValidateAdd();
}
