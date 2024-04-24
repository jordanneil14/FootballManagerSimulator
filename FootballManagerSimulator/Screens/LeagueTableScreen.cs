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
    private readonly List<LeagueKeyModel> LeagueKeyModels = new();

    public void GenerateLeagueKeyModels(int currentLeagueId)
    {
        LeagueKeyModels.Clear();
        var leagues = State.Leagues;
        var key = (int)'C';

        foreach (var league in leagues)
        {
            LeagueKeyModels.Add(new LeagueKeyModel
            {
                Key = Convert.ToChar(key),
                League = league,
                IsCurrent = league.Id == currentLeagueId
            });
            key++;
        }
    }

    public class LeagueKeyModel
    {
        public char Key { get; set; }
        public League League { get; set; } = new League();
        public bool IsCurrent { get; set; }
    }

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
        if (input == "B")
        {
            while(true)
            {
                var screen = State.ScreenStack.Peek();
                if (screen.Type != ScreenType.LeagueTable)
                    break;
                State.ScreenStack.Pop();
            }
            return;
        }

        if (input.Length > 1)
        {
            var club = ClubHelper.GetClubByName(input);
            if (club != null)
            {
                State.ScreenStack.Push(ClubScreen.CreateScreen(club));
            }
            return;
        }

        var selectedLeague = LeagueKeyModels.FirstOrDefault(p => p.Key.ToString() == input);
        if (selectedLeague == null)
            return;

        State.ScreenStack.Push(new Screen
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
        var screen = State.ScreenStack.Peek();
        var leagueId = screen.Parameters == null
            ? State.MyClub.LeagueId
            : (screen.Parameters as LeagueTableObj)!.LeagueId;

        GenerateLeagueKeyModels(leagueId);

        var league = State.Leagues.First(p => p.Id == leagueId);
        var leagueTable = league.GenerateLeagueTable(); 

        Console.WriteLine($"{league.Name} League Table\n");
        Console.WriteLine(string.Format("{0,-8}{1,-40}{2,-7}{3,-7}{4,-7}{5,-7}{6,-7}", "Pos", "Team", "Pld", "Pnts", "F", "A", "GD"));
        for (var i = 0; i < leagueTable.Count(); i++)
        {
            var leagueTablePosition = leagueTable.ElementAt(i);
            
            Console.WriteLine(string.Format("{0,-8}{1,-40}{2,-7}{3,-7}{4,-7}{5,-7}{6,-7}", 
                i+1, leagueTablePosition.ClubName, leagueTablePosition.Played, leagueTablePosition.Points, leagueTablePosition.GoalsScored, leagueTablePosition.GoalsConceded, leagueTablePosition.GoalDifference));
            if (ShouldAddSeperator(i + 1, league.Id))
            {
                Console.WriteLine("-------------------------------------------------------------------------------");
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
        
        foreach(var leagueKeyModel in LeagueKeyModels)
        {
            if (leagueKeyModel.IsCurrent) continue;
            Console.WriteLine($"{leagueKeyModel.Key}) View {leagueKeyModel.League.Name}");
        }
        Console.WriteLine("<Enter Club Name>) Go To Club");
    }
}
