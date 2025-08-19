using FootballManagerSimulator.Enums;
using FootballManagerSimulator.Interfaces;
using FootballManagerSimulator.Models;

namespace FootballManagerSimulator.Screens;

public class ClubScreen(
    IState state,
    IPlayerHelper utils) : BaseScreen(state)
{
    private readonly IState State = state;
    private readonly IPlayerHelper Utils = utils;

    public override ScreenType Screen => ScreenType.Club;

    public override void HandleInput(string input)
    {
        switch (input)
        {
            case "B":
                State.ScreenStack.Pop();
                break;
            default:
                var isInt = int.TryParse(input, out int value);
                if (!isInt) return;
                var player = Utils.GetPlayerById(value);
                if (player != null)
                {
                    State.ScreenStack.Push(PlayerScreen.CreateScreen(player));
                }
                break;
        }
    }

    public static Screen CreateScreen(Club club)
    {
        return new Screen
        {
            Type = ScreenType.Club,
            Parameters = new ClubScreenObj
            {
                Club = club
            }
        };
    }

    public class ClubScreenObj
    {
        public Club Club { get; set; } = new Club();
    }

    public override void RenderSubscreen()
    {
        var clubScreenObj = State.ScreenStack.Peek().Parameters as ClubScreenObj;

        Console.WriteLine($"{clubScreenObj!.Club.Name}");

        Console.WriteLine($"\nStadium:\n{clubScreenObj.Club.Stadium}\n");

        Console.WriteLine("Upcoming Fixtures:");
        var upcomingFixtures = State.Competitions
            .SelectMany(p => p.Fixtures)
            .Where(p => p.HomeClub.Id == clubScreenObj.Club.Id || p.AwayClub.Id == clubScreenObj.Club.Id).Take(5);
        foreach (var fixture in upcomingFixtures)
        {
            var comp = State.Competitions.First(p => p.Fixtures.Contains(fixture));
            Console.WriteLine($"{comp.Name} - {fixture.HomeClub.Name} v {fixture.AwayClub.Name}");
        }

        Console.WriteLine("\nPlayers:");

        var players = State.Players.Where(p => p.Contract?.ClubId == clubScreenObj.Club.Id);

        Console.WriteLine($"{"Id",-10}{"Number",-10}{"Position",-10}{"Name",-40}{"Rating",-10}{"Transfer Value",-20}{"Contract Expiry Date",-15}");
        Console.WriteLine("------------------------------------------------------------------------------------------------------------------------");

        foreach (var player in players.OrderBy(p => p.Name))
        {
            var transferValue = Utils.GetTransferValue(player);
            var transferValueFriendly = $"£{transferValue:n}";
            Console.WriteLine($"{player.Id,-10}{player.ShirtNumber,-10}{player.PreferredPosition,-10}{player.Name,-40}{player.Rating,-10}{transferValueFriendly,-20}{player.Contract.ExpiryDate,-15}");
        }
    }

    public override void RenderOptions()
    {
        Console.WriteLine("Options:");
        Console.WriteLine("B) Back");
        Console.WriteLine("<Enter Player ID>) Go To Player");
    }
}
