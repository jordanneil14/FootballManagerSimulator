using FootballManagerSimulator.Maui.Models;

namespace FootballManagerSimulator.Tests.Models;

public class TransferListRulesTests
{
    [Theory]
    [InlineData(600_000, 1.0)]    // AI clubs list below value
    [InlineData(1_000_000, 1.0)]
    [InlineData(1_200_000, 1.0)]
    [InlineData(1_600_000, 0.51)] // halfway between 120% and 200%
    [InlineData(2_000_000, 0.02)]
    [InlineData(10_000_000, 0.02)]
    public void BidChance_FallsAwayAsTheAskingPriceClimbsAboveValue(int askingPrice, double expected) =>
        Assert.Equal(expected, TransferListRules.BidChance(askingPrice, 1_000_000), precision: 6);

    [Fact]
    public void BidChance_IsUnaffectedWhenThePlayerHasNoValue() =>
        Assert.Equal(1, TransferListRules.BidChance(5_000, 0));
}
