using FootballManagerSimulator.Interfaces;

namespace FootballManagerSimulator.Events;

public class EventManager(IState state) : IEventManager
{
	private readonly IState State = state;

	public void ValidateAndAddEvent(IEvent @event)
	{
		var (success, errorMessage) = @event.ValidateAdd();
		if (success)
		{
            State.Events.Add(@event);
        }
		else
		{
			State.UserFeedbackUpdates.Add(errorMessage);
        }
	}

	public void ExecuteEvents()
	{
		var triggeredEvents = State.Events
			.Where(e => e.TriggerDate <= State.Date && !e.IsCompleted)
			.ToList();

		foreach (var @event in triggeredEvents)
		{
			@event.Execute();
			@event.IsCompleted = true;
		}
	}
}
