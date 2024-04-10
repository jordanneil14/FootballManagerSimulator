using FootballManagerSimulator.Enums;
using FootballManagerSimulator.Interfaces;
using FootballManagerSimulator.Structures;
using Microsoft.Extensions.Options;

namespace FootballManagerSimulator.Screens;

public class LeagueTableScreen : BaseScreen
{
    private readonly IState State;
    private readonly IClubHelper ClubHelper;
    private readonly Settings Settings;

    public LeagueTableScreen(
        IState state, 
        IClubHelper clubHelper,
        IOptions<Settings> settings) : base(state)
    {
        State = state;
        ClubHelper = clubHelper;
        Settings = settings.Value;
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
        Console.WriteLine(string.Format("{0,-8}{1,-25}{2,-7}{3,-7}{4,-7}{5,-7}{6,-7}", "Pos", "Team", "Pld", "Pnts", "F", "A", "GD"));
        for (var i = 0; i < leagueTable.Count(); i++)
        {
            var leagueTablePosition = leagueTable.ElementAt(i);
            
            Console.WriteLine(string.Format("{0,-8}{1,-25}{2,-7}{3,-7}{4,-7}{5,-7}{6,-7}", 
                i+1, leagueTablePosition.ClubName, leagueTablePosition.Played, leagueTablePosition.Points, leagueTablePosition.GoalsScored, leagueTablePosition.GoalsConceded, leagueTablePosition.GoalDifference));
            if (ShouldAddSeperator(i + 1, league.Id))
            {
                Console.WriteLine("----------------------------------------------------------------");
            }
        }
    }

    private bool ShouldAddSeperator(int index, int leagueId)
    {
        var leagueTableSettings = Settings.Leagues.First(p => p.Id == leagueId).LeagueTable;

        if (leagueTableSettings.AutomaticPromotionPlaces == index)
            return true;

        if (leagueTableSettings.PlayoffPlaces + leagueTableSettings.AutomaticPromotionPlaces == index)
            return true;

        if (leagueTableSettings.Places - leagueTableSettings.RelegationPlaces == index)
            return true;

        return false;
    }

    public override void RenderOptions()
    {
        Console.WriteLine("Options:");
        Console.WriteLine("B) Back");
        Console.WriteLine("<Enter Club Name>) Go To Club");
    }
}
