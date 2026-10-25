using FootballManagerSimulator.Maui.Services;
using Microsoft.Extensions.DependencyInjection;

namespace FootballManagerSimulator.Maui;

public partial class App : Application
{
	public App()
	{
		InitializeComponent();
	}

	protected override Window CreateWindow(IActivationState? activationState)
	{
		var window = new Window(new MainPage()) { Title = "FootballManagerSimulator.Maui" };

		// Android can kill a backgrounded app without warning, so save the career on the way out.
		window.Stopped += (_, _) => AutoSave(activationState?.Context.Services ?? IPlatformApplication.Current?.Services);

		return window;
	}

	private static void AutoSave(IServiceProvider? services)
	{
		if (services == null) return;

		// The engine is scoped to the Blazor view; a fresh scope still shares the singleton game state.
		// Saving on the UI thread could take long enough for Android to report the app as not
		// responding, so it runs in the background; the overlay holds the game if it is reopened mid-save.
		// Skipped while other game work is running: the state is mid-change and would save half-updated.
		var activity = services.GetRequiredService<GameActivity>();
		if (activity.IsBusy) return;

		var scope = services.CreateScope();
		var engine = scope.ServiceProvider.GetRequiredService<GameEngine>();
		_ = activity.RunAsync(() =>
		{
			try { engine.AutoSave(); }
			finally { scope.Dispose(); }
		});
	}
}
