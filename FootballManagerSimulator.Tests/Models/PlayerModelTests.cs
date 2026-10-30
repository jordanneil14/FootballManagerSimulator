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

    [Fact]
    public void HasGivenUp_OnceAFreeAgentHasGoneAWholeSeasonUnsigned()
    {
        var player = new PlayerModel { FreeAgentSince = new DateOnly(2026, 6, 30) };

        Assert.False(player.HasGivenUp(new DateOnly(2027, 6, 29)));
        Assert.True(player.HasGivenUp(new DateOnly(2027, 6, 30)));
        Assert.True(player.HasGivenUp(new DateOnly(2028, 1, 1)));
    }

    [Fact]
    public void HasGivenUp_NeverForAPlayerWithAClub()
    {
        var player = new PlayerModel
        {
            FreeAgentSince = new DateOnly(2020, 6, 30),
            Contract = new PlayerModel.ContractModel { ClubId = 1, ExpiryDate = new DateOnly(2028, 6, 30) }
        };

        Assert.False(player.HasGivenUp(new DateOnly(2027, 6, 30)));
    }

    [Fact]
    public void HasGivenUp_NeverForAFreeAgentNotYetSeenAtASeasonEnd()
    {
        Assert.False(new PlayerModel().HasGivenUp(new DateOnly(2040, 6, 30)));
    }
}
