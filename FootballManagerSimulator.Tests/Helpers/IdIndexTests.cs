using FootballManagerSimulator.Maui.Helpers;
using FootballManagerSimulator.Maui.Models;
using FootballManagerSimulator.Tests.Support;

namespace FootballManagerSimulator.Tests.Helpers;

public class IdIndexTests
{
    private readonly IdIndex<PlayerModel> _index = new(p => p.Id);
    private readonly List<PlayerModel> _players = Enumerable.Range(1, 5).Select(i => TestData.Player(i, clubId: 1)).ToList();

    [Fact]
    public void Find_ReturnsThePlayerWithThatId()
    {
        Assert.Same(_players[2], _index.Find(_players, 3));
        Assert.Null(_index.Find(_players, 99));
    }

    [Fact]
    public void Find_SeesPlayersAddedAfterTheIndexWasBuilt()
    {
        _index.Find(_players, 1);
        var added = TestData.Player(6, clubId: 1);
        _players.Add(added);

        Assert.Same(added, _index.Find(_players, 6));
    }

    [Fact]
    public void Find_ForgetsRemovedPlayersAndFindsTheOnesThatMoved()
    {
        _index.Find(_players, 1);
        var last = _players[4];
        _players.RemoveAt(0);

        Assert.Null(_index.Find(_players, 1));
        Assert.Same(last, _index.Find(_players, 5));
    }

    [Fact]
    public void Find_SeesASwapThatLeavesTheCountUnchanged()
    {
        _index.Find(_players, 1);
        _players.RemoveAt(0);
        var replacement = TestData.Player(7, clubId: 1);
        _players.Add(replacement);

        Assert.Same(replacement, _index.Find(_players, 7));
        Assert.Null(_index.Find(_players, 1));
    }

    [Fact]
    public void Find_UsesAReplacedList()
    {
        _index.Find(_players, 1);
        var loaded = new List<PlayerModel> { TestData.Player(1, clubId: 2) };

        Assert.Same(loaded[0], _index.Find(loaded, 1));
    }
}
