using FootballManagerSimulator.Enums;

namespace FootballManagerSimulator.Events;

public class StadiumExpansionEvent(DateOnly requestedDate) : EventBase
{
    public override EventType Type => EventType.StadiumExpansion;
    public override DateOnly CompletionDate => requestedDate.AddMonths(3);
	public override DateOnly StartDate => requestedDate;
}
