using FootballManagerSimulator.Enums;

namespace FootballManagerSimulator.Events;

public class RequestHigherTransferBudgetEvent(DateOnly requestedDate) : EventBase
{
    public override EventType Type => EventType.RequestHigherTransferBudget;
    public override DateOnly CompletionDate => requestedDate.AddDays(2);
    public override DateOnly StartDate => requestedDate;
}
