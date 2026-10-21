using System.Text.RegularExpressions;
using FootballManagerSimulator.Maui.Enums;
using FootballManagerSimulator.Maui.Factories;
using FootballManagerSimulator.Maui.Helpers;
using FootballManagerSimulator.Maui.Models;
using FootballManagerSimulator.Maui.Services;
using FootballManagerSimulator.Tests.Support;

namespace FootballManagerSimulator.Tests.Helpers;

public class NumberHelperTests
{
    [Theory]
    [InlineData(0, "0")]
    [InlineData(-3, "-3")]
    [InlineData(1, "1st")]
    [InlineData(2, "2nd")]
    [InlineData(3, "3rd")]
    [InlineData(4, "4th")]
    [InlineData(11, "11th")]
    [InlineData(12, "12th")]
    [InlineData(13, "13th")]
    [InlineData(21, "21st")]
    [InlineData(22, "22nd")]
    [InlineData(23, "23rd")]
    [InlineData(101, "101st")]
    [InlineData(111, "111th")]
    [InlineData(112, "112th")]
    public void AddOrdinal(int number, string expected) => Assert.Equal(expected, NumberHelper.AddOrdinal(number));
}

public class ClubHelperTests
{
    private readonly StateModel _state = TestData.StateWithClubs(2);
    private readonly ClubHelper _helper;

    public ClubHelperTests()
    {
        _helper = new ClubHelper(_state);
        new TacticHelper(_state, new PlayerHelper(_state)).ResetTacticForClub(_state.Clubs[0]);
    }

    [Fact]
    public void GetClubByName_IsCaseInsensitive()
    {
        Assert.Same(_state.Clubs[1], _helper.GetClubByName("CLUB 2"));
        Assert.Null(_helper.GetClubByName("Missing"));
    }

    [Fact]
    public void GetClubById_ThrowsForUnknownClub()
    {
        Assert.Same(_state.Clubs[0], _helper.GetClubById(1));
        Assert.Throws<InvalidOperationException>(() => _helper.GetClubById(42));
    }

    [Fact]
    public void GetStartingElevenByClub_ExcludesSubsAndReserves()
    {
        var starters = _helper.GetStartingElevenByClub(1).ToList();

        Assert.Equal(11, starters.Count);
        Assert.DoesNotContain(starters, s => s.TacticSlotType is TacticSlotType.SUB or TacticSlotType.RES);
    }

    [Fact]
    public void GetStartingElevenSumRatingForClub_SumsOnlyFilledStarterSlots()
    {
        var club = _state.Clubs[0];
        var starters = club.TacticSlots.Where(s => s.TacticSlotType is not (TacticSlotType.SUB or TacticSlotType.RES)).ToList();
        starters[0].PlayerId = 1;
        starters[1].PlayerId = 2;
        club.TacticSlots.First(s => s.TacticSlotType == TacticSlotType.SUB).PlayerId = 3;

        var expected = _state.Players.First(p => p.Id == 1).Rating + _state.Players.First(p => p.Id == 2).Rating;
        Assert.Equal(expected, _helper.GetStartingElevenSumRatingForClub(1));
    }
}

public class NotificationFactoryTests
{
    [Fact]
    public void AddNotification_AppendsToState()
    {
        var state = new StateModel { Date = new DateOnly(2026, 8, 1) };
        var factory = new NotificationFactory(state);

        factory.AddNotification(new DateOnly(2026, 9, 1), "Chairman", "Subject", "Body");
        factory.AddNotificationNow("Scout", "Now", "Body 2");

        Assert.Collection(state.Notifications,
            n => { Assert.Equal(new DateOnly(2026, 9, 1), n.Date); Assert.Equal("Chairman", n.Recipient); Assert.False(n.RequiresAction); },
            n => { Assert.Equal(state.Date, n.Date); Assert.Equal("Scout", n.Recipient); });
    }
}

public class WeatherHelperTests
{
    [Theory]
    [InlineData(1, -3, 3)]
    [InlineData(6, 20, 26)]
    [InlineData(10, 6, 12)]
    [InlineData(12, -2, 4)]
    public void GetTodaysWeather_ReturnsTypeAndTemperatureForMonth(int month, int min, int maxExclusive)
    {
        var state = new StateModel { Date = new DateOnly(2026, month, 10) };
        var helper = new WeatherHelper(state);

        for (var i = 0; i < 50; i++)
        {
            var match = Regex.Match(helper.GetTodaysWeather(), @"^(?<type>[A-Za-z]+) (?<temp>-?\d+)c$");
            Assert.True(match.Success);
            var temp = int.Parse(match.Groups["temp"].Value);
            Assert.InRange(temp, min, maxExclusive - 1);
        }
    }
}

public class UiTests
{
    [Fact]
    public void Money_FormatsWithPoundSignAndThousandsSeparators() =>
        Assert.Equal($"£{1234567:n0}", Ui.Money(1234567));

    [Theory]
    [InlineData("GK", 0)]
    [InlineData("LW/ST", 15)]
    [InlineData("st", 19)]
    [InlineData("??", 23)]
    [InlineData(null, 23)]
    public void PositionSortOrder(string? position, int expected) => Assert.Equal(expected, Ui.PositionSortOrder(position));

    [Fact]
    public void PositionSortOrder_GoalkeepersBeforeDefendersBeforeMidfieldersBeforeAttackers()
    {
        Assert.True(Ui.PositionSortOrder("GK") < Ui.PositionSortOrder("CB"));
        Assert.True(Ui.PositionSortOrder("CB") < Ui.PositionSortOrder("CM"));
        Assert.True(Ui.PositionSortOrder("CM") < Ui.PositionSortOrder("ST"));
    }

    [Theory]
    [InlineData(90, "rating-badge elite")]
    [InlineData(82, "rating-badge elite")]
    [InlineData(75, "rating-badge good")]
    [InlineData(74, "rating-badge")]
    public void RatingCssClass(int rating, string expected) => Assert.Equal(expected, Ui.RatingCssClass(rating));
}
