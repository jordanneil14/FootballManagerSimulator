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
    [InlineData(40, 40 * 750)]
    [InlineData(49, 49 * 750)]
    [InlineData(50, 50 * 1050)]
    [InlineData(60, 60 * 4125)]
    [InlineData(70, 70 * 12000)]
    [InlineData(79, 79 * 75000)]
    [InlineData(80, 80 * 202500)]
    [InlineData(85, 85 * 337500)]
    [InlineData(91, 91 * 862500)]
    [InlineData(92, 92 * 1125000)]
    public void GetTransferValue_UsesRatingBand(int rating, int expected)
    {
        Assert.Equal(expected, _helper.GetTransferValue(TestData.Player(1, clubId: 1, rating: rating)));
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

    [Fact]
    public void ScoringProbability_IsFinishingTimesShotPowerTimesWeightedPositioning()
    {
        var player = new PlayerModel { Finishing = 80, ShotPower = 70, AttackingPosition = 60 };
        Assert.Equal(80 * 70 * (60 * 0.75), player.ScoringProbability, precision: 6);
    }
}
