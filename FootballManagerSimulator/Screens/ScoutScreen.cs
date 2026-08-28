using FootballManagerSimulator.Enums;
using FootballManagerSimulator.Interfaces;
using FootballManagerSimulator.Models;
using static FootballManagerSimulator.Screens.PlayerScreen;

namespace FootballManagerSimulator.Screens;

public class ScoutScreen(
    IState state) : BaseScreen(state)
{
    private readonly List<PlayerDetailModel> PlayerDetails = [];
    private readonly IState State = state;

    public override ScreenType Screen => ScreenType.Scout;

    public override Dictionary<string, string> Options => new() {};

	public override string? OptionPrompt => "Enter player Id: ";

	public override void HandleInput(string input)
    {
        switch (input)
        {
			case "UPARROW":
				if (base.OptionIndex > 0)
					base.OptionIndex -= 1;
				break;
			case "DOWNARROW":
				if (Options.Count > 1 && base.OptionIndex < Options.Count - 1)
					base.OptionIndex += 1;
				break;
			case "ESCAPE":
				State.ScreenStack.Pop();
				OptionIndex = 0;
				break;
            default:
                var success = int.TryParse(input, out int result);
                if (!success) return;
                if (PlayerDetails.Count >= result && result > 0)
                {
                    var playerDetail = PlayerDetails.First(p => p.Row == result);
                    State.ScreenStack.Push(new Screen
                    {
                        Type = ScreenType.Player,
                        Parameters = new PlayerScreenObj()
                        {
                            Player = playerDetail.Player
                        }
                    });
                }
                break;
        }
    }

    public override void RenderSubscreen()
    {
        PlayerDetails.Clear();
        var contractedPlayers = State.Players
            .Where(p => p.Contract != null)
            .OrderBy(p => p.Contract!.ClubName);

        for (var i = 0; i < contractedPlayers.Count(); i++)
        {
            PlayerDetails.Add(new PlayerDetailModel
            {
                Player = contractedPlayers.ElementAt(i),
                Row = i + 1
            });
        }

        var freeAgents = State.Players
            .Where(p => p.Contract == null)
            .OrderByDescending(p => p.Rating)
            .Take(100);

        for (var i = 0; i < freeAgents.Count(); i++)
        {
            PlayerDetails.Add(new PlayerDetailModel
            {
                Player = freeAgents.ElementAt(i),
                Row = i + 1 + contractedPlayers.Count()
            });
        }

        Console.WriteLine("All Players\n");
        Console.WriteLine($"{"Row",-5}{"Player",-35}{"Rating",-10}{"Team",-25}{"Position",-10}");

        var orderedPlayerDetails = PlayerDetails
            .OrderBy(p => p.Player.Contract?.ClubName == null)
            .ThenBy(p => p.Player.Contract?.ClubName);

        foreach (var playerDetail in orderedPlayerDetails)
        {
            var club = playerDetail.Player.Contract == null ? "Free Agent" : playerDetail.Player.Contract!.ClubName;
            Console.WriteLine($"{playerDetail.Row,-5}{playerDetail.Player.Name,-35}{playerDetail.Player.Rating,-10}{club,-25}{playerDetail.Player.PreferredPosition}");
        }
    }
}
