using FootballManagerSimulator.Enums;
using FootballManagerSimulator.Interfaces;

namespace FootballManagerSimulator.Screens;

public class TacticsScreen : BaseScreen
{
    private readonly IState State;
    private readonly ITacticHelper TacticHelper;

    public TacticsScreen(IState state, ITacticHelper tacticHelper) : base(state)
    {
        State = state;
        TacticHelper = tacticHelper;
    }

    public override ScreenType Screen => ScreenType.Tactics;

    public override void HandleInput(string input)
    {
        var parts = input.Split("->");

        if (parts.Length > 1)
        {
            var fromPlayerID = int.Parse(parts[0]) - 1;
            var toPlayerID = int.Parse(parts[1]) - 1;

            var fromPlayer = State.MyTeam.TacticSlots.ElementAt(fromPlayerID).Player;
            var toPlayer = State.MyTeam.TacticSlots.ElementAt(toPlayerID).Player;

            State.MyTeam.TacticSlots.ElementAt(toPlayerID).Player = fromPlayer;
            State.MyTeam.TacticSlots.ElementAt(fromPlayerID).Player = toPlayer;
            return;
        }

        switch (input)
        {
            case "B":
                State.ScreenStack.Pop();
                break;
            case "C":
                TacticHelper.ResetTacticForTeam(State.MyTeam);
                TacticHelper.PickTacticSlots(State.MyTeam);
                break;
            default:
                break;
        }
    }

    public override void RenderOptions()
    {
        Console.WriteLine("Options:");
        Console.WriteLine("B) Back");
        Console.WriteLine("<Enter Number> -> <Enter Number>) Switch Places");
        Console.WriteLine("C) Get Assistant To Pick Team");
    }

    public override void RenderSubscreen()
    {
        Console.WriteLine(string.Format("{0,-10}{1,-10}{2,-10}{3,-40}{4,-10}", "Number", "Slot", "Position", "Name", "Rating"));
        for(int i = 0; i < State.MyTeam.TacticSlots.Count; i++)
        {
            var element = State.MyTeam.TacticSlots.ElementAt(i);
            if (element?.Player != null)
            {
                Console.WriteLine($"{i + 1,-10}{element.TacticSlotType,-10}{element.Player.Position,-10}{element.Player.Name,-40}{element.Player.Rating,-10}");
            }
            else
            {
                Console.WriteLine($"{i + 1,-10}{element.TacticSlotType,-10}{"",-10}{"EMPTY SLOT",-40}");
            }
        }   
    }
}
