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
            .Where(p => p.Name.EndsWith("Screen") || p.Name.EndsWith("Helper") || p.Name.EndsWith("Factory") || p.Name.EndsWith("Event"))
            .AsPublicImplementedInterfaces();

        serviceProvider.AddSingleton<IGame, Game>();
        serviceProvider.AddSingleton<IState, State>();
        serviceProvider.AddSingleton<IGameCreator, GameCreator>();

        var directory = Directory.GetCurrentDirectory() + "\\Resources";
        var settingsConfig = new ConfigurationBuilder()
            .SetBasePath(directory)
            .AddJsonFile("settings.json")
            .Build();
        serviceProvider.AddOptions<Settings>().Bind(settingsConfig);

        return serviceProvider.BuildServiceProvider();
    }

    public static void Main(string[] args)
    {
        var serviceProvider = CreateServices();
        var game = serviceProvider.GetRequiredService<IGame>();
        if (game != null) game.Run();
    }
}