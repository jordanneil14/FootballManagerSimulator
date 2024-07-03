using FootballManagerSimulator.Enums;
using FootballManagerSimulator.Interfaces;

namespace FootballManagerSimulator.Events;

public class RequestHigherTransferBudgetEvent(IState state) : EventBase(state)
{
    public override EventType Type => EventType.RequestHigherTransferBudget;

    private readonly DateOnly completionDate = state.Date.AddDays(2);
    public override DateOnly CompletionDate => completionDate;

}
