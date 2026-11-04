using FootballManagerSimulator.Maui.Enums;
using FootballManagerSimulator.Maui.Helpers;
using FootballManagerSimulator.Maui.Interfaces;
using FootballManagerSimulator.Maui.Models;
using FootballManagerSimulator.Tests.Support;
using Xunit.Abstractions;

namespace FootballManagerSimulator.Tests.Services;

// Plays thousands of matches through the real engine to check that every team instruction is a
// trade-off: it helps in some situations and hurts in others. Run with RUN_SIMULATIONS=1.
public class TacticsBalanceSimulationTests(ITestOutputHelper output)
{
    private const int Matches = 20_000;

    // Our side's rating against an opponent rated 70, giving a strength share (home) of about
    // 0.35 (underdog), 0.50 (even) and 0.65 (favourite).
    private const int UnderdogRating = 53;
    private const int EvenRating = 65;
    private const int FavouriteRating = 80;

    private sealed record Result(double PointsPerGame, double WinRate, double DrawRate, double GoalsPerMatch);

    // switchTo / switchMinute: we change our instructions mid-match, as the user can from the match screen.
    private static Result Play(int ourRating, TeamInstructionsModel ours, int ourStamina = 65,
        TeamInstructionsModel? theirs = null, int matches = Matches,
        TeamInstructionsModel? switchTo = null, int switchMinute = 0)
    {
        var state = TestData.StateWithClubs(3, myClubId: 3);
        var players = new PlayerHelper(state);
        var tactics = new TacticHelper(state, players);
        var simulator = new MatchSimulatorHelper(new ClubHelper(state), tactics, players, state, new RecordingNotifications { Now = state.Date });

        var us = state.Clubs[0];
        var them = state.Clubs[1];
        foreach (var player in state.Players)
        {
            var isOurs = player.Contract?.ClubId == us.Id;
            player.Rating = isOurs ? ourRating : 70;
            player.Stamina = isOurs ? ourStamina : 65;
        }
        foreach (var club in new[] { us, them })
        {
            tactics.ResetTacticForClub(club);
            tactics.FillEmptyTacticSlotsByClubId(club.Id);
        }
        us.Instructions = ours;
        them.Instructions = theirs ?? new TeamInstructionsModel();

        var league = TestData.League(50, state.Clubs);
        state.Competitions.Add(league);

        double points = 0, wins = 0, draws = 0, goals = 0;
        for (var i = 0; i < matches; i++)
        {
            var fixture = new FixtureModel { HomeClub = us, AwayClub = them, Date = state.Date, FixtureType = FixtureType.Base };
            league.Fixtures.Clear();
            league.Fixtures.Add(fixture);

            int ourGoals, theirGoals;
            if (switchTo == null)
            {
                simulator.ConcludeFixture(fixture, league);
                (ourGoals, theirGoals) = (fixture.GoalsHome!.Value, fixture.GoalsAway!.Value);
            }
            else
            {
                // The live-match path: the whole match is generated at kick-off, then re-evaluated from the change.
                us.Instructions = ours.Clone();
                simulator.PreGenerateMatch(fixture);
                us.Instructions = switchTo.Clone();
                simulator.ReGenerateFromMinute(fixture, switchMinute);
                ourGoals = fixture.HomeScorers.Count(g => g.Minute <= 90);
                theirGoals = fixture.AwayScorers.Count(g => g.Minute <= 90);
                us.Instructions = ours.Clone();
            }

            goals += ourGoals + theirGoals;
            if (ourGoals > theirGoals) { points += 3; wins++; }
            else if (ourGoals == theirGoals) { points += 1; draws++; }

            // Keep the squads fit and eligible: only the tactics should change the results.
            foreach (var player in state.Players)
            {
                player.SuspensionMatches = 0;
                player.InjuryEndDate = null;
                player.Fitness = FitnessRules.Max;
            }
        }

        return new Result(points / matches, wins / matches, draws / matches, goals / matches);
    }

    private static TeamInstructionsModel With(Mentality mentality = Mentality.Balanced, Pressing pressing = Pressing.Medium) =>
        new() { Mentality = mentality, Pressing = pressing };

    private void Print(string label, Result r) =>
        output.WriteLine($"{label,-44} PPG {r.PointsPerGame:F3}  W {r.WinRate:P1}  D {r.DrawRate:P1}  goals {r.GoalsPerMatch:F2}");

    [SimulationFact]
    public void Mentality_HelpsTheUnderdogWhenDefensiveAndTheFavouriteWhenAttacking()
    {
        foreach (var (label, rating) in new[] { ("underdog", UnderdogRating), ("even", EvenRating), ("favourite", FavouriteRating) })
        {
            var balanced = Play(rating, With());
            var defensive = Play(rating, With(Mentality.Defensive));
            var attacking = Play(rating, With(Mentality.Attacking));
            Print($"{label} balanced", balanced);
            Print($"{label} defensive", defensive);
            Print($"{label} attacking", attacking);

            if (rating == UnderdogRating)
            {
                Assert.True(defensive.PointsPerGame > balanced.PointsPerGame, "Defensive should help an underdog");
                Assert.True(attacking.PointsPerGame < balanced.PointsPerGame, "Attacking should hurt an underdog");
            }
            else if (rating == FavouriteRating)
            {
                Assert.True(attacking.PointsPerGame > balanced.PointsPerGame, "Attacking should help a favourite");
                Assert.True(defensive.PointsPerGame < balanced.PointsPerGame, "Defensive should hurt a favourite");
            }
            else
            {
                Assert.InRange(defensive.PointsPerGame - balanced.PointsPerGame, -0.08, 0.08);
                Assert.InRange(attacking.PointsPerGame - balanced.PointsPerGame, -0.08, 0.08);
            }
        }
    }

