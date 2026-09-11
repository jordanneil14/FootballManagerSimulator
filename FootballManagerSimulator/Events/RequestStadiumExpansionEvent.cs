using FootballManagerSimulator.Enums;

namespace FootballManagerSimulator.Events;

public class RequestStadiumExpansionEvent(DateOnly requestedDate) : EventBase
{
    public override EventType Type => EventType.RequestStadiumExpansion;
    public override DateOnly CompletionDate => requestedDate.AddDays(2);
	public override DateOnly StartDate => requestedDate;
}
