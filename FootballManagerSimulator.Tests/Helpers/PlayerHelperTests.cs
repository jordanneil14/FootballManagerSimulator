using FootballManagerSimulator.Maui.Helpers;
using FootballManagerSimulator.Maui.Models;
using FootballManagerSimulator.Tests.Support;

namespace FootballManagerSimulator.Tests.Helpers;

public class PlayerHelperTests
{
    private readonly StateModel _state = new();
    private readonly PlayerHelper _helper;

    public PlayerHelperTests() => _helper = new PlayerHelper(_state);

    [Fact]
    public void GetTransferValue_FreeAgent_IsZero()
    {
        Assert.Equal(0, _helper.GetTransferValue(TestData.Player(1, clubId: null, rating: 90)));
    }

    [Theory]
    [InlineData(40, 40 * 500)]
    [InlineData(49, 49 * 500)]
    [InlineData(50, 50 * 700)]
    [InlineData(60, 60 * 2750)]
    [InlineData(70, 70 * 8000)]
    [InlineData(79, 79 * 50000)]
    [InlineData(80, 80 * 135000)]
    [InlineData(85, 85 * 225000)]
    [InlineData(91, 91 * 575000)]
    [InlineData(92, 92 * 750000)]
    public void GetTransferValue_UsesRatingBand(int rating, int expected)
    {
        Assert.Equal(expected, _helper.GetTransferValue(TestData.Player(1, clubId: 1, rating: rating)));
    }

    [Theory]
    [InlineData(17, 1.3)]
    [InlineData(21, 1.3)]
    [InlineData(22, 1.15)]
    [InlineData(25, 1.15)]
    [InlineData(26, 1.0)]
    [InlineData(28, 1.0)]
    [InlineData(29, 0.8)]
    [InlineData(31, 0.55)]
    [InlineData(33, 0.35)]
    [InlineData(34, 0.2)]
    public void GetTransferValue_YoungPlayersAreWorthMoreAndOlderPlayersLess(int age, double factor)
    {
        _state.Date = new DateOnly(2027, 1, 1);
        var player = TestData.Player(1, clubId: 1, rating: 80);
        player.BirthDate = _state.Date.AddYears(-age).AddDays(-1).ToString("MM/dd/yyyy", System.Globalization.CultureInfo.InvariantCulture);

        Assert.Equal((int)(80 * 135000 * factor), _helper.GetTransferValue(player));
    }

    [Fact]
    public void GetTransferValue_WithNoBirthDateOnRecordIsNotAdjustedForAge()
    {
        var player = TestData.Player(1, clubId: 1, rating: 80);
        player.BirthDate = "";

        Assert.Equal(80 * 135000, _helper.GetTransferValue(player));
    }

    [Fact]
    public void GetTransferValue_FallsWithAgeForTheSameRating()
    {
        _state.Date = new DateOnly(2027, 1, 1);
        var values = Enumerable.Range(17, 18).Select(age =>
        {
            var player = TestData.Player(1, clubId: 1, rating: 75);
            player.BirthDate = _state.Date.AddYears(-age).AddDays(-1).ToString("MM/dd/yyyy", System.Globalization.CultureInfo.InvariantCulture);
            return _helper.GetTransferValue(player);
        }).ToList();

        Assert.Equal(values.OrderByDescending(v => v), values);
        Assert.True(values[0] > values[^1] * 5);
    }

    [Fact]
    public void GetTransferValue_IncreasesWithRating()
    {
        var values = Enumerable.Range(40, 59)
            .Select(r => _helper.GetTransferValue(TestData.Player(1, clubId: 1, rating: r)))
            .ToList();

        Assert.Equal(values.OrderBy(v => v), values);
    }

    [Fact]
    public void AddPlayersToState_AssignsSequentialIdsAndResolvesClubsByName()
    {
        _state.Clubs.Add(TestData.Club(7, "Arsenal"));
        var data = new PlayerData
        {
            Players =
            [
                new PlayerModel { Name = "A", Contract = new PlayerModel.ContractModel { ClubName = "arsenal" } },
                new PlayerModel { Name = "B", Contract = new PlayerModel.ContractModel { ClubName = "Nowhere FC" } },
                new PlayerModel { Name = "C", Contract = null }
            ]
        };

        _helper.AddPlayersToState(data);

        Assert.Equal([1, 2, 3], _state.Players.Select(p => p.Id));
        Assert.Equal(7, _state.Players[0].Contract!.ClubId);
        Assert.Null(_state.Players[1].Contract); // unknown club -> free agent
        Assert.Null(_state.Players[2].Contract);
    }

    [Fact]
    public void Lookups_FindPlayersByIdAndName()
    {
        _state.Players.Add(TestData.Player(5, clubId: 1));

        Assert.Same(_state.Players[0], _helper.GetPlayerById(5));
        Assert.Same(_state.Players[0], _helper.GetPlayerByName("Player 5"));
        Assert.Null(_helper.GetPlayerById(6));
        Assert.Null(_helper.GetPlayerByName("Nobody"));
    }

    [Fact]
    public void DoesPlayerPlaysForClub_ChecksContract()
    {
        _state.Players.Add(TestData.Player(1, clubId: 3));
        _state.Players.Add(TestData.Player(2, clubId: null));

        Assert.True(_helper.DoesPlayerPlaysForClub(1, 3));
        Assert.False(_helper.DoesPlayerPlaysForClub(1, 4));
        Assert.False(_helper.DoesPlayerPlaysForClub(2, 3));
    }

    [Theory]
    [InlineData("03/15/1990", 2026, 3, 14, 35)]
    [InlineData("03/15/1990", 2026, 3, 15, 36)]
    [InlineData("02/29/2004", 2026, 2, 27, 21)]
    [InlineData("not a date", 2026, 1, 1, 0)]
    public void GetAge_IsWorkedOutFromTheBirthDate(string birthDate, int y, int m, int d, int expected)
    {
        var player = new PlayerModel { BirthDate = birthDate };
        Assert.Equal(expected, player.GetAge(new DateOnly(y, m, d)));
    }

    [Fact]
    public void IsRetiring_FromTheirThirtyFifthBirthday()
    {
        var player = new PlayerModel { BirthDate = "07/01/1991" };
        Assert.False(player.IsRetiring(new DateOnly(2026, 6, 30)));
        Assert.True(player.IsRetiring(new DateOnly(2026, 7, 1)));
    }

    [Fact]
    public void ScoringProbability_IsFinishingTimesShotPowerTimesWeightedPositioning()
    {
        var player = new PlayerModel { Finishing = 80, ShotPower = 70, AttackingPosition = 60 };
        Assert.Equal(80 * 70 * (60 * 0.75), player.ScoringProbability, precision: 6);
    }
}
