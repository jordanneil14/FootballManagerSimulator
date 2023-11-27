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

    public Club? GetClubByName(string name)
    {
        return State.Clubs.Where(p => p.Name.ToLower() == name.ToLower()).FirstOrDefault();
    }

    public Club GetClub(int id)
    {
        return State.Clubs.Where(p => p.ID == id).First();
    }

    public Player? GetPlayerById(int id)
    {
        return State.Players.Where(p => p.ID == id).FirstOrDefault();
    }

    public Player? GetPlayerByName(string name)
    {
        return State.Players.Where(p => p.Name == name).FirstOrDefault();
    }

    public void MapPlayersToAClub(List<Player.SerialisablePlayerModel> serialisablePlayers)
    {
        for (var i = 0; i < serialisablePlayers.Count; i++)
        {
            Club? club = null;
            if (serialisablePlayers[i].Contract != null)
            {
                club = GetClubByName(serialisablePlayers[i].Contract!.ClubName);
            }
            var player = Player.FromPlayerData(serialisablePlayers[i], i + 1, club);
            State.Players.Add(player);
        }
    }

    public int GetStartingElevenSumRatingForClub(Club club)
    {
        var startingEleven = club.TacticSlots
            .Where(p => p.TacticSlotType != Enums.TacticSlotType.SUB && p.TacticSlotType != Enums.TacticSlotType.RES);

        var sum = 0;
        foreach(var slot in startingEleven)
        {
            if (slot.PlayerID == null) continue;
            var playerRating = State.Players.First(p => p.ID == slot.PlayerID).Rating;
            sum += playerRating;
        }
        return sum;
    }
}
