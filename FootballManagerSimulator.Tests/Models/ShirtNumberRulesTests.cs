using FootballManagerSimulator.Maui.Models;

namespace FootballManagerSimulator.Tests.Models;

public class ShirtNumberRulesTests
{
    [Theory]
    [InlineData(new int[0], 1)]
    [InlineData(new[] { 1 }, 13)]
    [InlineData(new[] { 1, 13 }, 21)]
    [InlineData(new[] { 1, 13, 21, 22 }, 23)]
    [InlineData(new[] { 13 }, 1)]
    public void Goalkeepers_Take1Then13Then21ThenFrom22(int[] taken, int expected)
    {
        Assert.Equal(expected, ShirtNumberRules.NextAvailable(taken, "GK"));
    }

    [Theory]
    [InlineData(new int[0], 2)]
    [InlineData(new[] { 2, 3, 5 }, 4)]
    [InlineData(new[] { 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12 }, 14)]
    [InlineData(new[] { 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 14, 15, 16, 17, 18, 19, 20 }, 22)]
    public void OutfieldPlayers_TakeTheLowestFreeNumberSkippingKeepersNumbers(int[] taken, int expected)
    {
        Assert.Equal(expected, ShirtNumberRules.NextAvailable(taken, "CM"));
    }

    [Fact]
    public void OnlyCountsNumbersAtTheSameClub()
    {
        var players = new List<PlayerModel>
        {
            new() { ShirtNumber = 2, Contract = new PlayerModel.ContractModel { ClubId = 1 } },
            new() { ShirtNumber = 3, Contract = new PlayerModel.ContractModel { ClubId = 2 } },
            new() { ShirtNumber = 4 } // free agent
        };

        Assert.Equal(3, ShirtNumberRules.NextAvailable(players, clubId: 1, "ST"));
    }
}
