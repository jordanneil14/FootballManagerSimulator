using FootballManagerSimulator.Interfaces;

namespace FootballManagerSimulator.Maui;

/// <summary>
/// MAUI/Android implementation of <see cref="IAppStorage"/>. Bundled data files are read from
/// the app package (MauiAsset), and save games are written to the app's private data directory.
/// </summary>
public class MauiAppStorage : IAppStorage
{
    public Stream OpenBundledResource(string fileName)
    {
        // Run the async package read on a thread-pool thread. Blocking the UI thread directly on
        // OpenAppPackageFileAsync (e.g. during startup) can deadlock and crash the app. App-package
        // streams also aren't seekable, so we copy into a MemoryStream the callers can seek.
        return Task.Run(async () =>
        {
            using var packageStream = await FileSystem.OpenAppPackageFileAsync(fileName);
            var memory = new MemoryStream();
            await packageStream.CopyToAsync(memory);
            memory.Position = 0;
            return memory;
        }).GetAwaiter().GetResult();
    }

    public string SaveDirectory => FileSystem.AppDataDirectory;
}
