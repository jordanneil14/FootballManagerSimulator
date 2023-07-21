using FootballManagerSimulator.Enums;
using FootballManagerSimulator.Interfaces;
using FootballManagerSimulator.Structures;

namespace FootballManagerSimulator.Screens;

public class LeagueTableScreen : BaseScreen
{
    private readonly IState State;
    private readonly IHelperFunction HelperFunction;

    public LeagueTableScreen(IState state, IHelperFunction helperFunction) : base(state)
    {
        State = state;
        HelperFunction = helperFunction;
    }

    public override ScreenType Screen => ScreenType.LeagueTable;

    public override void HandleInput(string input)
    {
        switch (input)
        {
            case "A":
                State.CurrentScreen.Type = ScreenType.Main;
                break;
            default:
                var team = HelperFunction.GetTeamByName(input);
                if (team != null)
                {
                    State.CurrentScreen = ClubScreen.CreateScreen(team);
                    State.CurrentScreen.Type = ScreenType.Club;
                }
                break;
        }
    }

    public override void RenderSubscreen()
    {
        var s = State.Competitions.Where(p => p.Name == "Premier League").First() as League;
        var q = s.LeagueTable;

        Console.WriteLine($"League Table\n");
        Console.WriteLine(string.Format("{0,-10}{1,-20}{2,-10}", "Position", "Team", "Points"));
        for (int i = 0; i < q.Count(); i++)
        {
            var leaguePosition = q.ElementAt(i);
            Console.WriteLine($"{i + 1,-10}{leaguePosition.TeamName,-20}{leaguePosition.Points}");
        }
    }

    public override void RenderOptions()
    {
        Console.WriteLine("Options:");
        Console.WriteLine("A) Back");
        Console.WriteLine("<Enter Club Name>) Go To Club");
    }
}
