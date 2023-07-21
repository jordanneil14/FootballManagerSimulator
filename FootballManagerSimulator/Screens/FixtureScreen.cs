using FootballManagerSimulator.Enums;
using FootballManagerSimulator.Interfaces;

namespace FootballManagerSimulator.Screens;

public class FixtureScreen : BaseScreen
{
    private readonly IState State;
    private readonly ITacticHelper TacticHelper;

    public FixtureScreen(IState state, ITacticHelper tacticHelper) : base(state)
    {
        State = state;
        TacticHelper = tacticHelper;
    }

    public override ScreenType Screen => ScreenType.Fixture;

    public override void HandleInput(string input)
    {
        switch (input)
        {
            case "A":
                State.CurrentScreen.Type = ScreenType.PreMatch;
                HandleAITeamSelection();
                break;
            case "B":
                State.CurrentScreen.Type = ScreenType.Main;
                break;
            default:
                break;
        }
    }

    public override void RenderOptions()
    {
        Console.WriteLine("Options:");
        Console.WriteLine("A) Advance");
        Console.WriteLine("B) Back");
    }

    private void HandleAITeamSelection()
    {
        var aiTeams = State.Teams.Where(p => p != State.MyTeam);

        foreach(var aiTeam in aiTeams)
        {
            TacticHelper.ResetTacticForTeam(aiTeam);
            TacticHelper.PickTacticSlots(aiTeam); 
        }
    }

    public override void RenderSubscreen()
    {
        Console.WriteLine("Todays Fixtures\n");
        foreach(var fixture in State.TodaysFixtures) 
        {
            var homeTeam = State.Teams.Where(p => p == fixture.HomeTeam).First();
            var awayTeam = State.Teams.Where(p => p == fixture.AwayTeam).First();
            Console.WriteLine($"{homeTeam,45} v {awayTeam,-45}");
        }
    }
}
