using FootballManagerSimulator.Enums;
using FootballManagerSimulator.Interfaces;
using FootballManagerSimulator.Structures;

namespace FootballManagerSimulator.Screens;

public class FixtureScreen : BaseScreen
{
    private readonly IState State;
    private readonly ITacticHelper TacticHelper;

    public FixtureScreen(
        IState state, 
        ITacticHelper tacticHelper) : base(state)
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
                HandleAIClubSelection();
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

    private void HandleAIClubSelection()
    {
        var aiClubs = State.Clubs.Where(p => p != State.MyClub);
        foreach(var aiClub in aiClubs)
        {
            TacticHelper.ResetTacticForClub(aiClub);
            TacticHelper.FillEmptyTacticSlotsByClub(aiClub); 
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
                var homeClub = State.Clubs.Where(p => p == fixture.HomeClub).First();
                var awayClub = State.Clubs.Where(p => p == fixture.AwayClub).First();
                Console.WriteLine($"{homeClub,48} v {awayClub,-48}{"3PM KO",21}");
            }
            Console.WriteLine("\n");
        }
    }
}
