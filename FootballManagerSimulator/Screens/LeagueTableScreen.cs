using FootballManagerSimulator.Enums;
using FootballManagerSimulator.Interfaces;
using FootballManagerSimulator.Structures;

namespace FootballManagerSimulator.Screens;

public class LeagueTableScreen : BaseScreen
{
    private readonly IState State;
    private readonly IUtils HelperFunction;

    public LeagueTableScreen(
        IState state, 
        IUtils helperFunction) : base(state)
    {
        State = state;
        HelperFunction = helperFunction;
    }

    public override ScreenType Screen => ScreenType.LeagueTable;

    public override void HandleInput(string input)
    {
        switch (input)
        {
            case "B":
                State.ScreenStack.Pop();
                break;
            default:
                var team = HelperFunction.GetTeamByName(input);
                if (team != null)
                {
                    State.ScreenStack.Push(ClubScreen.CreateScreen(team));
                }
                break;
        }
    }

    public override void RenderSubscreen()
    {
        var league = State.Competitions.First(p => p.ID == State.MyTeam.CompetitionID) as League;
        var leagueTable = league.GenerateLeagueTable(); 

        Console.WriteLine($"League Table\n");
        Console.WriteLine(string.Format("{0,-10}{1,-20}{2,-10}", "Position", "Team", "Points"));
        for (int i = 0; i < leagueTable.Count(); i++)
        {
            var leagueTablePosition = leagueTable.ElementAt(i);
            Console.WriteLine($"{i + 1,-10}{leagueTablePosition.TeamName,-20}{leagueTablePosition.Points}");
        }
    }

    public override void RenderOptions()
    {
        Console.WriteLine("Options:");
        Console.WriteLine("B) Back");
        Console.WriteLine("<Enter Club Name>) Go To Club");
    }
}
