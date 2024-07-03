using FootballManagerSimulator.Enums;
using FootballManagerSimulator.Interfaces;

namespace FootballManagerSimulator.Events;

public class StadiumExpansionEvent(IState state) : EventBase(state)
{
    public override EventType Type => EventType.StadiumExpansion;
    private readonly DateOnly completionDate = state.Date.AddMonths(3);

    public override DateOnly CompletionDate => completionDate;
}
