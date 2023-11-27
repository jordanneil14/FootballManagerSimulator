using FootballManagerSimulator.Enums;
using FootballManagerSimulator.Factories;
using FootballManagerSimulator.Interfaces;
using FootballManagerSimulator.Structures;

namespace FootballManagerSimulator.Screens;

public class SelectClubScreen : IBaseScreen
{
    private readonly IState State;
    private readonly IUtils Utils;

    public SelectClubScreen(
        IState state, 
        IUtils utils)
    {
        State = state;
        Utils = utils;
    }

    public ScreenType Screen => ScreenType.SelectClub;

    public void HandleInput(string input)
    {
        var club = Utils.GetClubByName(input);

        if (club != null)
        {
            State.MyClub = club;
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
        
        var freeAgents = State.Players.Where(p => p.Contract == null).OrderByDescending(p => p.Rating).Select(p => p.Name).Take(4);
        State.Notifications = new List<Notification>()
        {
            new Notification
            {
                Date = State.Date,
                Recipient = "Chairman",
                Subject = $"Welcome to {State.MyClub.Name}",
                Message = "Everyone at the club wishes you a successful reign as manager."
            },
            new Notification
            {
                Date = State.Date,
                Recipient = "Chairman",
                Subject = "Transfer Budget",
                Message = $"Your transfer budget for the upcoming season is {State.MyClub.TransferBudgetFriendly}."
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
        Console.WriteLine("Select a club to manage:\n");

        Console.WriteLine($"{"Team",-50}{"League",-40}");
        var clubs = State.Clubs.OrderBy(p => p.CompetitionID).ThenBy(p => p.Name);
        foreach (var club in clubs)
        {
            var competition = State.Competitions.First(p => p.ID == club.CompetitionID);
            Console.WriteLine($"{club.Name,-50}{competition.Name}");
        }

        Console.WriteLine("\nOptions:");
        Console.WriteLine("B) Back");
        Console.WriteLine("<Enter Club Name>) To Manage Club");
    }
}
