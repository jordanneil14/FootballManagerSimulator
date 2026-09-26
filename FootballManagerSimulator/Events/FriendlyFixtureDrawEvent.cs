using FootballManagerSimulator.Enums;
using FootballManagerSimulator.Interfaces;

namespace FootballManagerSimulator.Events;

public class FriendlyFixtureDrawEvent(DateOnly drawDate, DateOnly fixtureDate, DateOnly startDate, int round) : IEvent
{
	public EventType Type => EventType.FriendlyDrawFixture;
	public DateOnly CompletionDate => drawDate;
	public DateOnly StartDate => startDate;

	public DateOnly FixtureDate { get; set; } = fixtureDate;
	public int Round { get; set; } = round;
}
