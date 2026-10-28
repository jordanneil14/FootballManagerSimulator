using FootballManagerSimulator.Maui.Models;

namespace FootballManagerSimulator.Tests.Models;

public class FixtureModelTests
{
    [Fact]
    public void NewFixture_InjuryListsAreEmptyByDefault()
    {
        var fixture = new FixtureModel();

        Assert.NotNull(fixture.HomeInjuries);
        Assert.NotNull(fixture.AwayInjuries);
        Assert.Empty(fixture.HomeInjuries);
        Assert.Empty(fixture.AwayInjuries);
    }

    [Fact]
    public void InjuryLists_CanStoreMultipleInjuries()
    {
        var fixture = new FixtureModel();
        fixture.HomeInjuries.Add(new InjuryModel { Minute = 12, PlayerId = 1, Description = "Minor knock", DaysOut = 5 });
        fixture.HomeInjuries.Add(new InjuryModel { Minute = 67, PlayerId = 2, Description = "Fracture", DaysOut = 60 });
        fixture.AwayInjuries.Add(new InjuryModel { Minute = 33, PlayerId = 3, Description = "Muscle strain", DaysOut = 14 });

        Assert.Equal(2, fixture.HomeInjuries.Count);
        Assert.Single(fixture.AwayInjuries);
    }
}
