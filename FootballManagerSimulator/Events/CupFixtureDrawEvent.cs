using FootballManagerSimulator.Enums;

namespace FootballManagerSimulator.Events;


public class CupFixtureDrawEvent(DateOnly drawDate, DateOnly fixtureDate, int competitionId, DateOnly createdDate, int round) : EventBase
{
    public override EventType Type => EventType.CupDrawFixture;
	public override DateOnly CompletionDate => drawDate;
    public override DateOnly StartDate => createdDate;

    public DateOnly FixtureDate { get; set; } = fixtureDate;
    public int Round { get; set; } = round;
    public int CompetitionId { get; set; } = competitionId;
}
