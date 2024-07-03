using FootballManagerSimulator.Enums;
using FootballManagerSimulator.Interfaces;

namespace FootballManagerSimulator.Events;


public class CupFixtureDrawEvent(IState state) : EventBase(state)
{
    public override EventType Type => EventType.CupDrawFixture;


    public override DateOnly CompletionDate => this.DrawDate;


    public DateOnly DrawDate { get; set; }
    public DateOnly FixtureDate { get; set; }
    public int Round { get; set; }
    public int CompetitionId { get; set; }
}
