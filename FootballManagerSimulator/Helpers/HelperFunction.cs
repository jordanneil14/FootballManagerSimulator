using FootballManagerSimulator.Interfaces;
using FootballManagerSimulator.Structures;
using Newtonsoft.Json;
using System.Reflection;

namespace FootballManagerSimulator.Helpers;

public class HelperFunction : IHelperFunction
{
    private readonly IState State;

    public HelperFunction(IState state)
    {
        State = state;
    }

    public IEnumerable<Team> GetTeams()
    {
        var path = Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), $"Resources\\teams.json");
        var content = File.ReadAllText(path);
        var teams = JsonConvert.DeserializeObject<IEnumerable<Team>>(content);
        return teams;
    }

    public IEnumerable<Player.SerialisablePlayerModel> GetPlayers()
    {
        var path = Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), @"Resources\\playersImproved.json");
        var content = File.ReadAllText(path);
        var players = JsonConvert.DeserializeObject<IEnumerable<Player.SerialisablePlayerModel>>(content);
        return players;
    }

    public Team? GetTeamByName(string name)
    {
        return State.Teams.Where(p => p.Name.ToLower() == name.ToLower()).FirstOrDefault();
    }

    public Team GetTeam(int id)
    {
        return State.Teams.Where(p => p.ID == id).First();
    }

    public Player GetPlayer(int id)
    {
        return State.Players.Where(p => p.ID == id).First();
    }

    public Player? GetPlayerByName(string name)
    {
        return State.Players.Where(p => p.Name == name).FirstOrDefault();
    }
}
