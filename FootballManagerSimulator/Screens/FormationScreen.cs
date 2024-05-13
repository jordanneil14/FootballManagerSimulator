using FootballManagerSimulator.Enums;
using FootballManagerSimulator.Helpers;
using FootballManagerSimulator.Interfaces;
using FootballManagerSimulator.Structures;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FootballManagerSimulator.Screens;

public class FormationScreen(
    IState state,
    ITacticHelper tacticHelper) : BaseScreen(state)
{
    public override ScreenType Screen => ScreenType.Formation;

    public override void HandleInput(string input)
    {
        switch(input)
        {
            case "B":
                state.ScreenStack.Pop();
                break;
            case "C":
                state.MyClub.Formation = "4-4-3";
                tacticHelper.ResetTacticForClub(state.MyClub);
                state.ScreenStack.Pop();
                break;
            case "D":
                state.MyClub.Formation = "4-4-2";
                tacticHelper.ResetTacticForClub(state.MyClub);
                state.ScreenStack.Pop();
                break;
            case "E":
                state.MyClub.Formation = "4-5-1";
                tacticHelper.ResetTacticForClub(state.MyClub);
                state.ScreenStack.Pop();
                break;
            case "F":
                state.MyClub.Formation = "4-1-2-1-2";
                tacticHelper.ResetTacticForClub(state.MyClub);
                state.ScreenStack.Pop();
                break;
            default:
                break;
        }
    }

    public override void RenderOptions()
    {
        Console.WriteLine("Options:");
        Console.WriteLine("B) Back");
        Console.WriteLine("C) Select 4-3-3");
        Console.WriteLine("D) Select 4-4-2");
        Console.WriteLine("E) Select 4-5-1");
        Console.WriteLine("F) Select 4-1-2-1-2");
    }

    public override void RenderSubscreen()
    {
        Console.WriteLine($"Current Formation is: {state.MyClub.Formation}");
    }
}
