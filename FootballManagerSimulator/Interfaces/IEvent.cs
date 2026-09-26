namespace FootballManagerSimulator.Interfaces;

public interface IEvent
{
    DateOnly TriggerDate { get; }
    bool IsCompleted { get; set; }
    void Execute();
    (bool success, string errorMessage) ValidateAdd();
}
