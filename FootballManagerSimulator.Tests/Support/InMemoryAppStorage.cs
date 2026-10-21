using FootballManagerSimulator.Maui.Interfaces;

namespace FootballManagerSimulator.Tests.Support;

/// <summary>
/// Test double for <see cref="IAppStorage"/>: bundled resources are read from the test output
/// folder and saves go to a throwaway temp directory that is deleted on dispose.
/// </summary>
public sealed class InMemoryAppStorage : IAppStorage, IDisposable
{
    public InMemoryAppStorage()
    {
        SaveDirectory = Path.Combine(Path.GetTempPath(), "fms-tests-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(SaveDirectory);
    }

    public string SaveDirectory { get; }

    public Stream OpenBundledResource(string fileName) =>
        File.OpenRead(Path.Combine(AppContext.BaseDirectory, fileName));

    public void Dispose()
    {
        try { Directory.Delete(SaveDirectory, recursive: true); } catch { }
    }
}
