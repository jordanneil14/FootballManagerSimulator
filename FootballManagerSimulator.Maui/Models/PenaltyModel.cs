using FootballManagerSimulator.Maui.Enums;

namespace FootballManagerSimulator.Maui.Models;

// A penalty awarded during play. A scored one is also in the side's scorers, as a goal with IsPenalty set.
public class PenaltyModel
{
    public int Minute { get; set; }
    public int TakerId { get; set; }
    // Null when the defending side had no goalkeeper on the pitch.
    public int? KeeperId { get; set; }
    public PenaltyOutcome Outcome { get; set; }
}
