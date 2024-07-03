using FootballManagerSimulator.Enums;
using FootballManagerSimulator.Interfaces;

namespace FootballManagerSimulator.Events;

public class RequestHigherTransferBudgetEvent(IState state) : EventBase(state)
{
    public override EventType Type => EventType.RequestHigherTransferBudget;

    public override DateOnly CompletionDate { get; }

}
