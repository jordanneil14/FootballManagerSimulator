using FootballManagerSimulator.Enums;

namespace FootballManagerSimulator.Events;

public class CupFixtureDrawEvent(DateOnly completionDate, DateOnly fixtureDate, int competitionId, DateOnly startDate, int round) : EventBase
{
    public override EventType Type => EventType.CupDrawFixture;
	public override DateOnly CompletionDate => completionDate;
    public override DateOnly StartDate => startDate;

    public DateOnly FixtureDate { get; set; } = fixtureDate;
    public int Round { get; set; } = round;
    public int CompetitionId { get; set; } = competitionId;
}
