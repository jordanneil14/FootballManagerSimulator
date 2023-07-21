using FootballManagerSimulator.Enums;
using FootballManagerSimulator.Interfaces;
using FootballManagerSimulator.Structures;

namespace FootballManagerSimulator.Screens;

public class PostMatchLeagueTableScreen : BaseScreen
{
    private readonly IState State;
    private readonly ITacticHelper TacticHelper;

    public PostMatchLeagueTableScreen(IState state, ITacticHelper tacticHelper) : base(state)
    {
        State = state;
        TacticHelper = tacticHelper;
    }

    public override ScreenType Screen => ScreenType.PostMatchLeagueTable;

    public override void HandleInput(string input)
    {
        switch (input)
        {
            case "A":
                State.CurrentScreen.Type = ScreenType.FullTime;
                break;
            default:
                break;
        }
    }

    public override void RenderOptions()
    {
        Console.WriteLine("Options:");
        Console.WriteLine("A) Back");
    }

    public override void RenderSubscreen()
    {
        var competition = State.Competitions.Where(p => p.Name == "Premier League").First() as League;
        var leagueTable = competition.LeagueTable;

        Console.WriteLine($"League Table\n");
        Console.WriteLine(string.Format("{0,-10}{1,-20}{2,-10}", "Position", "Team", "Points"));
        for (int i = 0; i < leagueTable.Count(); i++)
        {
            var leaguePosition = leagueTable.ElementAt(i);
            Console.WriteLine($"{i + 1,-10}{leaguePosition.TeamName,-20}{leaguePosition.Points}");
        }
    }
}
