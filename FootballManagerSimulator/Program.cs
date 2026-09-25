using FootballManagerSimulator.Competitions.Services;
using FootballManagerSimulator.Factories;
using FootballManagerSimulator.Interfaces;
using FootballManagerSimulator.Models;
using FootballManagerSimulator.Structures;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using NetCore.AutoRegisterDi;

namespace FootballManagerSimulator;

public class Program
{
    private static ServiceProvider CreateServices()
    {
        var serviceProvider = new ServiceCollection();

        serviceProvider
            .RegisterAssemblyPublicNonGenericClasses()
            .Where(p => p.Name.EndsWith("Screen") || p.Name.EndsWith("Helper") || p.Name.EndsWith("Factory") || p.Name.EndsWith("Event") || p.Name.EndsWith("Provider") || p.Name.EndsWith("Service"))
            .AsPublicImplementedInterfaces();

        serviceProvider.AddSingleton<IGame, Game>();
        serviceProvider.AddSingleton<IState, StateModel>();
        serviceProvider.AddSingleton<IGameCreator, GameCreatorModel>();

        var directory = Directory.GetCurrentDirectory() + "\\Resources";
        var settingsConfig = new ConfigurationBuilder()
            .SetBasePath(directory)
            .AddJsonFile("settings.json")
            .Build();
        serviceProvider.AddOptions<SettingsModel>().Bind(settingsConfig);

        serviceProvider.AddScoped<EnglishLeagueCupService>();
        serviceProvider.AddScoped<FriendlyService>();
        serviceProvider.AddScoped<EnglishPremierLeagueService>();
        serviceProvider.AddScoped<LeagueService>();
        serviceProvider.AddScoped<EnglishFootballLeagueService>();

        serviceProvider.AddScoped<CupFixtureDrawFactory>();

        return serviceProvider.BuildServiceProvider();
    }

    public static void Main(string[] args)
    {
        var serviceProvider = CreateServices();
        var game = serviceProvider.GetRequiredService<IGame>();
        if (game != null) game.Run();
    }
}