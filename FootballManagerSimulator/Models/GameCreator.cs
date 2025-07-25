﻿using FootballManagerSimulator.Interfaces;
using FootballManagerSimulator.Models;
using Microsoft.Extensions.Options;

namespace FootballManagerSimulator.Models;

public class GameCreator : IGameCreator
{
    private readonly Settings Settings;

    public GameCreator(
        IOptions<Settings> settings)
    {
        Settings = settings.Value;
    }

    public string ManagerName { get; set; } = "";
    public int LeagueId { get; set; }
    public int ClubId { get; set; }
    public IEnumerable<Club> Clubs => Settings.Clubs;
    public IEnumerable<CompetitionModel> Competitions => Settings.Competitions;
}
