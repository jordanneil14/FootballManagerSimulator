using FootballManagerSimulator.Enums;
using FootballManagerSimulator.Interfaces;

namespace FootballManagerSimulator.Events;

public class FriendlyFixtureDrawEvent(IState state) : EventBase(state)
{
    public override EventType Type => EventType.FriendlyDrawFixture;

    public override DateOnly CompletionDate => this.DrawDate;

    public DateOnly DrawDate { get; set; }
    public DateOnly FixtureDate { get; set; }
    public int Round { get; set; }
}
