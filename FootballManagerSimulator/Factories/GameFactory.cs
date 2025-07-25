﻿using FootballManagerSimulator.Interfaces;
using FootballManagerSimulator.Models;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;

namespace FootballManagerSimulator.Factories;

public class GameFactory(
    IPlayerHelper playerHelper,
    IState state,
    IEnumerable<ICompetitionFactory> competitionFactories,
    INotificationFactory notificationFactory,
    IGameCreator gameCreator,
    IOptions<Settings> settings,
    ITacticHelper tacticHelper,
    IWeatherHelper weatherHelper,
    ITransferListHelper transferListHelper,
    IEnumerable<IEventFactory> eventFactories) : IGameFactory
{
    private readonly Settings Settings = settings.Value;

    public void CreateGame()
    {
        state.ManagerName = gameCreator.ManagerName;

        state.Date = Settings.General.StartDateAsDate;

        state.Weather = weatherHelper.GetTodaysWeather();

        state.Clubs = Settings.Clubs.Select(p => new Club
        {
            Id = p.Id,
            Name = p.Name,
            Stadium = p.Stadium,
            TransferBudget = p.TransferBudget,
            WageBudget = p.WageBudget,
            LeagueId = p.LeagueId
        }).ToList();

        state.MyClubId = gameCreator.ClubId;

        var content = File.ReadAllText($"Resources\\playerData.json");
        var playerData = JsonConvert.DeserializeObject<PlayerData>(content);
        if (playerData == null)
            throw new Exception("Unable to load players from playerData.json");

        playerHelper.AddPlayersToState(playerData);

        foreach (var club in state.Clubs)
        {
            tacticHelper.ResetTacticForClub(club);
        }

        foreach (var competition in Settings.Competitions)
        {
            var competitionFactory = competitionFactories
                .First(p => p.Type.ToString() == competition.Type).CreateCompetition(competition);
            state.Competitions.Add(competitionFactory);
        }

        foreach(var comp in state.Competitions.Where(p => p.Type == Enums.CompetitionType.Cup))
        {
            foreach(var s in comp.DrawDates)
            {
                var eventFactory = eventFactories.First(p => p.Type == Enums.EventType.CupDrawFixture);
                eventFactory.Data.DrawDate = new DateTime(s.DrawDate.Year, s.DrawDate.Month, s.DrawDate.Day);
                eventFactory.Data.FixtureDate = new DateTime(s.FixtureDate.Year, s.FixtureDate.Month, s.FixtureDate.Day);
                eventFactory.Data.Round = s.Round;
                eventFactory.Data.CompetitionId = comp.Id;
                eventFactory.CreateEvent();
            }
        }

        transferListHelper.UpdateTransferList();

        var freeAgents = state.Players
            .Where(p => p.Contract == null)
            .OrderByDescending(p => p.Rating)
            .Select(p => p.Name)
            .Take(4);

        notificationFactory.AddNotification(
            state.Date,
            "Chairman",
            $"Welcome to {state.Clubs.First(p => p.Id == state.MyClubId).Name}",
            "Everyone at the club wishes you a successful reign as manager.");

        notificationFactory.AddNotification(
            state.Date,
            "Chairman",
            "Transfer Budget",
            $"Your transfer budget for the upcoming season is {state.Clubs.First(p => p.Id == state.MyClubId).TransferBudgetFriendly}.");

        notificationFactory.AddNotification(
            state.Date.AddDays(1),
            "Scout",
            "Players With Expired Contracts",
            $"Congratulations on your new job! There are lots of free agents on the marketplace at the minute. Here are a\n" +
            $"small list of players which you may be interested in:\n\t{string.Join("\n\t", freeAgents)}{Environment.NewLine}Free agents can be found on the Scout page.");

        foreach (var comp in state.Competitions.Where(p => p.Type == Enums.CompetitionType.Friendly))
        {
            foreach (var s in comp.DrawDates)
            {
                var eventFactory = eventFactories.First(p => p.Type == Enums.EventType.FriendlyDrawFixture);
                eventFactory.Data.FixtureDate = new DateTime(s.FixtureDate.Year, s.FixtureDate.Month, s.FixtureDate.Day);
                eventFactory.Data.Round = s.Round;
                eventFactory.CreateEvent();
            }
        }

        var concludedEvents = state.Events.Where(p => p.CompletionDate <= state.Date);
        foreach(var concludedEvent in concludedEvents)
        {
            var eventFactory = eventFactories.First(p => p.Type == concludedEvent.Type);
            eventFactory.CompleteEvent(concludedEvent);
        }
    }
}
