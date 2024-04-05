namespace FootballManagerSimulator.Structures;

public class Settings
{
    public GeneralModel General { get; set; } = new GeneralModel();
    public class GeneralModel
    {
        public string StartDate { get; set; }
        public DateOnly StartDateAsDate => DateOnly.Parse(StartDate);
    }
    public List<ClubModel> Clubs { get; set; } = new List<ClubModel>();
    public class ClubModel
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public int TransferBudget { get; set; }
        public int WageBudget { get; set; }
        public string Stadium { get; set; }
        public int LeagueId { get; set; }
    }
    public IEnumerable<LeagueModel> Leagues { get; set; } = new List<LeagueModel>();
    public class LeagueModel
    {
        public int Id { get; set; }
        public string Name { get; set; }
    }
}
