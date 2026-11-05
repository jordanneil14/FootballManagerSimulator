using FootballManagerSimulator.Maui.Helpers;
using FootballManagerSimulator.Maui.Models;

namespace FootballManagerSimulator.Maui.Interfaces;

public interface IReputationHelper
{
    double Reputation { get; }
    void Change(double amount);
    IReadOnlyDictionary<int, double> Standings();
    double LeagueWeight(int leagueId, IReadOnlyDictionary<int, double> standings);
    int? ExpectedFinish(ClubModel club);
    void SetExpectedFinish();
    SeasonReview? ReviewSeason();
    void CompleteSeason(SeasonReview? review);
}
