using FootballManagerSimulator.Enums;
using FootballManagerSimulator.Helpers;
using FootballManagerSimulator.Interfaces;

namespace FootballManagerSimulator.Screens;

public class PreMatchScreen : BaseScreen
{
    private readonly IState State;
    private readonly IMatchSimulator MatchSimulator;
    private readonly IUtils Utils;

    public PreMatchScreen(IState state, 
        IMatchSimulator matchSimulator,
        IUtils utils) : base(state)
    {
        State = state;
        MatchSimulator = matchSimulator;
        Utils = utils;
    }

    public override ScreenType Screen => ScreenType.PreMatch;

    public override void HandleInput(string input)
    {
        switch (input)
        {
            case "A":
                var result = ValidateStartMatch();
                if (result != null)
                {
                    State.UserFeedbackUpdates.Add(result);
                    return;
                }
                foreach(var fixture in State.TodaysFixtures.SelectMany(p => p.Fixtures))
                {
                    MatchSimulator.SimulateFirstHalf(fixture);
                }
                State.ScreenStack.Push(new Structures.Screen
                {
                    Type = ScreenType.HalfTime
                });
                break;
            case "B":
                State.ScreenStack.Push(new Structures.Screen
                {
                    Type = ScreenType.Tactics
                });
                break;
            case "C":
                State.ScreenStack.Pop();
                break;
            default:
                break;
        }
    }

    private string? ValidateStartMatch()
    {
        var positions = State.MyClub.TacticSlots.Where(p => p.TacticSlotType != TacticSlotType.SUB && p.TacticSlotType != TacticSlotType.RES);
        if (positions.Where(p => p.PlayerID == null).Any())
        {
            return "Unable to start game. Your team has not been fully selected";
        }
        return null;
    }

    public override void RenderOptions()
    {
        Console.WriteLine("Options:");
        Console.WriteLine("A) Start Match");
        Console.WriteLine("B) Tactics");
        Console.WriteLine("C) Back");
    }

    public override void RenderSubscreen()
    {
        var fixture = State.TodaysFixtures.SelectMany(p => p.Fixtures).Where(p => p.HomeClub == State.MyClub || p.AwayClub == State.MyClub).First();
        var homeClub = State.Clubs.Where(p => p == fixture.HomeClub).First();
        var awayClub = State.Clubs.Where(p => p == fixture.AwayClub).First();

        Console.WriteLine($"{homeClub, 58} v {awayClub, -58}\n");

        var homeClubPlayers = State.Clubs.Where(p => p == homeClub).First().TacticSlots;
        var awayClubPlayers = State.Clubs.Where(p => p == awayClub).First().TacticSlots;

        for (var i = 0; i < 18; i++)
        {
            if (i == 11)
                Console.WriteLine($"{"------------",58}{"   ------------",-58}");

            var homePlayer = "EMPTY SLOT";
            var awayPlayer = "EMPTY SLOT";

            var tacticSlotHome = homeClubPlayers.ElementAt(i);
            if (tacticSlotHome.PlayerID != null)
            {
                var player = Utils.GetPlayerById(tacticSlotHome.PlayerID.Value)!;
                homePlayer = $"{player.Name,55}{player.ShirtNumber,3}";
            }

            var tacticSlotAway = awayClubPlayers.ElementAt(i);
            if (tacticSlotAway.PlayerID != null)
            {
                var player = Utils.GetPlayerById(tacticSlotAway.PlayerID.Value)!;
                awayPlayer = $"{player.ShirtNumber,-3}{player.Name,-55}";
            }

            Console.WriteLine($"{homePlayer,58}   {awayPlayer,-58}");
        }
    }
}
