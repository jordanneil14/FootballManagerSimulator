﻿using FootballManagerSimulator.Interfaces;
using FootballManagerSimulator.Screens;
using FootballManagerSimulator.Structures;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.DependencyInjection;
using FootballManagerSimulator.Helpers;
using FootballManagerSimulator.Factories;

namespace FootballManagerSimulator;

public class Program
{
    public static void Main(string[] args)
    {
        var builder = Host.CreateApplicationBuilder(args);

        builder.Services.AddSingleton<IGame, Game>();
        builder.Services.AddSingleton<IUtils, Utils>();
        builder.Services.AddSingleton<IState, State>();
        builder.Services.AddSingleton<ITacticHelper, TacticHelper>();
        builder.Services.AddSingleton<IMatchSimulator, MatchSimulator>();

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
        builder.Services.AddSingleton<IBaseScreen, PostMatchLeagueTableScreen>();
        builder.Services.AddSingleton<IBaseScreen, PostMatchScoreScreen>();
        builder.Services.AddSingleton<IBaseScreen, PlayerScreen>();
        builder.Services.AddSingleton<IBaseScreen, FinancesScreen>();

        builder.Services.AddSingleton<ICompetitionFactory, LeagueFactory>();

        var host = builder.Build();
        var game = host.Services.GetService<IGame>();
        if (game != null) game.Run();
    }
}