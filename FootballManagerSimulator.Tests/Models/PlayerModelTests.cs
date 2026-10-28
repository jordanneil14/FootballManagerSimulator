using FootballManagerSimulator.Maui.Models;

namespace FootballManagerSimulator.Tests.Models;

public class PlayerModelTests
{
    [Fact]
    public void IsInjured_ReturnsTrueWhenInjuryEndDateIsAfterTheGivenDate()
    {
        var player = new PlayerModel { InjuryEndDate = new DateOnly(2026, 8, 15) };

        Assert.True(player.IsInjured(new DateOnly(2026, 8, 1)));
        Assert.True(player.IsInjured(new DateOnly(2026, 8, 14)));
    }

    [Fact]
    public void IsInjured_ReturnsFalseOnOrAfterTheEndDate()
    {
        var player = new PlayerModel { InjuryEndDate = new DateOnly(2026, 8, 15) };

        Assert.False(player.IsInjured(new DateOnly(2026, 8, 15)));
        Assert.False(player.IsInjured(new DateOnly(2026, 9, 1)));
    }

    [Fact]
    public void IsInjured_ReturnsFalseWhenNoInjury()
    {
        var player = new PlayerModel();

        Assert.False(player.IsInjured(new DateOnly(2026, 8, 1)));
    }
}
