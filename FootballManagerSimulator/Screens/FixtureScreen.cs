using FootballManagerSimulator.Enums;
using FootballManagerSimulator.Interfaces;
using FootballManagerSimulator.Structures;

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
                State.ScreenStack.Push(new Screen
                {
                    Type = ScreenType.PreMatch
                });
                HandleAITeamSelection();
                break;
            case "B":
                State.ScreenStack.Pop();
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
        var aiTeams = State.Clubs.Where(p => p != State.MyClub);

        foreach(var aiTeam in aiTeams)
        {
            TacticHelper.ResetTacticForTeam(aiTeam);
            TacticHelper.PickTacticSlots(aiTeam); 
        }
    }

    public override void RenderSubscreen()
    {
        Console.WriteLine("Today's Fixtures\n");
        var groupedFixtures = State.TodaysFixtures;
        foreach(var group in groupedFixtures)
        {
            Console.WriteLine(group.Competition.Name);
            foreach (var fixture in group.Fixtures)
            {
                var homeTeam = State.Clubs.Where(p => p == fixture.HomeTeam).First();
                var awayTeam = State.Clubs.Where(p => p == fixture.AwayTeam).First();
                Console.WriteLine($"{homeTeam,48} v {awayTeam,-48}{"3PM KO",21}");
            }
            Console.WriteLine("\n");
        }
    }
}
