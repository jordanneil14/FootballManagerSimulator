using FootballManagerSimulator.Enums;
using FootballManagerSimulator.Interfaces;

namespace FootballManagerSimulator.Events;

public class RequestHigherTransferBudgetEvent(DateOnly requestedDate) : IEvent
{
    public EventType Type => EventType.RequestHigherTransferBudget;
    public DateOnly CompletionDate => requestedDate.AddDays(2);
    public DateOnly StartDate => requestedDate;
}
