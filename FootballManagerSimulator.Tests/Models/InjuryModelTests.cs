using FootballManagerSimulator.Maui.Models;

namespace FootballManagerSimulator.Tests.Models;

public class InjuryModelTests
{
    [Fact]
    public void InjuryModel_DefaultsToEmptyDescriptionAndZeroDays()
    {
        var injury = new InjuryModel();

        Assert.Equal(0, injury.Minute);
        Assert.Equal(0, injury.PlayerId);
        Assert.Equal("", injury.Description);
        Assert.Equal(0, injury.DaysOut);
    }

    [Fact]
    public void InjuryModel_StoresAllProperties()
    {
        var injury = new InjuryModel
        {
            Minute = 34,
            PlayerId = 42,
            Description = "Hamstring injury",
            DaysOut = 28
        };

        Assert.Equal(34, injury.Minute);
        Assert.Equal(42, injury.PlayerId);
        Assert.Equal("Hamstring injury", injury.Description);
        Assert.Equal(28, injury.DaysOut);
    }
}
