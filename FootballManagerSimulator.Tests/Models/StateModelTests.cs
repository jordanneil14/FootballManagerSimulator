using FootballManagerSimulator.Maui.Models;

namespace FootballManagerSimulator.Tests.Models;

public class StateModelTests
{
    [Fact]
    public void NewState_NotificationSettingsDefaultToFalse()
    {
        var state = new StateModel();

        Assert.False(state.ShowTransferNotifications);
        Assert.False(state.ShowManagerNotifications);
    }

    [Fact]
    public void NewState_NotificationSettingsCanBeToggled()
    {
        var state = new StateModel { ShowTransferNotifications = true, ShowManagerNotifications = true };

        Assert.True(state.ShowTransferNotifications);
        Assert.True(state.ShowManagerNotifications);
    }
}
