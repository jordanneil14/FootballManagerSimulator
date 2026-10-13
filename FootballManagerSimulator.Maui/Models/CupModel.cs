using FootballManagerSimulator.Enums;
using FootballManagerSimulator.Interfaces;

namespace FootballManagerSimulator.Models;

public class CupModel : ICompetition
{
    public CompetitionType Type => CompetitionType.Cup;
    public int Round { get; set; } = 1;
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public List<FixtureModel> Fixtures { get; set; } = new List<FixtureModel>();
    public List<ClubModel> Clubs { get; set; } = new List<ClubModel>();
    public List<DrawSettingsModel> DrawSettings { get; set; } = [];
    public int CountryId { get; set; }
	public bool IsLeague => false;
}
