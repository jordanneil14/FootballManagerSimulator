using FootballManagerSimulator.Interfaces;
using FootballManagerSimulator.Structures;
using Newtonsoft.Json.Linq;

namespace FootballManagerSimulator.Factories;

public interface ICompetitionFactory
{
    string CompetitionType { get; }
    ICompetition CreateCompetition(Competition competition);
    ICompetition Deserialise(JObject s);
    List<Fixture> GenerateNextRoundOfFixtures(List<Club> clubs, League league);
}

