namespace FootballManagerSimulator.Models;
public class CompetitionModel
{
    public string Type { get; set; } = "";
    public int Id { get; set; }
    public string Name { get; set; } = "";
    public int Rank { get; set; }
    public LeagueTableModel LeagueTable { get; set; } = new LeagueTableModel();
    public class LeagueTableModel
    {
        public int AutomaticPromotionPlaces { get; set; }
        public int PlayoffPlaces { get; set; }
        public int RelegationPlaces { get; set; }
        public int Places { get; set; }
    }

    public IEnumerable<DrawDateModel> DrawDates { get; set; } = new List<DrawDateModel>();

    public class DrawDateModel
    {
        public int Round { get; set; }
        public DateOnly DrawDate { get; set; }
        public DateOnly FixtureDate { get; set; }
        public List<int> IncludedClubs { get; set; } = new List<int>();
    }
}
