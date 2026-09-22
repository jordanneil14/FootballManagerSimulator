namespace FootballManagerSimulator.Models;
public class CompetitionModel
{
    public string Type { get; set; } = "";
    public int Id { get; set; }
    public int CountryId { get; set; }
    public string Name { get; set; } = "";
    public int Rank { get; set; }
    public bool IsLeague { get; set; }
    public LeagueTableModel LeagueTable { get; set; } = new LeagueTableModel();
    public class LeagueTableModel
    {
        public int AutomaticPromotionPlaces { get; set; }
        public int PlayoffPlaces { get; set; }
        public int RelegationPlaces { get; set; }
        public int Places { get; set; }
    }

    public IEnumerable<DrawSettingsModel> DrawSettings { get; set; } = [];

    public class DrawSettingsModel
    {
        public int Round { get; set; }
        public DateOnly DrawDate { get; set; }
        public DateOnly FixtureDate { get; set; }
        public List<int> IntroducedClubIds { get; set; } = new List<int>();
    }
}
