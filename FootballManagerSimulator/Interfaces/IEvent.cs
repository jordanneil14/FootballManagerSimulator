using FootballManagerSimulator.Enums;

namespace FootballManagerSimulator.Interfaces;

public interface IEvent
{
    EventType Event { get; }
    DateOnly StartDate { get; set; }
    DateOnly CompletionDate { get; }
    void Complete();
    void Start();
    void Initialise();
    decimal GetProgressPercentage();
}
