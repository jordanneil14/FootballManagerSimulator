namespace FootballManagerSimulator.Models;

public class DrawSettingsModel
{
    public int Round { get; set; }
    public DateOnly DrawDate { get; set; }
    public DateOnly FixtureDate { get; set; }
    public List<int> IntroducedClubIds { get; set; } = new List<int>();
}
