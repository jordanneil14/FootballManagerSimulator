using FootballManagerSimulator.Interfaces;
using FootballManagerSimulator.Structures;
using Newtonsoft.Json;

namespace FootballManagerSimulator.Helpers;

public class Utils : IUtils
{
    private readonly IState State;

    public Utils(IState state)
    {
        State = state;
    }

    public T GetResource<T>(string filename)
    {
        var content = File.ReadAllText($"Resources\\{filename}");
        var data = JsonConvert.DeserializeObject<T>(content);
        return data;
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
