using FootballManagerSimulator.Models;
using FootballManagerSimulator.Structures;

namespace FootballManagerSimulator.Interfaces;

public interface ICompetition
{
    string Type { get; }
    int Id { get; } 
    string Name { get; }
    List<Fixture> Fixtures { get; set; }
    List<Club> Clubs { get; set; }
    List<DrawDateModel> DrawDates { get; set; }
}
