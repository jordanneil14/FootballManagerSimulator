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
        Console.WriteLine($"{"Pos",-8}{"Team",-20}{"Points",-9}{"Scored",-9}{"Conceded",-9}{"Diff",-9}");
        for (var i = 0; i < leagueTable.Count(); i++)
        {
            var leagueTablePosition = leagueTable.ElementAt(i);
            Console.WriteLine($"{i + 1,-8}{leagueTablePosition.ClubName,-20}{leagueTablePosition.Points,-9}{leagueTablePosition.GoalsScored,-9}{leagueTablePosition.GoalsConceded,-9}{leagueTablePosition.GoalDifference,-9}");
        }
    }
}
