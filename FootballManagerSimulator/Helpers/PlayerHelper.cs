using FootballManagerSimulator.Interfaces;
using FootballManagerSimulator.Structures;

namespace FootballManagerSimulator.Helpers;

public class PlayerHelper(IState state) : IPlayerHelper
{
    public Club? GetClubByName(string name)
    {
        return state.Clubs
            .Where(p => p.Name.ToLower() == name.ToLower())
            .FirstOrDefault();
    }

    public bool PlayerPlaysForClub(int playerId, int clubId)
    {
        var player = state.Players.First(p => p.Id == playerId);
        return player.Contract != null && clubId == player.Contract.ClubId;
    }

    public Player? GetPlayerById(int id)
    {
        return state.Players
            .Where(p => p.Id == id)
            .FirstOrDefault();
    }

    public Player? GetPlayerByName(string name)
    {
        return state.Players
            .Where(p => p.Name == name)
            .FirstOrDefault();
    }

    public void AddPlayersToState(PlayerData playerData)
    {
        for (var i = 0; i < playerData.Players.Count(); i++)
        {
            var club = playerData.Players.ElementAt(i).Contract?.ClubName == null
                ? null 
                : GetClubByName(playerData.Players.ElementAt(i).Contract.ClubName);

            playerData.Players.ElementAt(i).Id = i+1;

            if (club == null)
            {
                playerData.Players.ElementAt(i).Contract = null;
                continue;
            }
            playerData.Players.ElementAt(i).Contract!.ClubId = club.Id;
        }

        state.Players.AddRange(playerData.Players);
    }

    public int GetTransferValue(Player player)
    {
        if (player.Contract == null) return 0;

        return player.Rating switch
        {
            < 50 => player.Rating * 1000,
            < 53 => player.Rating * 1400,
            < 56 => player.Rating * 2000,
            < 59 => player.Rating * 3500,
            < 62 => player.Rating * 5500,
            < 65 => player.Rating * 7000,
            < 68 => player.Rating * 10000,
            < 71 => player.Rating * 16000,
            < 74 => player.Rating * 24000,
            < 77 => player.Rating * 40000,
            < 80 => player.Rating * 100000,
            < 83 => player.Rating * 270000,
            < 86 => player.Rating * 450000,
            < 89 => player.Rating * 700000,
            < 92 => player.Rating * 1150000,
            _ => player.Rating * 1500000,
        };
    }
}
