namespace FootballManagerSimulator.Models;

public class LeagueKeyModel
{
    public char Key { get; set; }
    public LeagueModel League { get; set; } = new LeagueModel();
    public bool IsCurrent { get; set; }
}