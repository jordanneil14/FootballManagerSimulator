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

    [Theory]
    [InlineData(26, 1, 0.98)]   // first team
    [InlineData(26, 11, 0.98)]
    [InlineData(26, 12, 0.98)]  // rotation
    [InlineData(26, 18, 0.98)]
    [InlineData(26, 19, 0.95)]  // squad
    [InlineData(26, 25, 0.95)]
    [InlineData(26, 26, 0.5)]   // surplus: half are let go
    public void RenewalChance_DependsOnThePlayersPlaceInTheSquad(int age, int squadRank, double expected) =>
        Assert.Equal(expected, ContractRenewalRules.RenewalChance(Aged(age, 70), squadRank, 70, SeasonEnd), precision: 6);

    [Theory]
    [InlineData(21, 30, 0.65)]  // prospects are kept
    [InlineData(21, 20, 0.98)]  // capped
    [InlineData(31, 5, 0.93)]   // first-team regulars barely lose out with age
    [InlineData(33, 5, 0.83)]
    [InlineData(31, 15, 0.83)]  // everyone else loses out far more
    [InlineData(33, 15, 0.68)]
    [InlineData(33, 28, 0.2)]
    public void RenewalChance_AgeCountsAgainstFringePlayersMoreThanTheFirstTeam(int age, int squadRank, double expected) =>
        Assert.Equal(expected, ContractRenewalRules.RenewalChance(Aged(age, 70), squadRank, 70, SeasonEnd), precision: 6);

    [Theory]
    [InlineData(60, 30, 0.65)]  // 10 below the squad: still a prospect
    [InlineData(59, 30, 0.5)]   // 11 below: no bonus, treated like any surplus player
    [InlineData(55, 20, 0.95)]
    public void RenewalChance_OnlyYoungPlayersCloseToTheSquadsLevelAreKeptAsProspects(int rating, int squadRank, double expected) =>
        Assert.Equal(expected, ContractRenewalRules.RenewalChance(Aged(20, rating), squadRank, 70, SeasonEnd), precision: 6);

    [Fact]
    public void RenewalChance_IsZeroForPlayersWhoWillRetire() =>
        Assert.Equal(0, ContractRenewalRules.RenewalChance(Aged(PlayerModel.RetirementAge, 80), 1, 70, SeasonEnd));

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
