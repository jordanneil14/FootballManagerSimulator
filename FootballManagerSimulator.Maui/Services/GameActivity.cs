namespace FootballManagerSimulator.Maui.Services;

/// <summary>
/// Runs heavy game work (advancing days, simulating matches, saving) off the UI thread so the
/// app keeps responding; on Android a blocked UI thread shows "not responding". While anything
/// is running, <see cref="IsBusy"/> is true: the layout shows an overlay that blocks input, and
/// the pages involved skip re-rendering so nothing reads the game state while it changes.
/// </summary>
public sealed class GameActivity
{
    private int _running;

    public bool IsBusy => Volatile.Read(ref _running) > 0;

    /// <summary>Raised on whichever thread starts or finishes the work; subscribers use InvokeAsync.</summary>
    public event Action? Changed;

    public async Task<T> RunAsync<T>(Func<T> work)
    {
        Interlocked.Increment(ref _running);
        Changed?.Invoke();
        try
        {
            return await Task.Run(work);
        }
        finally
        {
            Interlocked.Decrement(ref _running);
            Changed?.Invoke();
        }
    }

    public Task RunAsync(Action work) => RunAsync(() =>
    {
        work();
        return true;
    });
}
