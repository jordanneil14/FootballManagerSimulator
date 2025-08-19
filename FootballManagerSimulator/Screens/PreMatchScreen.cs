using FootballManagerSimulator.Enums;
using FootballManagerSimulator.Interfaces;
using FootballManagerSimulator.Models;

namespace FootballManagerSimulator.Screens;

public class PreMatchScreen(
    IState state,
    IMatchSimulatorHelper matchSimulator,
    IPlayerHelper playerHelper) : BaseScreen(state)
{
    private readonly IState State = state;
    private readonly IMatchSimulatorHelper MatchSimulator = matchSimulator;
    private readonly IPlayerHelper PlayerHelper = playerHelper;

    public override ScreenType Screen => ScreenType.PreMatch;

    public override Dictionary<string, string> Options => new() {
        { "A", "Start Match" },
        { "B", "Tactics" },
        { "C", "Back" }
    };

    public override void HandleInput(string input)
    {
        switch (input)
        {
            case "A":
                ValidateStartMatch();
                if (State.UserFeedbackUpdates.Count != 0) return;
                foreach (var comp in State.Competitions)
                {
                    var todaysFixtures = comp.Fixtures.Where(p => p.Date == State.Date);
                    foreach (var fixture in todaysFixtures)
                    {
                        MatchSimulator.ProcessMatch(fixture, comp);
                    }
                }

                State.ScreenStack.Push(new Screen
                {
                    Type = ScreenType.Match
                });
                break;
            case "B":
                State.ScreenStack.Push(new Screen
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

    private void ValidateStartMatch()
    {
        var positions = State.Clubs.First(p => p.Id == State.MyClubId).TacticSlots.Where(p => p.TacticSlotType != TacticSlotType.SUB && p.TacticSlotType != TacticSlotType.RES);
        if (positions.Where(p => p.PlayerId == null).Any())
            State.UserFeedbackUpdates.Add("Unable to start game. Your team has not been fully selected");
    }

    public override void RenderSubscreen()
    {
        var fixture = State.Competitions
            .SelectMany(p => p.Fixtures)
            .First(p => p.Date == State.Date && (p.HomeClub.Id == State.MyClubId || p.AwayClub.Id == State.MyClubId));

        var homeClub = State.Clubs
            .Where(p => p.Id == fixture.HomeClub.Id)
            .First();

        var awayClub = State.Clubs
            .Where(p => p.Id == fixture.AwayClub.Id)
            .First();

        Console.WriteLine($"{homeClub.Name,58} v {awayClub.Name,-58}\n");

        var homeClubPlayers = State.Clubs.First(p => p.Id == homeClub.Id).TacticSlots;
        var awayClubPlayers = State.Clubs.First(p => p.Id == awayClub.Id).TacticSlots;

        for (var i = 0; i < 18; i++)
        {
            if (i == 11)
                Console.WriteLine($"{"------------",58}{"   ------------",-58}");

            var homePlayer = "EMPTY SLOT";
            var awayPlayer = "EMPTY SLOT";

            var tacticSlotHome = homeClubPlayers.ElementAt(i);
            if (tacticSlotHome.PlayerId != null)
            {
                var player = PlayerHelper.GetPlayerById(tacticSlotHome.PlayerId.Value)!;
                homePlayer = $"{player.Name,55}{player.ShirtNumber,3}";
            }

            var tacticSlotAway = awayClubPlayers.ElementAt(i);
            if (tacticSlotAway.PlayerId != null)
            {
                var player = PlayerHelper.GetPlayerById(tacticSlotAway.PlayerId.Value)!;
                awayPlayer = $"{player.ShirtNumber,-3}{player.Name,-55}";
            }

            Console.WriteLine($"{homePlayer,58}   {awayPlayer,-58}");
        }
    }
}
