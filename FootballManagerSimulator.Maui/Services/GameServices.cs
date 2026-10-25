using System.Reflection;
using FootballManagerSimulator.Maui.Competitions.Services;
using FootballManagerSimulator.Maui.Events;
using FootballManagerSimulator.Maui.Interfaces;
using FootballManagerSimulator.Maui.Models;
using Microsoft.Extensions.DependencyInjection;
using NetCore.AutoRegisterDi;

namespace FootballManagerSimulator.Maui.Services;

/// <summary>
/// Central registration of the game domain services. Shared by every front-end host
/// (Blazor Server web, .NET MAUI Android) so the composition is identical everywhere.
/// The host is responsible only for registering <see cref="IAppStorage"/> and
/// <c>IOptions&lt;SettingsModel&gt;</c> before calling this.
/// </summary>
public static class GameServices
{
    public static void Register(IServiceCollection services, Assembly gameAssembly)
    {
        // Auto-register the Helper/Factory/Provider/Service families by convention.
        // NOTE: the "Event" types are deliberately excluded – they are always built via
        // ActivatorUtilities with an explicit trigger DateOnly, so registering them as
        // IEvent yields descriptors the container cannot construct.
        services
            .RegisterAssemblyPublicNonGenericClasses(gameAssembly)
            .Where(p => p.Name.EndsWith("Helper")
                || p.Name.EndsWith("Factory")
                || p.Name.EndsWith("Provider")
                || p.Name.EndsWith("Service"))
            .AsPublicImplementedInterfaces();

        // A single in-memory game shared for the lifetime of the app (local single-player).
        services.AddSingleton<IState, StateModel>();
        services.AddSingleton<IGameCreator, GameCreatorModel>();
        services.AddSingleton<IEventManager, EventManager>();
        services.AddSingleton<GameActivity>();

        // Orchestration layer that replaces the old console screen HandleInput logic.
        services.AddScoped<GameEngine>();

        // Concrete types resolved directly (by events / providers / the factory).
        services.AddScoped<Competitions.Providers.EnglishLeagueCupProvider>();
        services.AddScoped<EnglishLeagueCupService>();
        services.AddScoped<FriendlyService>();
        services.AddScoped<EnglishPremierLeagueService>();
        services.AddScoped<LeagueService>();
        services.AddScoped<EnglishFootballLeagueService>();
        services.AddScoped<EnglishCommunityShieldService>();
        services.AddScoped<EnglishNationalLeagueService>();
        services.AddScoped<ScottishPremiershipService>();
    }
}
