using FootballManagerSimulator.Enums;
using FootballManagerSimulator.Interfaces;
using FootballManagerSimulator.Structures;

namespace FootballManagerSimulator.Screens;

public class TacticsScreen(IState state,
    ITacticHelper tacticHelper) : BaseScreen(state)
{
    public override ScreenType Screen => ScreenType.Tactics;

    public override void HandleInput(string input)
    {
        var parts = input.Split("->");

        if (parts.Length > 1)
        {
            var fromPlayerIndex = int.Parse(parts[0]) - 1;
            var toPlayerIndex = int.Parse(parts[1]) - 1;

            var fromPlayerId = state.Clubs.First(p => p.Id == state.MyClubId).TacticSlots.ElementAt(fromPlayerIndex).PlayerId;
            var toPlayerId = state.Clubs.First(p => p.Id == state.MyClubId).TacticSlots.ElementAt(toPlayerIndex).PlayerId;

            state.Clubs.First(p => p.Id == state.MyClubId).TacticSlots.ElementAt(toPlayerIndex).PlayerId = fromPlayerId;
            state.Clubs.First(p => p.Id == state.MyClubId).TacticSlots.ElementAt(fromPlayerIndex).PlayerId = toPlayerId;
            return;
        }

        switch (input)
        {
            case "B":
                state.ScreenStack.Pop();
                break;
            case "C":
                tacticHelper.ResetTacticForClub(state.Clubs.First(p => p.Id == state.MyClubId));
                tacticHelper.FillEmptyTacticSlotsByClubId(state.Clubs.First(p => p.Id == state.MyClubId).Id);
                break;
            case "D":
                tacticHelper.ResetTacticForClub(state.Clubs.First(p => p.Id == state.MyClubId));
                break;
            case "E":
                state.ScreenStack.Push(new Structures.Screen
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
        Console.WriteLine($"{"Number",-10}{"Slot",-10}{"Position",-10}{"Name",-40}{"Rating",-10}");
        for(var i = 0; i < state.Clubs.First(p => p.Id == state.MyClubId).TacticSlots.Count; i++)
        {
            var tacticSlot = state.Clubs.First(p => p.Id == state.MyClubId).TacticSlots.ElementAt(i);
            if (tacticSlot.PlayerId == null)
            {
                Console.WriteLine($"{i + 1,-10}{tacticSlot.TacticSlotType,-10}{"",-10}{"EMPTY SLOT",-40}");
                continue;
            }
            var player = state.Players.First(p => p.Id == tacticSlot.PlayerId);
            Console.WriteLine($"{i + 1,-10}{tacticSlot.TacticSlotType,-10}{player.PreferredPosition,-10}{player.Name,-40}{player.Rating,-10}");
        }
    }
}
