using FootballManagerSimulator.Enums;
using FootballManagerSimulator.Interfaces;
using FootballManagerSimulator.Structures;

namespace FootballManagerSimulator.Screens;

public class LeagueTableScreen : BaseScreen
{
    private readonly IState State;
    private readonly IClubHelper ClubHelper;

    public LeagueTableScreen(
        IState state, 
        IClubHelper clubHelper) : base(state)
    {
        State = state;
        ClubHelper = clubHelper;
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
                var club = ClubHelper.GetClubByName(input);
                if (club != null)
                {
                    State.ScreenStack.Push(ClubScreen.CreateScreen(club));
                }
                break;
        }
    }

    public override void RenderSubscreen()
    {
        var league = State.Leagues.First(p => p.Id == State.MyClub.LeagueId) as League;
        var leagueTable = league.GenerateLeagueTable(); 

        Console.WriteLine($"League Table\n");
        Console.WriteLine(string.Format("{0,-8}{1,-20}{2,-9}{3,-9}{4,-9}{5,-9}{6,-9}", "Pos", "Team", "Pld", "Points", "For", "Against", "GD"));
        for (var i = 0; i < leagueTable.Count(); i++)
        {
            var leagueTablePosition = leagueTable.ElementAt(i);
            Console.WriteLine(string.Format("{0,-8}{1,-20}{2,-9}{2,-9}{3,-9}{4,-9}{5,-9}", 
                i+1, leagueTablePosition.ClubName, leagueTablePosition.Played, leagueTablePosition.Points, leagueTablePosition.GoalsScored, leagueTablePosition.GoalsConceded, leagueTablePosition.GoalDifference));
        }
    }

    public override void RenderOptions()
    {
        Console.WriteLine("Options:");
        Console.WriteLine("B) Back");
        Console.WriteLine("<Enter Club Name>) Go To Club");
    }
}
