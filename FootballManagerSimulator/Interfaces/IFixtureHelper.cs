using FootballManagerSimulator.Structures;

namespace FootballManagerSimulator.Interfaces;

public interface IFixtureHelper
{
    List<Fixture> GenerateFixtures(List<Team> teams);
}
