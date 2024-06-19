using FootballManagerSimulator.Factories;
using FootballManagerSimulator.Interfaces;
using FootballManagerSimulator.Structures;

namespace FootballManagerSimulator.Models;

public class Cup : ICompetition
{
    public string Type => "Cup";
    public int? Round { get; set; }
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public List<Fixture> Fixtures { get; set; } = new List<Fixture>();
    public List<Club> Clubs { get; set; } = new List<Club>();
    public List<DrawDateModel> DrawDates { get; set; } = new List<DrawDateModel>();    
}
