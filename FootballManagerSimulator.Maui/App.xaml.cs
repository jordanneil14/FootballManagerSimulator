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
		using var scope = services.CreateScope();
		scope.ServiceProvider.GetRequiredService<GameEngine>().AutoSave();
	}
}
