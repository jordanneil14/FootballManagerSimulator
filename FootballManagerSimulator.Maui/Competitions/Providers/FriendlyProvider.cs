using FootballManagerSimulator.Maui.Competitions.Services;
using FootballManagerSimulator.Maui.Enums;
using FootballManagerSimulator.Maui.Interfaces;
using FootballManagerSimulator.Maui.Models;

namespace FootballManagerSimulator.Maui.Competitions.Providers;

public class FriendlyProvider(
    IState state,
    FriendlyService friendlyService) : ICompetitionProvider
{
    private readonly IState State = state;
    private readonly FriendlyService FriendlyService = friendlyService;
    public ICompetitionService CompetitionService => FriendlyService;

    public CompetitionType Type => CompetitionType.Friendly;

    public ICompetition CreateCompetition(CompetitionModel competition)
    {
        var currentYear = State.Date.Year;

        var friendy = new FriendlyModel
        {
            Id = competition.Id,
            Name = competition.Name,
            Year = currentYear,
            Clubs = State.Clubs.ToList(),
            DrawSettings = competition.DrawSettings.Select(p => new DrawSettingsModel
            {
                Round = p.Round,
                FixtureDate = new DateOnly(currentYear, p.FixtureDate.Month, p.FixtureDate.Day)
            }).ToList()
        };

        return friendy;
    }
}
