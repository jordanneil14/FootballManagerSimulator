using FootballManagerSimulator.Interfaces;

namespace FootballManagerSimulator.Events;

public class EventManager(IState state) : IEventManager
{
	private readonly IState State = state;

	public void ValidateAndAddEvent(IEvent gameEvent)
	{
		var (success, errorMessage) = gameEvent.ValidateAdd();
		if (success)
		{
            State.GameEvents.Add(gameEvent);
        }
		else
		{
			State.UserFeedbackUpdates.Add(errorMessage);
        }
	}

	public void ExecuteEvents()
	{
		var triggeredEvents = State.GameEvents
			.Where(e => e.TriggerDate <= State.Date && !e.IsCompleted)
			.ToList();

		foreach (var gameEvent in triggeredEvents)
		{
			gameEvent.Execute();
			gameEvent.IsCompleted = true;
		}
	}
}
