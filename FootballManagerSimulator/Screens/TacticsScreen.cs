using FootballManagerSimulator.Enums;
using FootballManagerSimulator.Interfaces;

namespace FootballManagerSimulator.Screens;

public class TacticsScreen : BaseScreen
{
    private readonly IState State;
    private readonly ITacticHelper TacticHelper;
    private readonly IUtils Utils;

    public TacticsScreen(IState state, 
        ITacticHelper tacticHelper,
        IUtils utils) : base(state)
    {
        State = state;
        TacticHelper = tacticHelper;
        Utils = utils;
    }

    public override ScreenType Screen => ScreenType.Tactics;

    public override void HandleInput(string input)
    {
        var parts = input.Split("->");

        if (parts.Length > 1)
        {
            var fromPlayerIndex = int.Parse(parts[0]) - 1;
            var toPlayerIndex = int.Parse(parts[1]) - 1;

            var fromPlayerId = State.MyClub.TacticSlots.ElementAt(fromPlayerIndex).PlayerID;
            var toPlayerId = State.MyClub.TacticSlots.ElementAt(toPlayerIndex).PlayerID;

            State.MyClub.TacticSlots.ElementAt(toPlayerIndex).PlayerID = fromPlayerId;
            State.MyClub.TacticSlots.ElementAt(fromPlayerIndex).PlayerID = toPlayerId;
            return;
        }

        switch (input)
        {
            case "B":
                State.ScreenStack.Pop();
                break;
            case "C":
                TacticHelper.ResetTacticForClub(State.MyClub);
                TacticHelper.FillEmptyTacticSlotsByClub(State.MyClub);
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
        Console.WriteLine($"{"Number",-10}{"Slot",-10}{"Position",-10}{"Name",-40}{"Rating",-10}");
        for(var i = 0; i < State.MyClub.TacticSlots.Count; i++)
        {
            var tacticSlot = State.MyClub.TacticSlots.ElementAt(i);
            if (tacticSlot.PlayerID == null)
            {
                Console.WriteLine($"{i + 1,-10}{tacticSlot.TacticSlotType,-10}{"",-10}{"EMPTY SLOT",-40}");
                continue;
            }
            var player = State.Players.First(p => p.ID == tacticSlot.PlayerID);
            Console.WriteLine($"{i + 1,-10}{tacticSlot.TacticSlotType,-10}{player.Position,-10}{player.Name,-40}{player.Rating,-10}");
        }
    }
}
