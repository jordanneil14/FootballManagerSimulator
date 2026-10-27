using FootballManagerSimulator.Maui.Models;

namespace FootballManagerSimulator.Maui.Interfaces;

public interface IScoutHelper
{
    void SeedDiscoveredPlayers();
    void EnsureDiscoveredPlayers();
    IEnumerable<PlayerModel> DiscoveredPlayers();
    bool WillSign(PlayerModel player);
    bool MatchesAssignment(PlayerModel player, ScoutAssignmentModel assignment);
    string HireScout();
    string DismissScout(int scoutId);
    string SetAssignment(int scoutId, ScoutAssignmentModel? assignment);
    void ProcessScouts();
}
