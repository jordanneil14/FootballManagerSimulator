using FootballManagerSimulator.Enums;
using FootballManagerSimulator.Interfaces;
using FootballManagerSimulator.Structures;
using static FootballManagerSimulator.Screens.PlayerScreen;

namespace FootballManagerSimulator.Screens;

public class ScoutScreen : BaseScreen
{
    private readonly IState State;
    private readonly List<PlayerDetailModel> PlayerDetails = new(); 

    public ScoutScreen(IState state) : base(state)
    {
        State = state;
    }

    public override ScreenType Screen => ScreenType.Scout;

    public override void HandleInput(string input)
    {
        switch (input)
        {
            case "B":
                State.ScreenStack.Pop();
                break;
            default:
                var success = int.TryParse(input, out int result);
                if (!success) return;
                if (PlayerDetails.Count > result && result > 0)
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

    public override void RenderOptions()
    {
        Console.WriteLine("Options:");
        Console.WriteLine("B) Back");
        Console.WriteLine("<Enter Row>) Go To Player");
    }

    public override void RenderSubscreen()
    {
        PlayerDetails.Clear();
        var employeedPlayers = State.Players.Where(p => p.Contract != null).OrderBy(p => p.Contract!.Club.Name);
        for(int i = 0; i < employeedPlayers.Count(); i++) 
        {
            PlayerDetails.Add(new PlayerDetailModel
            {
                Player = employeedPlayers.ElementAt(i),
                Row = i+1
            });
        }
        var freeAgents = State.Players.Where(p => p.Contract == null).OrderByDescending(p => p.Rating).Take(100);
        for (int i = 0; i < freeAgents.Count(); i++)
        {
            PlayerDetails.Add(new PlayerDetailModel
            {
                Player = freeAgents.ElementAt(i),
                Row = i + 1 + employeedPlayers.Count()
            });
        }

        Console.WriteLine("All Players\n");
        Console.WriteLine(string.Format("{0,-5}{1,-35}{2,-10}{3,-25}", "Row", "Player", "Rating", "Team"));
        foreach(var playerDetail in PlayerDetails.OrderBy(p => p.Player.Club))
        {
            var team = playerDetail.Player.Contract == null ? "Free Agent" : playerDetail.Player.Contract!.Club.Name;
            Console.WriteLine($"{playerDetail.Row,-5}{playerDetail.Player.Name,-35}{playerDetail.Player.Rating,-10}{team,-25}");
        }
    }
}
