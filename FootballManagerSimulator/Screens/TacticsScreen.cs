using FootballManagerSimulator.Enums;
using FootballManagerSimulator.Interfaces;
using FootballManagerSimulator.Models;

namespace FootballManagerSimulator.Screens;

public class TacticsScreen(IState state,
    ITacticHelper tacticHelper) : BaseScreen(state)
{
    private readonly IState State = state;
    private readonly ITacticHelper TacticHelper = tacticHelper;

    public override ScreenType Screen => ScreenType.Tactics;

    public override void HandleInput(string input)
    {
        var parts = input.Split("->");

        if (parts.Length > 1)
        {
            var fromPlayerIndex = int.Parse(parts[0]) - 1;
            var toPlayerIndex = int.Parse(parts[1]) - 1;

            var fromPlayerId = State.Clubs.First(p => p.Id == State.MyClubId).TacticSlots.ElementAt(fromPlayerIndex).PlayerId;
            var toPlayerId = State.Clubs.First(p => p.Id == State.MyClubId).TacticSlots.ElementAt(toPlayerIndex).PlayerId;

            State.Clubs.First(p => p.Id == State.MyClubId).TacticSlots.ElementAt(toPlayerIndex).PlayerId = fromPlayerId;
            State.Clubs.First(p => p.Id == State.MyClubId).TacticSlots.ElementAt(fromPlayerIndex).PlayerId = toPlayerId;
            return;
        }

        switch (input)
        {
            case "B":
                State.ScreenStack.Pop();
                break;
            case "C":
                TacticHelper.ResetTacticForClub(State.Clubs.First(p => p.Id == State.MyClubId));
                TacticHelper.FillEmptyTacticSlotsByClubId(State.Clubs.First(p => p.Id == State.MyClubId).Id);
                break;
            case "D":
                TacticHelper.ResetTacticForClub(State.Clubs.First(p => p.Id == State.MyClubId));
                break;
            case "E":
                State.ScreenStack.Push(new Screen
                {
                    Type = ScreenType.Formation
                });
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
        Console.WriteLine("D) Reset Tactic");
        Console.WriteLine("E) Change Formation");
    }

    public override void RenderSubscreen()
    {
        Console.WriteLine("Tactics\n");
        Console.WriteLine($"{"Number",-10}{"Slot",-10}{"Position",-10}{"Name",-40}{"Rating",-10}");
        Console.WriteLine("----------------------------------------------------------------------------------");

        var hasEmptyReserveSlot = false;
        for (var i = 0; i < State.Clubs.First(p => p.Id == State.MyClubId).TacticSlots.Count; i++)
        {
            var tacticSlot = State.Clubs.First(p => p.Id == State.MyClubId).TacticSlots.ElementAt(i);
            if (tacticSlot.TacticSlotType == TacticSlotType.RES && tacticSlot.PlayerId == null && hasEmptyReserveSlot)
                continue;

            if (tacticSlot.PlayerId == null)
            {
                Console.WriteLine($"{i + 1,-10}{tacticSlot.TacticSlotType,-10}{"",-10}{"EMPTY SLOT",-40}");
                if (tacticSlot.TacticSlotType == TacticSlotType.RES)
                    hasEmptyReserveSlot = true;
                continue;
            }
            var player = State.Players.First(p => p.Id == tacticSlot.PlayerId);
            Console.WriteLine($"{i + 1,-10}{tacticSlot.TacticSlotType,-10}{player.PreferredPosition,-10}{player.Name,-40}{player.Rating,-10}");
        }
    }
}
