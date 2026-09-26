namespace FootballManagerSimulator.GameEvent;

public interface IGameEvent
{
	DateOnly TriggerDate { get; }
	bool IsCompleted { get; set; }
	void Execute();
}

public abstract class GameEvent(DateOnly triggerDate) : IGameEvent
{
	public DateOnly TriggerDate { get; } = triggerDate;
	public bool IsCompleted { get; set; }

	public abstract void Execute();
}
