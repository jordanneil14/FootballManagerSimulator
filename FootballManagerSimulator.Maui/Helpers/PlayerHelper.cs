using FootballManagerSimulator.Maui.Interfaces;
using FootballManagerSimulator.Maui.Models;

namespace FootballManagerSimulator.Maui.Helpers;

public class PlayerHelper(IState state) : IPlayerHelper
{
    private readonly IState state = state;

    public ClubModel? GetClubByName(string name)
    {
        return state.Clubs
            .Where(p => p.Name.ToLower() == name.ToLower())
            .FirstOrDefault();
    }

    public bool DoesPlayerPlaysForClub(int playerId, int clubId)
    {
        var player = state.Players.First(p => p.Id == playerId);
        return player.Contract != null && clubId == player.Contract.ClubId;
    }

    public PlayerModel? GetPlayerById(int id)
    {
        return state.Players
            .Where(p => p.Id == id)
            .FirstOrDefault();
    }

    public PlayerModel? GetPlayerByName(string name)
    {
        return state.Players
            .Where(p => p.Name == name)
            .FirstOrDefault();
    }

    public void AddPlayersToState(PlayerData playerData)
    {
        for (var i = 0; i < playerData.Players.Count(); i++)
        {
            var clubName = playerData.Players.ElementAt(i).Contract?.ClubName;
            var club = clubName == null ? null : GetClubByName(clubName);

            playerData.Players.ElementAt(i).Id = i + 1;

            if (club == null)
            {
                playerData.Players.ElementAt(i).Contract = null;
                continue;
            }
            playerData.Players.ElementAt(i).Contract!.ClubId = club.Id;
        }

        state.Players.AddRange(playerData.Players);
    }

    public int GetTransferValue(PlayerModel player)
    {
        if (player.Contract == null) return 0;

        return player.Rating switch
        {
            < 50 => player.Rating * 500,
            < 53 => player.Rating * 700,
            < 56 => player.Rating * 1000,
            < 59 => player.Rating * 1750,
            < 62 => player.Rating * 2750,
            < 65 => player.Rating * 3500,
            < 68 => player.Rating * 5000,
            < 71 => player.Rating * 8000,
            < 74 => player.Rating * 12000,
            < 77 => player.Rating * 20000,
            < 80 => player.Rating * 50000,
            < 83 => player.Rating * 135000,
            < 86 => player.Rating * 225000,
            < 89 => player.Rating * 350000,
            < 92 => player.Rating * 575000,
            _ => player.Rating * 750000,
        };
    }
}
