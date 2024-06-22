using FootballManagerSimulator.Models;

namespace FootballManagerSimulator.Structures;

public class Settings
{
    public GeneralModel General { get; set; } = new GeneralModel();
    public class GeneralModel
    {
        public string StartDate { get; set; } = "";
        public DateOnly StartDateAsDate => DateOnly.Parse(StartDate);
    }
    public List<Club> Clubs { get; set; } = new List<Club>();
    public IEnumerable<CompetitionModel> Competitions { get; set; } = new List<CompetitionModel>();
    
}
