using FootballManagerSimulator.Enums;
using FootballManagerSimulator.Factories;
using FootballManagerSimulator.Interfaces;
using FootballManagerSimulator.Structures;

namespace FootballManagerSimulator.Screens;

public class SelectTeamScreen : IBaseScreen
{
    private readonly IState State;
    private readonly IUtils HelperFunction;

    public SelectTeamScreen(
        IState state, 
        IUtils helperFunction)
    {
        State = state;
        HelperFunction = helperFunction;
    }

    public ScreenType Screen => ScreenType.SelectTeam;

    public void HandleInput(string input)
    {
        var team = HelperFunction.GetTeamByName(input);

        if (team != null)
        {
            State.MyTeam = team;
            SetupStateForNewGame();
            State.ScreenStack.Clear();
            State.ScreenStack.Push(new Screen
            {
                Type = ScreenType.Main
            });
            return;
        }

        switch(input)
        {
            case "B":
                State.ScreenStack.Pop();
                break;
        }
    }

    public void SetupStateForNewGame()
    {
        State.Date = new DateOnly(2022, 07, 01);

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

        Console.WriteLine(string.Format("{0,-50}{1,-40}", "Team", "League Competition"));
        foreach(var team in State.Teams) 
        {
            var competition = State.Competitions.FirstOrDefault(p => p.ID == team.CompetitionID);
            Console.WriteLine($"{team.Name,-50}{competition.Name}");
        }

        Console.WriteLine("\nOptions:");
        Console.WriteLine("B) Back");
        Console.WriteLine("<Enter Team Name>) To Manage Team");
    }
}
