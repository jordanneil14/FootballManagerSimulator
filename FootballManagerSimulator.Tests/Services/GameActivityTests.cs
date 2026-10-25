using FootballManagerSimulator.Maui.Services;

namespace FootballManagerSimulator.Tests.Services;

public class GameActivityTests
{
    [Fact]
    public async Task RunAsync_IsBusyOffTheCallingThreadUntilTheWorkFinishes()
    {
        var activity = new GameActivity();
        var changes = 0;
        activity.Changed += () => changes++;
        var release = new ManualResetEventSlim();

        // If the work ran on the calling thread, RunAsync would block on the wait and never return.
        var running = activity.RunAsync(() =>
        {
            release.Wait();
            return 42;
        });

        Assert.True(activity.IsBusy);
        Assert.Equal(1, changes);

        release.Set();
        Assert.Equal(42, await running);

        Assert.False(activity.IsBusy);
        Assert.Equal(2, changes);
    }

    [Fact]
    public async Task RunAsync_ClearsBusyAndPassesOnTheErrorWhenTheWorkFails()
    {
        var activity = new GameActivity();

        await Assert.ThrowsAsync<InvalidOperationException>(() => activity.RunAsync(() => throw new InvalidOperationException()));

        Assert.False(activity.IsBusy);
    }
}
