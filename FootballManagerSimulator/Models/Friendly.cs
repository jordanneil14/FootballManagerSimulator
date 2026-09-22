using FootballManagerSimulator.Enums;
using FootballManagerSimulator.Interfaces;

namespace FootballManagerSimulator.Models;

public class Friendly : ICompetition
{
    public CompetitionType Type => CompetitionType.Friendly;
    public int? Round { get; set; }
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public List<Fixture> Fixtures { get; set; } = new List<Fixture>();
    public List<Club> Clubs { get; set; } = new List<Club>();
    public List<DrawSettingsModel> DrawSettings { get; set; } = new List<DrawSettingsModel>();
    public bool IsLeague => false;
}
