using FootballManagerSimulator.Interfaces;
using FootballManagerSimulator.Structures;
using Newtonsoft.Json.Linq;

namespace FootballManagerSimulator.Factories;

public interface ICompetitionFactory
{
    string CompetitionType { get; }
    ICompetition CreateCompetition(Competition competition);
    ICompetition Deserialise(JObject s);
    IEnumerable<Fixture> GenerateNextRoundOfFixtures(IEnumerable<Club> clubs, League league);
}

