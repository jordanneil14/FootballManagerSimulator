using FootballManagerSimulator.Enums;
using FootballManagerSimulator.Interfaces;
using FootballManagerSimulator.Structures;

namespace FootballManagerSimulator.Screens;

public class PostMatchLeagueTableScreen : BaseScreen
{
    private readonly IState State;

    public PostMatchLeagueTableScreen(IState state) : base(state)
    {
        State = state;
    }

    public override ScreenType Screen => ScreenType.PostMatchLeagueTable;

    public override void HandleInput(string input)
    {
        switch (input)
        {
            case "B":
                State.ScreenStack.Push(new Screen
                {
                    Type = ScreenType.FullTime
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
    }

    public override void RenderSubscreen()
    {
        var league = State.Competitions.First(p => p.ID == State.MyClub.CompetitionID) as League;
        var leagueTable = league.GenerateLeagueTable();

        Console.WriteLine($"League Table\n");
        Console.WriteLine(string.Format("{0,-8}{1,-20}{2,-8}{3,-12}{4,-12}{5,-12}", "Pos", "Team", "Points", "Gls Scored", "Gls Con", "Goal Diff"));
        for (int i = 0; i < leagueTable.Count(); i++)
        {
            var leagueTablePosition = leagueTable.ElementAt(i);
            Console.WriteLine(string.Format("{0,-8}{1,-20}{2,-8}{3,-12}{4,-12}{5,-12}",
                i + 1, leagueTablePosition.TeamName, leagueTablePosition.Points, leagueTablePosition.GoalsScored, leagueTablePosition.GoalsConceded, leagueTablePosition.GoalDifference));
        }
    }
}
