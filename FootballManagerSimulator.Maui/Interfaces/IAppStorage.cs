namespace FootballManagerSimulator.Maui.Interfaces;

/// <summary>
/// Abstracts the two platform-specific file concerns so the shared game code can run on
/// both the Blazor Server web host (reads from disk) and the .NET MAUI Android host
/// (reads bundled assets, writes to app-private storage).
/// </summary>
public interface IAppStorage
{
    /// <summary>
    /// Opens a read-only stream over a data file shipped with the app,
    /// e.g. <c>settings.json</c> or <c>playerData.json</c>.
    /// </summary>
    Stream OpenBundledResource(string fileName);

    /// <summary>Directory where save games (<c>*.fms</c>) are written and read.</summary>
    string SaveDirectory { get; }
}
