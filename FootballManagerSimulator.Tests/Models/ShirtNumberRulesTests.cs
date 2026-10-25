using FootballManagerSimulator.Maui.Models;

namespace FootballManagerSimulator.Tests.Models;

public class ShirtNumberRulesTests
{
    [Theory]
    [InlineData(new int[0], 1)]
    [InlineData(new[] { 1 }, 13)]
    [InlineData(new[] { 1, 13 }, 21)]
    [InlineData(new[] { 1, 13, 21 }, 12)]
    [InlineData(new[] { 13 }, 1)]
    public void Goalkeepers_Take1Then13Then21ThenTheLowestUnreservedNumber(int[] taken, int expected)
    {
        Assert.Equal(expected, ShirtNumberRules.NextAvailable(taken, "GK"));
    }

    [Theory]
    [InlineData("CB", new int[0], 2)]
    [InlineData("LB", new[] { 2 }, 3)]
    [InlineData("RB", new[] { 2, 3 }, 5)]
    [InlineData("CB", new[] { 2, 3, 5 }, 6)]
    [InlineData("CDM", new int[0], 4)]
    [InlineData("CM", new[] { 4 }, 7)]
    [InlineData("LM", new[] { 4, 7 }, 8)]
    [InlineData("CAM", new[] { 4, 7, 8 }, 11)]
    [InlineData("ST", new int[0], 9)]
    [InlineData("LW", new[] { 9 }, 10)]
    public void OutfieldPlayers_TakeTheNumbersReservedForTheirLineFirst(string position, int[] taken, int expected)
    {
        Assert.Equal(expected, ShirtNumberRules.NextAvailable(taken, position));
    }

    [Theory]
    [InlineData("CB", new[] { 2, 3, 5, 6 }, 12)]
    [InlineData("CM", new[] { 4, 7, 8, 11, 12 }, 14)]
    [InlineData("ST", new[] { 9, 10, 12, 14, 15, 16, 17, 18, 19, 20 }, 22)]
    public void OnceTheirLinesNumbersAreGone_PlayersTakeTheLowestUnreservedNumber(string position, int[] taken, int expected)
    {
        // Other lines' free reserved numbers (e.g. 4 or 9 for a defender) stay untouched.
        Assert.Equal(expected, ShirtNumberRules.NextAvailable(taken, position));
    }

    [Fact]
    public void ReservedNumbersAreOnlyBorrowedWhenEveryOtherNumberIsTaken()
    {
        var taken = Enumerable.Range(1, 99).Where(n => n != 9).ToList();

        Assert.Equal(9, ShirtNumberRules.NextAvailable(taken, "CB"));
    }

    [Fact]
    public void OnlyCountsNumbersAtTheSameClub()
    {
        var players = new List<PlayerModel>
        {
            new() { ShirtNumber = 9, Contract = new PlayerModel.ContractModel { ClubId = 1 } },
            new() { ShirtNumber = 10, Contract = new PlayerModel.ContractModel { ClubId = 2 } },
            new() { ShirtNumber = 11 } // free agent
        };

        Assert.Equal(10, ShirtNumberRules.NextAvailable(players, clubId: 1, "ST"));
    }
}
