using FootballManagerSimulator.Enums;

namespace FootballManagerSimulator.Events;

public class FriendlyFixtureDrawEvent(DateOnly drawDate, DateOnly fixtureDate, DateOnly startDate, int round) : EventBase
{
    public override EventType Type => EventType.FriendlyDrawFixture;
	public override DateOnly CompletionDate => drawDate;
    public override DateOnly StartDate => startDate;

    public DateOnly FixtureDate { get; set; } = fixtureDate;
    public int Round { get; set; } = round;
}
