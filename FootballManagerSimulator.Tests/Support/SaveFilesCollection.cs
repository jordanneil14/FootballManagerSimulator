namespace FootballManagerSimulator.Tests.Support;

/// <summary>
/// Saves and loads share one process-wide lock, and autosaves skip rather than wait for it, so
/// test classes that save must not run in parallel or their autosaves can be skipped at random.
/// </summary>
[CollectionDefinition(Name)]
public class SaveFilesCollection
{
    public const string Name = "Save files";
}
