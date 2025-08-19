using FootballManagerSimulator.Interfaces;
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
    private readonly IPlayerHelper PlayerHelper = playerHelper;
    private readonly IState State = state;
    private readonly IEnumerable<ICompetitionFactory> CompetitionFactories = competitionFactories;
    private readonly INotificationFactory NotificationFactory = notificationFactory;
    private readonly IGameCreator GameCreator = gameCreator;
    private readonly ITacticHelper TacticHelper = tacticHelper;
    private readonly IWeatherHelper WeatherHelper = weatherHelper;
    private readonly ITransferListHelper TransferListHelper = transferListHelper;
    private readonly IEnumerable<IEventFactory> EventFactories = eventFactories;

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

        State.MyClubId = GameCreator.ClubId;

        var content = File.ReadAllText($"Resources\\playerData.json");
        var playerData = JsonConvert.DeserializeObject<PlayerData>(content);
        if (playerData == null)
            throw new Exception("Unable to load players from playerData.json");

        PlayerHelper.AddPlayersToState(playerData);

        foreach (var club in State.Clubs)
        {
            TacticHelper.ResetTacticForClub(club);
        }

        foreach (var competition in Settings.Competitions)
        {
            var competitionFactory = CompetitionFactories
                .First(p => p.Type.ToString() == competition.Type).CreateCompetition(competition);
            State.Competitions.Add(competitionFactory);
        }

        foreach (var comp in State.Competitions.Where(p => p.Type == Enums.CompetitionType.Cup))
        {
            foreach (var s in comp.DrawDates)
            {
                var eventFactory = EventFactories.First(p => p.Type == Enums.EventType.CupDrawFixture);
                eventFactory.Data.DrawDate = new DateTime(s.DrawDate.Year, s.DrawDate.Month, s.DrawDate.Day);
                eventFactory.Data.FixtureDate = new DateTime(s.FixtureDate.Year, s.FixtureDate.Month, s.FixtureDate.Day);
                eventFactory.Data.Round = s.Round;
                eventFactory.Data.CompetitionId = comp.Id;
                eventFactory.CreateEvent();
            }
        }

        TransferListHelper.UpdateTransferList();

        var freeAgents = State.Players
            .Where(p => p.Contract == null)
            .OrderByDescending(p => p.Rating)
            .Select(p => p.Name)
            .Take(4);

        NotificationFactory.AddNotification(
            State.Date,
            "Chairman",
            $"Welcome to {State.Clubs.First(p => p.Id == State.MyClubId).Name}",
            "Everyone at the club wishes you a successful reign as manager.");

        NotificationFactory.AddNotification(
            State.Date,
            "Chairman",
            "Transfer Budget",
            $"Your transfer budget for the upcoming season is {State.Clubs.First(p => p.Id == State.MyClubId).TransferBudgetFriendly}.");

        NotificationFactory.AddNotification(
            State.Date.AddDays(1),
            "Scout",
            "Players With Expired Contracts",
            $"Congratulations on your new job! There are lots of free agents on the marketplace at the minute. Here are a\n" +
            $"small list of players which you may be interested in:\n\t{string.Join("\n\t", freeAgents)}{Environment.NewLine}Free agents can be found on the Scout page.");

        foreach (var comp in State.Competitions.Where(p => p.Type == Enums.CompetitionType.Friendly))
        {
            foreach (var s in comp.DrawDates)
            {
                var eventFactory = EventFactories.First(p => p.Type == Enums.EventType.FriendlyDrawFixture);
                eventFactory.Data.FixtureDate = new DateTime(s.FixtureDate.Year, s.FixtureDate.Month, s.FixtureDate.Day);
                eventFactory.Data.Round = s.Round;
                eventFactory.CreateEvent();
            }
        }

        var concludedEvents = State.Events.Where(p => p.CompletionDate <= State.Date);
        foreach (var concludedEvent in concludedEvents)
        {
            var eventFactory = EventFactories.First(p => p.Type == concludedEvent.Type);
            eventFactory.CompleteEvent(concludedEvent);
        }
    }
}
