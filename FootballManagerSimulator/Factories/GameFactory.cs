﻿using FootballManagerSimulator.Interfaces;
using FootballManagerSimulator.Structures;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;

namespace FootballManagerSimulator.Factories;

public class GameFactory : IGameFactory
{
    private readonly IClubHelper ClubHelper;
    private readonly IPlayerHelper PlayerHelper;
    private readonly IState State;
    private readonly ILeagueFactory LeagueFactory;
    private readonly INotificationFactory NotificationFactory;
    private readonly IGameCreator GameCreator;
    private readonly Settings Settings;
    private readonly ITacticHelper TacticHelper;
    private readonly IWeatherHelper WeatherHelper;

    public GameFactory(
        IClubHelper clubHelper,
        IPlayerHelper playerHelper,
        IState state,
        ILeagueFactory leagueFactory,
        INotificationFactory notificationFactory,
        IGameCreator gameCreator,
        IOptions<Settings> settings,
        ITacticHelper tacticHelper,
        IWeatherHelper weatherHelper)
    {
        ClubHelper = clubHelper;
        PlayerHelper = playerHelper;
        State = state;
        LeagueFactory = leagueFactory;
        NotificationFactory = notificationFactory;
        GameCreator = gameCreator;
        Settings = settings.Value;
        TacticHelper = tacticHelper;
        WeatherHelper = weatherHelper;  
    }

    public void CreateGame()
    {
        State.ManagerName = GameCreator.ManagerName;

        State.Date = Settings.General.StartDateAsDate;

        State.Weather = WeatherHelper.GetTodaysWeather();

        State.Clubs = Settings.Clubs.Select(p => new Club
        {
            Id = p.Id,
            Name = p.Name,
            Stadium = p.Stadium,
            TransferBudget = p.TransferBudget,
            WageBudget = p.WageBudget,
            LeagueId = p.LeagueId
        }).ToList();

        State.MyClub = ClubHelper.GetClubById(GameCreator.ClubId);

        var content = File.ReadAllText($"Resources\\playerData.json");
        var playerData = JsonConvert.DeserializeObject<PlayerData>(content);
        if (playerData == null)
            throw new Exception("Unable to load players from playerData.json");

        PlayerHelper.AddPlayersToState(playerData);

        foreach(var club in State.Clubs)
        {
            TacticHelper.ResetTacticForClub(club);
        }

        State.Leagues = Settings.Leagues.Select(p => LeagueFactory.CreateLeague(p)).ToList();

        var freeAgents = State.Players
            .Where(p => p.Contract == null)
            .OrderByDescending(p => p.Rating)
            .Select(p => p.Name)
            .Take(4);

        NotificationFactory.AddNotification(
            State.Date,
            "Chairman",
            $"Welcome to {State.MyClub.Name}",
            "Everyone at the club wishes you a successful reign as manager.");

        NotificationFactory.AddNotification(
            State.Date,
            "Chairman",
            "Transfer Budget",
            $"Your transfer budget for the upcoming season is {State.MyClub.TransferBudgetFriendly}.");

        NotificationFactory.AddNotification(
            State.Date.AddDays(1),
            "Scout",
            "Players With Expired Contracts",
            $@"Congratulations on your new job! There are lots of free agents on the marketplace at the minute. Here are a small list of players which you might be interested in:
                {string.Join('\n', freeAgents)}{Environment.NewLine}All free agents can be found on the Scout page.");
    }
}