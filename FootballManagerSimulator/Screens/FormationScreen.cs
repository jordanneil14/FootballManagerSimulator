using FootballManagerSimulator.Enums;
using FootballManagerSimulator.Interfaces;

namespace FootballManagerSimulator.Screens;

public class FormationScreen(
    IState state,
    ITacticHelper tacticHelper) : BaseScreen(state)
{
    private readonly IState State = state;
    private readonly ITacticHelper TacticHelper = tacticHelper;

    public override ScreenType Screen => ScreenType.Formation;

    public override Dictionary<string, string> Options => new() {
        { "C", "Select 4-3-3" },
        { "D", "Select 4-4-2" },
        { "E", "Select 4-5-1" },
        { "F", "Select 4-1-2-1-2" }
    };

	public override string? OptionPrompt => null;

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
			case "B":
                State.ScreenStack.Pop();
                break;
            case "C":
                State.Clubs.First(p => p.Id == State.MyClubId).Formation = "4-3-3";
                TacticHelper.ResetTacticForClub(State.Clubs.First(p => p.Id == State.MyClubId));
                State.ScreenStack.Pop();
                break;
            case "D":
                State.Clubs.First(p => p.Id == State.MyClubId).Formation = "4-4-2";
                TacticHelper.ResetTacticForClub(State.Clubs.First(p => p.Id == State.MyClubId));
                State.ScreenStack.Pop();
                break;
            case "E":
                State.Clubs.First(p => p.Id == State.MyClubId).Formation = "4-5-1";
                TacticHelper.ResetTacticForClub(State.Clubs.First(p => p.Id == State.MyClubId));
                State.ScreenStack.Pop();
                break;
            case "F":
                State.Clubs.First(p => p.Id == State.MyClubId).Formation = "4-1-2-1-2";
                TacticHelper.ResetTacticForClub(State.Clubs.First(p => p.Id == State.MyClubId));
                State.ScreenStack.Pop();
                break;
            default:
                break;
        }
    }

    public override void RenderSubscreen()
    {
        Console.WriteLine($"Current Formation is: {State.Clubs.First(p => p.Id == State.MyClubId).Formation}");
    }
}
