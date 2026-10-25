using FootballManagerSimulator.Maui.Models;

namespace FootballManagerSimulator.Tests.Models;

public class ContractRenewalRulesTests
{
    private static readonly DateOnly SeasonEnd = new(2027, 6, 30);

    private static PlayerModel Aged(int age, int rating) => new()
    {
        Rating = rating,
        BirthDate = SeasonEnd.AddYears(-age).AddDays(-1).ToString("MM/dd/yyyy", System.Globalization.CultureInfo.InvariantCulture)
    };

    [Fact]
    public void RenewalChance_FavoursGoodYoungPlayersOverAgeingOnes()
    {
        var youngStar = ContractRenewalRules.RenewalChance(Aged(21, 75), 70, SeasonEnd);
        var primeRegular = ContractRenewalRules.RenewalChance(Aged(26, 72), 70, SeasonEnd);
        var primeReserve = ContractRenewalRules.RenewalChance(Aged(26, 65), 70, SeasonEnd);
        var veteranReserve = ContractRenewalRules.RenewalChance(Aged(33, 65), 70, SeasonEnd);

        Assert.Equal(0.95, youngStar, precision: 6);
        Assert.Equal(0.85, primeRegular, precision: 6);
        Assert.Equal(0.55, primeReserve, precision: 6);
        Assert.Equal(0.25, veteranReserve, precision: 6);
    }

    [Fact]
    public void RenewalChance_IsZeroForPlayersWhoWillRetire() =>
        Assert.Equal(0, ContractRenewalRules.RenewalChance(Aged(PlayerModel.RetirementAge, 80), 70, SeasonEnd));

    [Theory]
    [InlineData(20, 2, 4)]
    [InlineData(27, 1, 3)]
    [InlineData(33, 1, 2)]
    public void RenewalYears_GivesYoungerPlayersLongerDeals(int age, int min, int max)
    {
        var years = Enumerable.Range(0, 300).Select(_ => ContractRenewalRules.RenewalYears(age)).ToList();

        Assert.All(years, y => Assert.InRange(y, min, max));
        Assert.Equal(max - min + 1, years.Distinct().Count());
    }
}
