using FootballManagerSimulator.Enums;
using FootballManagerSimulator.Interfaces;

namespace FootballManagerSimulator.Events;

public class StadiumExpansionEvent(DateOnly requestedDate) : IEvent
{
    public EventType Type => EventType.StadiumExpansion;
    public DateOnly CompletionDate => requestedDate.AddMonths(3);
	public DateOnly StartDate => requestedDate;
}
