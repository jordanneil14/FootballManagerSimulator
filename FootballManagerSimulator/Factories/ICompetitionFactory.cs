using FootballManagerSimulator.Interfaces;
using FootballManagerSimulator.Structures;
using Newtonsoft.Json.Linq;

namespace FootballManagerSimulator.Factories;

public interface ICompetitionFactory
{
    string CompetitionType { get; }
    ICompetition CreateLeague(string name, IEnumerable<Team> teams);
    ICompetition Deserialise(JObject s);
}

