using FootballManagerSimulator.Structures;

namespace FootballManagerSimulator.Interfaces;

public interface IHelperFunction
{
    Player? GetPlayerByName(string name);
    Team? GetTeamByName(string name);
    IEnumerable<Team> GetTeams();
    Team GetTeam(int id);
    Player GetPlayer(int id);
    IEnumerable<Player.SerialisablePlayerModel> GetPlayers();
}
