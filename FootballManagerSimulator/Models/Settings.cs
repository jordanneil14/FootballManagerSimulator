namespace FootballManagerSimulator.Structures;

public class Settings
{
    public GeneralModel General { get; set; } = new GeneralModel();
    public class GeneralModel
    {
        public string StartDate { get; set; } = "";
        public DateOnly StartDateAsDate => DateOnly.Parse(StartDate);
    }
    public List<ClubModel> Clubs { get; set; } = new List<ClubModel>();
    public class ClubModel
    {
        public int Id { get; set; }
        public string Name { get; set; } = "";
        public int TransferBudget { get; set; }
        public int WageBudget { get; set; }
        public string Stadium { get; set; } = "";
        public int LeagueId { get; set; }
    }
    public IEnumerable<CompetitionModel> Competitions { get; set; } = new List<CompetitionModel>();
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
}
