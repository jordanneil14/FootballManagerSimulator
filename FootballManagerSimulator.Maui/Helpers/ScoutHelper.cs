using FootballManagerSimulator.Maui.Interfaces;
using FootballManagerSimulator.Maui.Models;
using FootballManagerSimulator.Maui.Services;

namespace FootballManagerSimulator.Maui.Helpers;

public class ScoutHelper(
    IState state,
    IPlayerHelper playerHelper,
    INotificationFactory notificationFactory) : IScoutHelper
{
    private readonly IState State = state;
    private readonly IPlayerHelper PlayerHelper = playerHelper;
    private readonly INotificationFactory NotificationFactory = notificationFactory;

    private ClubModel? MyClub => State.Clubs.FirstOrDefault(p => p.Id == State.MyClubId);

    // A new career starts out knowing a random mix of contracted players and free agents.
    public void SeedDiscoveredPlayers()
    {
        var count = RandomNumberHelper.Next(ScoutRules.StartingDiscoveredMin, ScoutRules.StartingDiscoveredMax + 1);
        var candidates = State.Players
            .Where(p => p.Contract?.ClubId != State.MyClubId || State.MyClubId == null)
            .OrderBy(_ => RandomNumberHelper.Next())
            .ToList();

        var freeAgents = candidates
            .Where(p => p.Contract == null)
            .Take((int)(count * ScoutRules.StartingFreeAgentShare));
        var contracted = candidates
            .Where(p => p.Contract != null)
            .Take(count - freeAgents.Count());
        var chosen = freeAgents.Concat(contracted).ToList();

        // Whichever group runs short, the rest of the pool makes up the numbers.
        chosen.AddRange(candidates.Except(chosen).Take(count - chosen.Count));

        State.DiscoveredPlayerIds = chosen.Select(p => p.Id).ToList();
    }

    // Retired players and your own squad are left out.
    public IEnumerable<PlayerModel> DiscoveredPlayers()
    {
        var discovered = State.DiscoveredPlayerIds.ToHashSet();
        return State.Players.Where(p => discovered.Contains(p.Id) && (p.Contract == null || p.Contract.ClubId != State.MyClubId));
    }

    // Mirrors the signing rules in GameEngine: retiring players won't sign, and nobody will
    // drop to a squad well below their level.
    public bool WillSign(PlayerModel player)
    {
        if (player.IsRetiring(State.Date))
            return false;

        if (player.Contract?.ClubId == State.MyClubId || MyClub is not { } club)
            return true;

        var ratings = State.Players
            .Where(p => p.Contract?.ClubId == club.Id)
            .Select(p => p.Rating)
            .OrderByDescending(r => r)
            .Take(GameEngine.SquadAverageBestPlayers)
            .ToList();

        return ratings.Count == 0 || player.Rating - ratings.Average() <= GameEngine.SigningRatingRange;
    }

    public bool MatchesAssignment(PlayerModel player, ScoutAssignmentModel assignment)
    {
        if (!string.IsNullOrEmpty(assignment.Position)
            && !player.PreferredPosition.Split('/', StringSplitOptions.TrimEntries).Contains(assignment.Position))
            return false;
        if (assignment.FreeAgent is { } freeAgent && (player.Contract == null) != freeAgent)
            return false;

        var age = player.GetAge(State.Date);
        if (assignment.MinAge is { } minAge && age < minAge) return false;
        if (assignment.MaxAge is { } maxAge && age > maxAge) return false;
        if (assignment.MinRating is { } minRating && player.Rating < minRating) return false;
        if (assignment.MaxRating is { } maxRating && player.Rating > maxRating) return false;
        if (assignment.MaxValue is { } maxValue && PlayerHelper.GetTransferValue(player) > maxValue) return false;

        return !assignment.OnlyWillSign || WillSign(player);
    }

    // The first month's wage is paid up front.
    public string HireScout()
    {
        if (MyClub is not { } club)
            return "You need a club to hire scouts.";
        if (State.Scouts.Count >= ScoutRules.MaxScouts)
            return $"You can employ at most {ScoutRules.MaxScouts} scouts.";
        if (club.TransferBudget < ScoutRules.MonthlyWage)
            return $"The board cannot fund this. A scout costs £{ScoutRules.MonthlyWage:n0} per month.";

        var scout = new ScoutModel
        {
            Id = State.Scouts.Count == 0 ? 1 : State.Scouts.Max(p => p.Id) + 1,
            Name = new PlayerGenerator.PlayerGenerator().GeneratePlayer(State.Date.ToString("yyyy-MM-dd")).Name,
            HiredDate = State.Date
        };

        State.Scouts.Add(scout);
        club.TransferBudget -= ScoutRules.MonthlyWage;
        return $"{scout.Name} has joined as a scout for £{ScoutRules.MonthlyWage:n0} per month. Give them an assignment to focus their search.";
    }

    public string DismissScout(int scoutId)
    {
        var scout = State.Scouts.FirstOrDefault(p => p.Id == scoutId);
        if (scout == null)
            return "Scout not found.";

        State.Scouts.Remove(scout);
        return $"{scout.Name} has been dismissed.";
    }

    public string SetAssignment(int scoutId, ScoutAssignmentModel? assignment)
    {
        var scout = State.Scouts.FirstOrDefault(p => p.Id == scoutId);
        if (scout == null)
            return "Scout not found.";

        scout.Assignment = assignment;
        return assignment == null
            ? $"{scout.Name} will search for any player."
            : $"{scout.Name}'s assignment has been updated.";
    }

    // Each scout reports once a month from the day they were hired, and is paid for the month ahead.
    public void ProcessScouts()
    {
        if (MyClub is not { } club)
            return;

        foreach (var scout in State.Scouts.Where(p => State.Date >= p.NextReportDate).ToList())
        {
            scout.MonthsWorked++;
            Report(scout);

            if (club.TransferBudget >= ScoutRules.MonthlyWage)
            {
                club.TransferBudget -= ScoutRules.MonthlyWage;
                continue;
            }

            State.Scouts.Remove(scout);
            NotificationFactory.AddNotification(
                State.Date,
                $"{scout.Name} (Scout)",
                "Scout Left",
                $"{scout.Name} has left the club because there was not enough in the budget to pay their £{ScoutRules.MonthlyWage:n0} monthly wage.");
        }
    }

    private void Report(ScoutModel scout)
    {
        var discovered = State.DiscoveredPlayerIds.ToHashSet();
        var assignment = scout.Assignment ?? new ScoutAssignmentModel();
        var found = State.Players
            .Where(p => !discovered.Contains(p.Id) && p.Contract?.ClubId != State.MyClubId && MatchesAssignment(p, assignment))
            .OrderBy(_ => RandomNumberHelper.Next())
            .Take(RandomNumberHelper.Next(1, ScoutRules.MaxPlayersPerReport + 1))
            .ToList();

        if (found.Count == 0)
        {
            NotificationFactory.AddNotification(
                State.Date,
                $"{scout.Name} (Scout)",
                "Scouting Report",
                $"I couldn't find any new players matching my assignment ({assignment.Describe()}) this month. I recommend you change my assignment on the Scout page to widen the search.");
            return;
        }

        State.DiscoveredPlayerIds.AddRange(found.Select(p => p.Id));

        var lines = found.Select(p =>
            $"{p.Name} ({p.PreferredPosition}, {p.Rating}, age {p.GetAge(State.Date)}) - {p.Contract?.ClubName ?? "Free Agent"}");

        NotificationFactory.AddNotification(
            State.Date,
            $"{scout.Name} (Scout)",
            "Scouting Report",
            $"I have found {found.Count} new player{(found.Count == 1 ? "" : "s")} for you this month. They have been added to the Scout page:\n\t{string.Join("\n\t", lines)}");
    }
}
