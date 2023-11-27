using FootballManagerSimulator.Enums;
using FootballManagerSimulator.Interfaces;
using FootballManagerSimulator.Structures;

namespace FootballManagerSimulator.Screens;

public class LeagueTableScreen : BaseScreen
{
    private readonly IState State;
    private readonly IUtils Utils;

    public LeagueTableScreen(
        IState state, 
        IUtils utils) : base(state)
    {
        State = state;
        Utils = utils;
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
                var club = Utils.GetClubByName(input);
                if (club != null)
                {
                    State.ScreenStack.Push(ClubScreen.CreateScreen(club));
                }
                break;
        }
    }

    public override void RenderSubscreen()
    {
        var league = State.Competitions.First(p => p.ID == State.MyClub.CompetitionID) as League;
        var leagueTable = league.GenerateLeagueTable(); 

        Console.WriteLine($"League Table\n");
        Console.WriteLine(string.Format("{0,-8}{1,-20}{2,-8}{3,-12}{4,-12}{5,-12}", "Pos", "Team", "Points", "Gls Scored", "Gls Con", "Goal Diff"));
        for (var i = 0; i < leagueTable.Count(); i++)
        {
            var leagueTablePosition = leagueTable.ElementAt(i);
            Console.WriteLine(string.Format("{0,-8}{1,-20}{2,-8}{3,-12}{4,-12}{5,-12}", 
                i+1, leagueTablePosition.ClubName, leagueTablePosition.Points, leagueTablePosition.GoalsScored, leagueTablePosition.GoalsConceded, leagueTablePosition.GoalDifference));
        }
    }

    public override void RenderOptions()
    {
        Console.WriteLine("Options:");
        Console.WriteLine("B) Back");
        Console.WriteLine("<Enter Club Name>) Go To Club");
    }
}
