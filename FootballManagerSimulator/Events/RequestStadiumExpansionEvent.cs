using FootballManagerSimulator.Enums;
using FootballManagerSimulator.Interfaces;

namespace FootballManagerSimulator.Events;

public class RequestStadiumExpansionEvent(DateOnly requestedDate) : IEvent
{
    public EventType Type => EventType.RequestStadiumExpansion;
    public DateOnly CompletionDate => requestedDate.AddDays(2);
	public DateOnly StartDate => requestedDate;
}
