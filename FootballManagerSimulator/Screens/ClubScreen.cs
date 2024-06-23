using FootballManagerSimulator.Enums;
using FootballManagerSimulator.Interfaces;
using FootballManagerSimulator.Structures;

namespace FootballManagerSimulator.Screens;

public class ClubScreen(
    IState state,
    IPlayerHelper utils) : BaseScreen(state)
{
    public override ScreenType Screen => ScreenType.Club;

    public override void HandleInput(string input)
    {
        switch (input)
        {
            case "B":
                state.ScreenStack.Pop();
                break;
            default:
                var isInt = int.TryParse(input, out int value);
                if (!isInt) return;
                var player = utils.GetPlayerById(value);
                if (player != null)
                {
                    state.ScreenStack.Push(PlayerScreen.CreateScreen(player));
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
        var clubScreenObj = state.ScreenStack.Peek().Parameters as ClubScreenObj;

        Console.WriteLine($"{clubScreenObj!.Club.Name}");

        Console.WriteLine($"\nStadium:\n{clubScreenObj.Club.Stadium}\n");

        Console.WriteLine("Upcoming Fixtures:");
        var upcomingFixtures = state.Competitions
            .SelectMany(p => p.Fixtures)
            .Where(p => p.HomeClub.Id == clubScreenObj.Club.Id || p.AwayClub.Id == clubScreenObj.Club.Id).Take(5);
        foreach (var fixture in upcomingFixtures)
        {
            var comp = state.Competitions.First(p => p.Fixtures.Contains(fixture));
            Console.WriteLine($"{comp.Name} - {fixture.HomeClub.Name} v {fixture.AwayClub.Name}");
        }

        Console.WriteLine("\nPlayers:");

        var players = state.Players.Where(p => p.Contract?.ClubId == clubScreenObj.Club.Id);

        Console.WriteLine($"{"Id",-10}{"Number",-10}{"Position",-10}{"Name",-40}{"Rating",-10}{"Transfer Value",-20}");
        Console.WriteLine("-----------------------------------------------------------------------------------------------");

        foreach (var player in players.OrderBy(p => p.Name))
        {
            var transferValue = utils.GetTransferValue(player);
            var transferValueFriendly = $"£{transferValue:n}";
            Console.WriteLine($"{player.Id,-10}{player.ShirtNumber,-10}{player.PreferredPosition,-10}{player.Name,-40}{player.Rating,-10}{transferValueFriendly,-10}");
        }
    }

    public override void RenderOptions()
    {
        Console.WriteLine("Options:");
        Console.WriteLine("B) Back");
        Console.WriteLine("<Enter Player ID>) Go To Player");
    }
}
