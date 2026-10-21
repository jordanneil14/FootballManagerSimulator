using FootballManagerSimulator.Maui.Interfaces;
using FootballManagerSimulator.Maui.Models;
using FootballManagerSimulator.Maui.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;

namespace FootballManagerSimulator.Tests.Support;

/// <summary>
/// Builds the real game container exactly as MauiProgram does (GameServices.Register), with the
/// platform storage swapped for <see cref="InMemoryAppStorage"/>. Settings default to the real
/// bundled settings.json.
/// </summary>
public sealed class GameHost : IDisposable
{
    private readonly ServiceProvider _root;
    private readonly IServiceScope _scope;

    public GameHost(SettingsModel? settings = null)
    {
        Storage = new InMemoryAppStorage();
        Settings = settings ?? LoadBundledSettings(Storage);

        var services = new ServiceCollection();
        services.AddSingleton<IAppStorage>(Storage);
        services.AddSingleton<IOptions<SettingsModel>>(Options.Create(Settings));
        GameServices.Register(services, typeof(GameEngine).Assembly);

        _root = services.BuildServiceProvider();
        _scope = _root.CreateScope();
    }

    public InMemoryAppStorage Storage { get; }
    public SettingsModel Settings { get; }
    public IServiceProvider Services => _scope.ServiceProvider;
    public GameEngine Engine => Services.GetRequiredService<GameEngine>();
    public IState State => Services.GetRequiredService<IState>();

    public T Get<T>() where T : notnull => Services.GetRequiredService<T>();

    public static SettingsModel LoadBundledSettings(IAppStorage storage)
    {
        using var stream = storage.OpenBundledResource("settings.json");
        using var reader = new StreamReader(stream);
        return JsonConvert.DeserializeObject<SettingsModel>(reader.ReadToEnd())
            ?? throw new InvalidOperationException("Unable to load settings.json");
    }

    /// <summary>Runs the new-game flow and picks the given club.</summary>
    public GameEngine StartNewGame(int clubId = 9, string managerName = "test manager")
    {
        var engine = Engine;
        engine.StartNewGame();
        engine.SetManagerName(managerName);
        engine.SelectLeague(Settings.Clubs.First(c => c.Id == clubId).LeagueId);
        engine.SelectClub(clubId);
        return engine;
    }

    public void Dispose()
    {
        _scope.Dispose();
        _root.Dispose();
        Storage.Dispose();
    }
}
