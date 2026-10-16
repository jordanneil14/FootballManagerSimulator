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
        var friendy = new FriendlyModel
        {
            Id = competition.Id,
            Name = competition.Name,
            Clubs = State.Clubs.ToList(),
            DrawSettings = competition.DrawSettings.Select(p => new DrawSettingsModel
            {
                Round = p.Round,
                FixtureDate = p.FixtureDate
            }).ToList()
        };

        return friendy;
    }
}
