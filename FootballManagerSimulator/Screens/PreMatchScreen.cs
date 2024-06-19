using FootballManagerSimulator.Enums;
using FootballManagerSimulator.Interfaces;

namespace FootballManagerSimulator.Screens;

public class PreMatchScreen(
    IState state,
    IMatchSimulatorHelper matchSimulator,
    IPlayerHelper playerHelper) : BaseScreen(state)
{
    public override ScreenType Screen => ScreenType.PreMatch;

    public override void HandleInput(string input)
    {
        switch (input)
        {
            case "A":
                ValidateStartMatch();
                if (state.UserFeedbackUpdates.Any()) return;
                var fixtures = state.TodaysFixtures.SelectMany(p => p.Fixtures);
                foreach (var fixture in fixtures)
                {
                    matchSimulator.ProcessMatch(fixture);
                }
                state.ScreenStack.Push(new Structures.Screen
                {
                    Type = ScreenType.HalfTime
                });
                break;
            case "B":
                state.ScreenStack.Push(new Structures.Screen
                {
                    Type = ScreenType.Tactics
                });
                break;
            case "C":
                state.ScreenStack.Pop();
                break;
            default:
                break;
        }
    }

    private void ValidateStartMatch()
    {
        var positions = state.Clubs.First(p => p.Id == state.MyClubId).TacticSlots.Where(p => p.TacticSlotType != TacticSlotType.SUB && p.TacticSlotType != TacticSlotType.RES);
        if (positions.Where(p => p.PlayerId == null).Any())
            state.UserFeedbackUpdates.Add("Unable to start game. Your team has not been fully selected");
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
        var fixture = state.TodaysFixtures
            .SelectMany(p => p.Fixtures)
            .Where(p => p.HomeClub.Id == state.Clubs.First(p => p.Id == state.MyClubId).Id || p.AwayClub.Id == state.Clubs.First(p => p.Id == state.MyClubId).Id)
            .First();

        var homeClub = state.Clubs
            .Where(p => p.Id == fixture.HomeClub.Id)
            .First();

        var awayClub = state.Clubs
            .Where(p => p.Id == fixture.AwayClub.Id)
            .First();

        Console.WriteLine($"{homeClub.Name, 58} v {awayClub.Name, -58}\n");

        var homeClubPlayers = state.Clubs.First(p => p.Id == homeClub.Id).TacticSlots;
        var awayClubPlayers = state.Clubs.First(p => p.Id == awayClub.Id).TacticSlots;

        for (var i = 0; i < 18; i++)
        {
            if (i == 11)
                Console.WriteLine($"{"------------",58}{"   ------------",-58}");

            var homePlayer = "EMPTY SLOT";
            var awayPlayer = "EMPTY SLOT";

            var tacticSlotHome = homeClubPlayers.ElementAt(i);
            if (tacticSlotHome.PlayerId != null)
            {
                var player = playerHelper.GetPlayerById(tacticSlotHome.PlayerId.Value)!;
                homePlayer = $"{player.Name,55}{player.ShirtNumber,3}";
            }

            var tacticSlotAway = awayClubPlayers.ElementAt(i);
            if (tacticSlotAway.PlayerId != null)
            {
                var player = playerHelper.GetPlayerById(tacticSlotAway.PlayerId.Value)!;
                awayPlayer = $"{player.ShirtNumber,-3}{player.Name,-55}";
            }

            Console.WriteLine($"{homePlayer,58}   {awayPlayer,-58}");
        }
    }
}
