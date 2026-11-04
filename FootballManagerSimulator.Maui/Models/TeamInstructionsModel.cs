namespace FootballManagerSimulator.Maui.Models;

// The neutral option of each is 0, so a club or save with no instructions plays as before.
public enum Mentality
{
    Balanced = 0,
    Defensive = 1,
    Attacking = 2
}

public enum Pressing
{
    Medium = 0,
    Low = 1,
    High = 2
}

// How a club sets up to play, on top of its formation and line-up (see TacticalModifierRules).
public class TeamInstructionsModel
{
    public Mentality Mentality { get; set; } = Mentality.Balanced;
    public Pressing Pressing { get; set; } = Pressing.Medium;

    public TeamInstructionsModel Clone() => new() { Mentality = Mentality, Pressing = Pressing };
}
