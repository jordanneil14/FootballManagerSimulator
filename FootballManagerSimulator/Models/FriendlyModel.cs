using FootballManagerSimulator.Enums;
using FootballManagerSimulator.Interfaces;

namespace FootballManagerSimulator.Models;

public class FriendlyModel : ICompetition
{
    public CompetitionType Type => CompetitionType.Friendly;
    public int? Round { get; set; }
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public List<FixtureModel> Fixtures { get; set; } = new List<FixtureModel>();
    public List<ClubModel> Clubs { get; set; } = new List<ClubModel>();
    public List<DrawSettingsModel> DrawSettings { get; set; } = new List<DrawSettingsModel>();
    public bool IsLeague => false;
}
