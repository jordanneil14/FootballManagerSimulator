using FootballManagerSimulator.Interfaces;

namespace FootballManagerSimulator.GameEvent;

public class GameEventManager(IState state)
{
	private readonly IState State = state;

	public void AddEvent(IGameEvent gameEvent)
	{
		State.GameEvents.Add(gameEvent);
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
