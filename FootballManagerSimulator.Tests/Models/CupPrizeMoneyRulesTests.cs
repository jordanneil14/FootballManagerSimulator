using FootballManagerSimulator.Maui.Models;

namespace FootballManagerSimulator.Tests.Models;

public class CupPrizeMoneyRulesTests
{
    [Fact]
    public void PrizeMoney_RisesEveryRound_AndWinnersGetDouble()
    {
        var losers = Enumerable.Range(1, 7).Select(CupPrizeMoneyRules.ForLoser).ToList();

        Assert.Equal([25_000, 40_000, 50_000, 100_000, 200_000, 500_000, 1_000_000], losers);
        Assert.All(Enumerable.Range(1, 7), r => Assert.Equal(2 * CupPrizeMoneyRules.ForLoser(r), CupPrizeMoneyRules.ForWinner(r)));
    }

    [Theory]
    [InlineData(7, "the final")]
    [InlineData(6, "the semi-final")]
    [InlineData(5, "the quarter-final")]
    [InlineData(4, "round 4")]
    [InlineData(1, "round 1")]
    public void RoundName_NamesTheLastThreeRounds(int round, string expected) =>
        Assert.Equal(expected, CupPrizeMoneyRules.RoundName(round, finalRound: 7));
}
