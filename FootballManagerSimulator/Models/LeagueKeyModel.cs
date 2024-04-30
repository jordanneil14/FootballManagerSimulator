using FootballManagerSimulator.Structures;

namespace FootballManagerSimulator.Models;

public class LeagueKeyModel
{
    public char Key { get; set; }
    public League League { get; set; } = new League();
    public bool IsCurrent { get; set; }
}