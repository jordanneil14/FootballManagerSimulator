using FootballManagerSimulator.Maui.Models;

namespace FootballManagerSimulator.Tests.Models;

public class StateModelTests
{
    [Fact]
    public void NewState_NotificationSettingsDefaultToTrue()
    {
        var state = new StateModel();

        Assert.True(state.ShowTransferNotifications);
        Assert.True(state.ShowManagerNotifications);
    }

    [Fact]
    public void NewState_NotificationSettingsCanBeToggled()
    {
        var state = new StateModel { ShowTransferNotifications = false, ShowManagerNotifications = false };

        Assert.False(state.ShowTransferNotifications);
        Assert.False(state.ShowManagerNotifications);
    }
}
