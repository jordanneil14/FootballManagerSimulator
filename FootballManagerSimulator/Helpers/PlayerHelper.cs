using FootballManagerSimulator.Interfaces;
using FootballManagerSimulator.Structures;
using System.Numerics;

namespace FootballManagerSimulator.Helpers;

public class PlayerHelper : IPlayerHelper
{
    private readonly IState State;

    public PlayerHelper(IState state)
    {
        State = state;
    }

    public Club? GetClubByName(string name)
    {
        return State.Clubs
            .Where(p => p.Name.ToLower() == name.ToLower())
            .FirstOrDefault();
    }

    public bool PlayerPlaysForClub(int playerId, int clubId)
    {
        var player = State.Players.First(p => p.Id == playerId);
        return player.Contract != null && clubId == player.Contract.ClubId;
    }

    public Player? GetPlayerById(int id)
    {
        return State.Players
            .Where(p => p.Id == id)
            .FirstOrDefault();
    }

    public Player? GetPlayerByName(string name)
    {
        return State.Players
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

        State.Players.AddRange(playerData.Players);
    }

    public int GetTransferValue(Player player)
    {
        if (player.Contract == null) return 0;

        return player.Rating switch
        {
            < 50 => player.Rating * 1000,
            < 55 => player.Rating * 2000,
            < 60 => player.Rating * 3500,
            < 65 => player.Rating * 6500,
            < 70 => player.Rating * 11000,
            < 75 => player.Rating * 19000,
            < 80 => player.Rating * 100000,
            < 85 => player.Rating * 400000,
            < 90 => player.Rating * 800000,
            _ => player.Rating * 2000000,
        };
    }
}
