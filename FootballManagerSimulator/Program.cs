using FootballManagerSimulator.Interfaces;
using FootballManagerSimulator.Screens;
using FootballManagerSimulator.Structures;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.DependencyInjection;
using FootballManagerSimulator.Helpers;
using FootballManagerSimulator.Factories;
using Microsoft.Extensions.Configuration;

namespace FootballManagerSimulator;

public class Program
{
    public static void Main(string[] args)
    {
        var builder = Host.CreateApplicationBuilder(args);

        builder.Services.AddSingleton<IGame, Game>();
        builder.Services.AddSingleton<IClubHelper, ClubHelper>();
        builder.Services.AddSingleton<IPlayerHelper, PlayerHelper>();
        builder.Services.AddSingleton<IState, State>();
        builder.Services.AddSingleton<ITacticHelper, TacticHelper>();
        builder.Services.AddSingleton<IMatchSimulatorHelper, MatchSimulatorHelper>();
        builder.Services.AddSingleton<IGameCreator, GameCreator>();
        builder.Services.AddSingleton<INotificationFactory, NotificationFactory>();
        builder.Services.AddSingleton<IProcessHelper, ProcessHelper>();
        builder.Services.AddSingleton<IGameFactory, GameFactory>();
        builder.Services.AddSingleton<IWeatherHelper, WeatherHelper>();
        builder.Services.AddSingleton<ITransferListHelper, TransferListHelper>();
        builder.Services.AddSingleton<ILeagueFactory, LeagueFactory>();

        builder.Services.AddSingleton<IBaseScreen, FixturesScreen>();
        builder.Services.AddSingleton<IBaseScreen, ScoutScreen>();
        builder.Services.AddSingleton<IBaseScreen, WelcomeScreen>();
        builder.Services.AddSingleton<IBaseScreen, MainScreen>();
        builder.Services.AddSingleton<IBaseScreen, SelectClubScreen>();
        builder.Services.AddSingleton<IBaseScreen, CreateManagerScreen>();
        builder.Services.AddSingleton<IBaseScreen, LoadGameScreen>();
        builder.Services.AddSingleton<IBaseScreen, TacticsScreen>();
        builder.Services.AddSingleton<IBaseScreen, LeagueTableScreen>();
        builder.Services.AddSingleton<IBaseScreen, ClubScreen>();
        builder.Services.AddSingleton<IBaseScreen, MainScreen>();
        builder.Services.AddSingleton<IBaseScreen, FixtureScreen>();
        builder.Services.AddSingleton<IBaseScreen, PreMatchScreen>();
        builder.Services.AddSingleton<IBaseScreen, SaveScreen>();
        builder.Services.AddSingleton<IBaseScreen, HalfTimeScreen>();
        builder.Services.AddSingleton<IBaseScreen, FullTimeScreen>();
        builder.Services.AddSingleton<IBaseScreen, PostMatchScoreScreen>();
        builder.Services.AddSingleton<IBaseScreen, PlayerScreen>();
        builder.Services.AddSingleton<IBaseScreen, FinancesScreen>();
        builder.Services.AddSingleton<IBaseScreen, SelectLeagueScreen>();
        builder.Services.AddSingleton<IBaseScreen, TransferListScreen>();
        builder.Services.AddSingleton<IBaseScreen, TransferPlayerScreen>();

        var directory = Directory.GetCurrentDirectory() + "\\Resources";

        var settingsConfig = new ConfigurationBuilder()
            .SetBasePath(directory)
            .AddJsonFile("settings.json")
            .Build();
        builder.Services.AddOptions<Settings>().Bind(settingsConfig);


        var host = builder.Build();
        var game = host.Services.GetService<IGame>();
        if (game != null) game.Run();
    }
}