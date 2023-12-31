﻿using FootballManagerSimulator.Enums;
using FootballManagerSimulator.Factories;
using FootballManagerSimulator.Interfaces;
using FootballManagerSimulator.Structures;
using Newtonsoft.Json;
using static FootballManagerSimulator.Interfaces.IState;

namespace FootballManagerSimulator.Screens;

public class LoadGameScreen : IBaseScreen
{
    private readonly IState State;
    private readonly List<LoadGamePreview> Games = new();
    private readonly IUtils Utils;
    private readonly IEnumerable<ICompetitionFactory> LeagueFactories;

    public LoadGameScreen(
        IState state, 
        IUtils utils, 
        IEnumerable<ICompetitionFactory> leagueFactories)
    {
        State = state;
        LeagueFactories = leagueFactories; 
        Utils = utils;
    }

    public ScreenType Screen => ScreenType.LoadGame;

    public void HandleInput(string input)
    {
        switch(input)
        {
            case "B":
                State.ScreenStack.Clear();
                State.ScreenStack.Push(new Screen
                {
                    Type = ScreenType.Welcome
                });
                break;
            default:
                if (input.All(char.IsNumber) && Games.Count >= int.Parse(input))
                {
                    var game = Games.ElementAt(int.Parse(input) - 1);
                    if (game == null) return;
                    TryLoadGame(game.FileName);
                }
                break;
        }
    }

    private void TryLoadGame(string fileName)
    {
        var path = Environment.GetFolderPath(Environment.SpecialFolder.Desktop);
        var fileContent = "";

        try
        {
            fileContent = File.ReadAllText(path + $"\\{fileName}");

            var deserialisedState = JsonConvert.DeserializeObject<SerialisableStateModel>(fileContent);

            var playerItems = deserialisedState.Players;
            Utils.MapPlayersToAClub(playerItems.ToList());
            State.Clubs = Utils.GetResource<IEnumerable<Club>>("teams.json");
            State.Date = deserialisedState.Date;
            State.Events = deserialisedState.Events;

            foreach (var competition in deserialisedState.Competitions)
            {
                var serialisableCompetitionModel = competition.ToObject<SerialisableCompetitionModel>();
                var comp = LeagueFactories.First(p => serialisableCompetitionModel.CompetitionType == p.CompetitionType);
                var deserialisedCompetition = comp.Deserialise(competition);
                State.Competitions.Add(deserialisedCompetition);
            }
            State.Weather = deserialisedState.Weather;
            State.ManagerName = deserialisedState.ManagerName;
            State.Notifications = deserialisedState.Notifications;
            State.MyClub = Utils.GetClub(deserialisedState.MyClub.ID);
            State.Players = deserialisedState.Players.Select(p =>
            {
                var club = p.Contract == null ? null : Utils.GetClub(p.Contract.ClubID);
                var player = Player.FromPlayerData(p, p.ID, club);
                return player;  
            }).ToList();
            State.ScreenStack.Clear();
            State.ScreenStack.Push(new Screen()
            {
                Type = ScreenType.Main
            });
        }
        catch (Exception)
        {
            State.UserFeedbackUpdates.Add("Unable to load game");
        }
    }

    public void RenderScreen()
    {
        Games.Clear();
        var path = Environment.GetFolderPath(Environment.SpecialFolder.Desktop);
        var directoryInfo = new DirectoryInfo(path);
        var files = directoryInfo.GetFiles("*.fms");

        foreach(var file in files)
        {
            try
            {
                var fileContents = File.ReadAllText(file.FullName);
                var deserialisedContent = JsonConvert.DeserializeObject<PreviewModel>(fileContents);
                if (deserialisedContent == null) continue;
                Games.Add(new LoadGamePreview
                {
                    FileName = file.Name,
                    ClubName = deserialisedContent.Club.Name,
                    SaveDate = file.LastWriteTime
                });
            }
            catch (Exception)
            {
                //Ignore and move to the next file
            }
        }

        Console.WriteLine("Load Game\n");

        if (!Games.Any())
        {
            Console.WriteLine("No game files found on your desktop");
            Console.WriteLine("\nOptions:");
            Console.WriteLine("B) Back");
            return;
        }

        Console.WriteLine(string.Format("{0,-10}{1,-30}{2,-30}{3,-20}", "Number", "File Name", "Club Managed", "Last Modified"));
        for(var i = 0; i < Games.Count; i++)
        {
            Console.WriteLine(string.Format("{0,-10}{1,-30}{2,-30}{3,-20}", i+1, Games.ElementAt(i).FileName, Games.ElementAt(i).ClubName, Games.ElementAt(i).SaveDate));
        }
        Console.WriteLine("\nOptions:");
        Console.WriteLine("B) Back");
        Console.WriteLine("<Enter Number>) Load Game");
    }
}


