using FootballManagerSimulator.Maui.Competitions;

namespace FootballManagerSimulator.Tests.Competitions;

public class LeagueSchedulerTests
{
    public static TheoryData<int, int> Sizes()
    {
        var data = new TheoryData<int, int>();
        for (var clubs = 4; clubs <= 30; clubs += 2)
        {
            data.Add(clubs, 2);
            data.Add(clubs, 3);
        }
        return data;
    }

    [Theory]
    [MemberData(nameof(Sizes))]
    public void EverySeasonVariantKeepsRunsToTwo_AndPairsMeetTheRightNumberOfTimes(int clubs, int meetings)
    {
        // Every rotation the scheduler can pick from is valid, so try each index it might choose.
        for (var choice = 0; choice < 200; choice++)
        {
            var season = LeagueScheduler.Season(clubs, meetings, count => choice % count);

            Assert.Equal((clubs - 1) * meetings, season.Count);
            Assert.True(LeagueScheduler.LongestRun(season, clubs) <= LeagueScheduler.MaxRun);
            Assert.All(season, round => Assert.Equal(clubs, round.SelectMany(m => new[] { m.Home, m.Away }).Distinct().Count()));

            var pairings = season.SelectMany(r => r).GroupBy(m => (Math.Min(m.Home, m.Away), Math.Max(m.Home, m.Away))).ToList();
            Assert.Equal(clubs * (clubs - 1) / 2, pairings.Count);
            Assert.All(pairings, p =>
            {
                Assert.Equal(meetings, p.Count());
                Assert.InRange(p.Count(m => m.Home == p.Key.Item1), meetings / 2, (meetings + 1) / 2);
            });
        }
    }

    [Fact]
    public void Continue_PicksVenuesAndOrderThatKeepRunsToTwoAfterEarlierGames()
    {
        // Slots 0-2 have just had two home games, slots 3-5 two away games.
        var history = Enumerable.Range(0, 6).Select(i => (IReadOnlyList<bool>)new[] { i < 3, i < 3 }).ToList();
        var random = new Random(7);

        var rounds = LeagueScheduler.Continue(history, (_, _) => 0, random.Next);

        Assert.NotNull(rounds);
        Assert.Equal(5, rounds.Count);
        var full = rounds.Prepend(Enumerable.Range(0, 3).Select(i => (i, i + 3)).ToList())
            .Prepend(Enumerable.Range(0, 3).Select(i => (i, i + 3)).ToList());
        Assert.True(LeagueScheduler.LongestRun(full, 6) <= LeagueScheduler.MaxRun);
        Assert.All(rounds[0], m => Assert.True(m.Home >= 3));
    }
}
