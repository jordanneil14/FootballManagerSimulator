﻿using FootballManagerSimulator.Interfaces;
using FootballManagerSimulator.Structures;

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
}