    [SimulationFact]
    public void HighPress_PaysOffForFitSidesAndCostsUnfitOnes()
    {
        var baseline = Play(EvenRating, With());
        var fit = Play(EvenRating, With(pressing: Pressing.High), ourStamina: 82);
        var average = Play(EvenRating, With(pressing: Pressing.High), ourStamina: 65);
        var unfit = Play(EvenRating, With(pressing: Pressing.High), ourStamina: 45);
        Print("even, medium press", baseline);
        Print("even, high press, stamina 82", fit);
        Print("even, high press, stamina 65", average);
        Print("even, high press, stamina 45", unfit);

        Assert.True(fit.PointsPerGame > baseline.PointsPerGame, "A fit side should gain from a high press");
        Assert.True(unfit.PointsPerGame < baseline.PointsPerGame, "An unfit side should lose from a high press");
        Assert.InRange(fit.PointsPerGame - baseline.PointsPerGame, 0, 0.15);
    }

    [SimulationFact]
    public void LowPress_CostsAgainstMediumButHoldsUpAgainstAHighPress()
    {
        // The low press's cost against a neutral side is small (about 0.02 points a game), so this
        // pair needs more matches to rise clearly above the noise.
        var mediumVsMedium = Play(EvenRating, With(), matches: 100_000);
        var lowVsMedium = Play(EvenRating, With(pressing: Pressing.Low), matches: 100_000);
        var mediumVsHigh = Play(EvenRating, With(), theirs: With(pressing: Pressing.High));
        var lowVsHigh = Play(EvenRating, With(pressing: Pressing.Low), theirs: With(pressing: Pressing.High));
        Print("even, medium vs medium", mediumVsMedium);
        Print("even, low vs medium", lowVsMedium);
        Print("even, medium vs high press", mediumVsHigh);
        Print("even, low vs high press", lowVsHigh);

        Assert.True(lowVsMedium.PointsPerGame < mediumVsMedium.PointsPerGame, "A low press should cost against a neutral side");
        Assert.True(lowVsHigh.PointsPerGame > mediumVsHigh.PointsPerGame, "A low press should do better than medium against a high press");
    }

    // Against a side pressing high all game, changing at the hour once they're tiring: either sit off
    // and counter, or press them yourself with fresh legs. Both should beat doing nothing.
    [SimulationFact]
    public void AgainstATiringPress_SwitchingAtTheHourPaysOff()
    {
        var theirs = With(pressing: Pressing.High);
        var stay = Play(EvenRating, With(), theirs: theirs);
        var toLow = Play(EvenRating, With(), theirs: theirs, switchTo: With(pressing: Pressing.Low), switchMinute: 60);
        var toHigh = Play(EvenRating, With(), theirs: theirs, switchTo: With(pressing: Pressing.High), switchMinute: 60);
        Print("vs high press: medium all game", stay);
        Print("vs high press: medium, low from 60'", toLow);
        Print("vs high press: medium, high from 60'", toHigh);

        Assert.True(toLow.PointsPerGame > stay.PointsPerGame, "Sitting off a tiring press should pay off");
        Assert.True(toHigh.PointsPerGame > stay.PointsPerGame, "Pressing a tiring side with fresh legs should pay off");
    }

    // Pressing only in the second half avoids most of the fatigue, so it mustn't be a free win
    // against a side that hasn't tired itself out.
    [SimulationFact]
    public void SecondHalfPress_IsNotAFreeWinAgainstAFreshSide()
    {
        var stay = Play(EvenRating, With());
        var fromHalfTime = Play(EvenRating, With(), switchTo: With(pressing: Pressing.High), switchMinute: 45);
        var fromTheHour = Play(EvenRating, With(), switchTo: With(pressing: Pressing.High), switchMinute: 60);
        Print("vs medium: medium all game", stay);
        Print("vs medium: high from 45'", fromHalfTime);
        Print("vs medium: high from 60'", fromTheHour);

        Assert.InRange(fromHalfTime.PointsPerGame - stay.PointsPerGame, -0.1, 0.06);
        Assert.InRange(fromTheHour.PointsPerGame - stay.PointsPerGame, -0.1, 0.06);
    }

    [SimulationFact]
    public void NeutralInstructions_KeepGoalsPerMatchWhereTheyWere()
    {
        var result = Play(EvenRating, With());
        Print("even, neutral", result);
        Assert.InRange(result.GoalsPerMatch, 2.2, 2.6);
    }
}
