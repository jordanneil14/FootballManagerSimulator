using FootballManagerSimulator.Enums;
using FootballManagerSimulator.Interfaces;
using FootballManagerSimulator.Structures;

namespace FootballManagerSimulator.Screens;

public class SelectTeamScreen : IBaseScreen
{
    private readonly IState State;
    private readonly IHelperFunction HelperFunction;
    private readonly IFixtureHelper FixtureHelper;

    public SelectTeamScreen(IState state, IHelperFunction helperFunction, IFixtureHelper fixtureHelper)
    {
        State = state;
        HelperFunction = helperFunction;
        FixtureHelper = fixtureHelper;
    }

    public ScreenType Screen => ScreenType.SelectTeam;

    public void HandleInput(string input)
    {
        var team = HelperFunction.GetTeamByName(input);

        if (team != null)
        {
            State.MyTeam = team;
            SetupStateForNewGame();
            State.CurrentScreen.Type = ScreenType.Main;
            return;
        }

        switch(input)
        {
            case "B":
                State.CurrentScreen.Type = ScreenType.CreateManager;
                break;
        }
    }

    public void SetupStateForNewGame()
    {
        State.Date = new DateOnly(2022, 07, 01);

        State.Competitions.Add(new League
        {
            Fixtures = FixtureHelper.GenerateFixtures(State.Teams.ToList()),
            Name = "Premier League"
        });

        State.Weather = "Sunny 28c";
        
        var freeAgents = State.Players.Where(p => p.Contract == null).OrderByDescending(p => p.Rating).Take(4);
        State.Notifications = new List<Notification>()
        {
            new Notification
            {
                Date = State.Date,
                Recipient = "Chairman",
                Subject = $"Welcome to {State.MyTeam.Name}",
                Message = "Everyone at the club wishes you a successful reign as manager."
            },
            new Notification
            {
                Date = State.Date,
                Recipient = "Chairman",
                Subject = "Transfer Budget",
                Message = $"Your transfer budget for the upcoming season is {State.MyTeam.TransferBudgetFriendly}."
            },
            new Notification
            {
                Date = State.Date.AddDays(1),
                Recipient = "Scout",
                Subject = "Players With Expired Contracts",
                Message = "Congratulations on your new job! There are lots of free agents on the " +
                    "marketplace at the minute. Here is a small list of players which you might " +
                    $"be interested in:\n{string.Join('\n', freeAgents)} \n\n" +
                    "All free agents can be found on the Scout page."
            }
        };
    }

    public void RenderScreen()
    {
        Console.WriteLine("Select a team to manage:\n");
        foreach(var team in State.Teams) 
        {
            Console.WriteLine($"{team}");
        }

        Console.WriteLine("\nOptions:");
        Console.WriteLine("B) Back");
        Console.WriteLine("<Enter Team Name>) To Manage Team");
    }
}
