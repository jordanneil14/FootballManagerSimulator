using FootballManagerSimulator.Enums;
using FootballManagerSimulator.Interfaces;
using FootballManagerSimulator.Models;
using FootballManagerSimulator.Structures;
using Microsoft.Extensions.Options;

namespace FootballManagerSimulator.Screens;

public class LeagueTableScreen(
    IState state,
    IClubHelper clubHelper,
    IOptions<Settings> settings) : BaseScreen(state)
{
    private readonly Settings Settings = settings.Value;
    private readonly List<LeagueKeyModel> LeagueKeyModels = new();

    public void GenerateLeagueKeyModels(int currentLeagueId)
    {
        LeagueKeyModels.Clear();
        var leagues = state.Competitions.Where(p => p.Type.ToString() == "League");
        var key = (int)'C';

        foreach (var league in leagues)
        {
            LeagueKeyModels.Add(new LeagueKeyModel
            {
                Key = Convert.ToChar(key),
                League = (League)league,
                IsCurrent = league.Id == currentLeagueId
            });
            key++;
        }
    }

    public override ScreenType Screen => ScreenType.LeagueTable;

    public override void HandleInput(string input)
    {
        if (input == "B")
        {
            while(true)
            {
                var screen = state.ScreenStack.Peek();
                if (screen.Type != ScreenType.LeagueTable)
                    break;
                state.ScreenStack.Pop();
            }
            return;
        }

        if (input.Length > 1)
        {
            var club = clubHelper.GetClubByName(input);
            if (club != null)
            {
                state.ScreenStack.Push(ClubScreen.CreateScreen(club));
            }
            return;
        }

        var selectedLeague = LeagueKeyModels.FirstOrDefault(p => p.Key.ToString() == input);
        if (selectedLeague == null)
            return;

        state.ScreenStack.Push(new Screen
        {
            Type = ScreenType.LeagueTable,
            Parameters = new LeagueTableObj
            {
                LeagueId = selectedLeague.League.Id
            }
        });
    }

    public class LeagueTableObj
    {
        public int LeagueId { get; set; }
    }

    public override void RenderSubscreen()
    {
        var screen = state.ScreenStack.Peek();
        var leagueId = screen.Parameters == null
            ? state.Clubs.First(p => p.Id == state.MyClubId).LeagueId
            : (screen.Parameters as LeagueTableObj)!.LeagueId;

        GenerateLeagueKeyModels(leagueId);

        var league = state.Competitions.First(p => p.Id == leagueId) as League;
        var leagueTable = league.GenerateLeagueTable(); 

        Console.WriteLine($"{league.Name} League Table\n");
        Console.WriteLine(string.Format("{0,-8}{1,-40}{2,-7}{3,-7}{4,-7}{5,-7}{6,-7}", "Pos", "Team", "Pld", "Pnts", "F", "A", "GD"));
        Console.WriteLine("-------------------------------------------------------------------------------");

        for (var i = 0; i < leagueTable.Count(); i++)
        {
            var leagueTablePosition = leagueTable.ElementAt(i);
            
            Console.WriteLine(string.Format("{0,-8}{1,-40}{2,-7}{3,-7}{4,-7}{5,-7}{6,-7}", 
                i+1, leagueTablePosition.Club.Name, leagueTablePosition.Played, leagueTablePosition.Points, leagueTablePosition.GoalsScored, leagueTablePosition.GoalsConceded, leagueTablePosition.GoalDifference));
            if (ShouldAddSeperator(i + 1, league.Id))
            {
                Console.WriteLine("-------------------------------------------------------------------------------");
            }
        }
    }

    private bool ShouldAddSeperator(int index, int leagueId)
    {
        var leagueTableSettings = Settings.Competitions.First(p => p.Id == leagueId).LeagueTable;

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
        
        foreach(var leagueKeyModel in LeagueKeyModels)
        {
            if (leagueKeyModel.IsCurrent) continue;
            Console.WriteLine($"{leagueKeyModel.Key}) View {leagueKeyModel.League.Name}");
        }
        Console.WriteLine("<Enter Club Name>) Go To Club");
    }
}
