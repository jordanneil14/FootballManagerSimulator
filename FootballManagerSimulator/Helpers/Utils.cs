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

    public Club? GetTeamByName(string name)
    {
        return State.Clubs.Where(p => p.Name.ToLower() == name.ToLower()).FirstOrDefault();
    }

    public Club GetTeam(int id)
    {
        return State.Clubs.Where(p => p.ID == id).First();
    }

    public Player GetPlayer(int id)
    {
        return State.Players.Where(p => p.ID == id).First();
    }

    public Player? GetPlayerByName(string name)
    {
        return State.Players.Where(p => p.Name == name).FirstOrDefault();
    }

    public Player? GetPlayerByClubAndShirtNumber(Club club, int shirtNumber)
    {
        return State.Clubs.Where(p => p == club).FirstOrDefault()?.Players.Where(p => p.ShirtNumber == shirtNumber).FirstOrDefault();
    }

    public void MapPlayersToATeam(List<Player.SerialisablePlayerModel> serialisablePlayers)
    {
        for (int i = 0; i < serialisablePlayers.Count; i++)
        {
            if (serialisablePlayers[i].Name == "De Gea")
            {

            }

            Club? club = null;
            if (serialisablePlayers[i].Contract != null)
            {
                club = GetTeamByName(serialisablePlayers[i].Contract!.ClubName);
            }
            var player = Player.FromPlayerData(serialisablePlayers[i], i + 1, club);
            State.Players.Add(player);
        }
    }
}
