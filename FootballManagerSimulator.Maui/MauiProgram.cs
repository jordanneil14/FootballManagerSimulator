using FootballManagerSimulator.Interfaces;
using FootballManagerSimulator.Models;
using FootballManagerSimulator.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;

namespace FootballManagerSimulator.Maui;

public static class MauiProgram
{
	public static MauiApp CreateMauiApp()
	{
		var builder = MauiApp.CreateBuilder();
		builder
			.UseMauiApp<App>()
			.ConfigureFonts(fonts =>
			{
				fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
			});

		builder.Services.AddMauiBlazorWebView();

		// --- Game wiring (shared with the web host) ---
		builder.Services.AddSingleton<IAppStorage, MauiAppStorage>();

		// Load settings.json lazily (on first use, off the UI/startup thread) rather than
		// eagerly in CreateMauiApp, so no bundled-asset I/O happens on the startup path.
		builder.Services.AddSingleton<IOptions<SettingsModel>>(sp =>
		{
			var storage = sp.GetRequiredService<IAppStorage>();
			using var stream = storage.OpenBundledResource("settings.json");
			using var reader = new StreamReader(stream);
			var settings = JsonConvert.DeserializeObject<SettingsModel>(reader.ReadToEnd())
				?? throw new InvalidOperationException("Unable to load settings.json");
			return Options.Create(settings);
		});

		// Register all game domain services from the shared assembly.
		GameServices.Register(builder.Services, typeof(GameEngine).Assembly);

#if DEBUG
		builder.Services.AddBlazorWebViewDeveloperTools();
		builder.Logging.AddDebug();
#endif

		return builder.Build();
	}
}
