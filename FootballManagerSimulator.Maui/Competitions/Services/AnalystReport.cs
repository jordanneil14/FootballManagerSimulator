using FootballManagerSimulator.Maui.Enums;
using FootballManagerSimulator.Maui.Interfaces;
using FootballManagerSimulator.Maui.Models;

namespace FootballManagerSimulator.Maui.Competitions.Services;

// The analyst's pre-match report, sent the day before a competitive match. Only clubs with an
// analyst get one. The analyst is a capable but not outstanding one: the formation and the
// opponent's plan are right, but the strength comparison is rough and the advice general.
public static class AnalystReport
{
    public const string Subject = "Pre-Match Report";
    public const int PlayersToWatch = 3;
    public const int FormMatches = 5;
    // Our starters below this fitness are flagged, at most MaxTiredPlayers of them.
    public const double TiredFitness = 75;
    public const int MaxTiredPlayers = 3;

    // situation: a line about the competition, e.g. "They're 4th in the league with 38 points, ..."
    public static void Send(IState state, INotificationFactory notificationFactory, FixtureModel fixture, string situation)
    {
        var analyst = state.Analysts.FirstOrDefault();
        if (analyst == null)
            return;

        var opponentIsHome = fixture.HomeClub.Id != state.MyClubId;
        var opponent = opponentIsHome ? fixture.HomeClub : fixture.AwayClub;
        var subject = $"{Subject}: {opponent.Name} ({(opponentIsHome ? "A" : "H")})";
        notificationFactory.AddNotification(state.Date, analyst.Name, subject, Build(state, fixture, situation));
    }

    public static string Build(IState state, FixtureModel fixture, string situation)
    {
        var opponentIsHome = fixture.HomeClub.Id != state.MyClubId;
        var opponent = state.Clubs.FirstOrDefault(c => c.Id == (opponentIsHome ? fixture.HomeClub.Id : fixture.AwayClub.Id))
            ?? (opponentIsHome ? fixture.HomeClub : fixture.AwayClub);
        var myClub = state.Clubs.FirstOrDefault(c => c.Id == state.MyClubId);
        var plan = (opponentIsHome ? fixture.HomePlannedInstructions : fixture.AwayPlannedInstructions)
            ?? opponent.Instructions
            ?? new TeamInstructionsModel();

        var theirXi = BestXi(state, opponent.Id, p => IsAvailable(p, fixture.Date));
        var myXi = myClub == null ? [] : MyXi(state, myClub, fixture.Date);

        var lines = new List<string>
        {
            $"Boss, here's what I've got on {opponent.Name} ahead of tomorrow's {(opponentIsHome ? "away" : "home")} game.",
            "",
            "THE OPPOSITION",
            Sentences(situation, Form(state, opponent.Id), StrengthLine(theirXi, myXi)),
            ""
        };

        var watch = theirXi.OrderByDescending(p => p.Rating).Take(PlayersToWatch).ToList();
        if (watch.Count > 0)
        {
            lines.Add("Players to watch:");
            lines.AddRange(watch.Select(p => $"• {p.Name} ({p.PreferredPosition}){GoalsLine(state, p.Id)}"));
            lines.Add("");
        }

        var missing = BestXi(state, opponent.Id, _ => true).Where(p => !IsAvailable(p, fixture.Date)).ToList();
        if (missing.Count > 0)
        {
            lines.Add($"Not available: {string.Join(", ", missing.Select(p => $"{p.Name} ({p.PreferredPosition}, {(p.SuspensionMatches > 0 ? "suspended" : "injured")})"))}.");
            lines.Add("");
        }

        lines.Add("HOW THEY'LL SET UP");
        if (!string.IsNullOrWhiteSpace(opponent.Formation))
            lines.Add($"• {opponent.Formation}");
        lines.Add($"• {MentalityLine(plan.Mentality)}");
        lines.Add($"• {PressingLine(plan.Pressing)}");
        lines.Add("");

        lines.Add("MY ADVICE");
        lines.AddRange(Advice(plan, myXi).Select(t => $"• {t}"));

        return string.Join("\n", lines);
    }

    private static bool IsAvailable(PlayerModel player, DateOnly matchDate) =>
        player.SuspensionMatches == 0 && !player.IsInjured(matchDate);

    // The side a club would pick from these players: the best keeper and the ten best outfielders.
    private static List<PlayerModel> BestXi(IState state, int clubId, Func<PlayerModel, bool> include)
    {
        var squad = state.Players
            .Where(p => p.Contract?.ClubId == clubId && include(p))
            .OrderByDescending(p => p.Rating)
            .ToList();
        var keeper = squad.FirstOrDefault(FitnessRules.IsGoalkeeper);
        var outfield = squad.Where(p => !FitnessRules.IsGoalkeeper(p)).Take(10);
        return (keeper == null ? outfield : outfield.Prepend(keeper)).ToList();
    }

    // Our current XI if one is picked, otherwise the side we'd be expected to pick.
    private static List<PlayerModel> MyXi(IState state, ClubModel myClub, DateOnly matchDate)
    {
        var picked = myClub.TacticSlots
            .Where(s => s.TacticSlotType is not (TacticSlotType.SUB or TacticSlotType.RES) && s.PlayerId != null)
            .Select(s => state.Players.FirstOrDefault(p => p.Id == s.PlayerId))
            .OfType<PlayerModel>()
            .ToList();
        return picked.Count == 11 ? picked : BestXi(state, myClub.Id, p => IsAvailable(p, matchDate));
    }

    private static string Sentences(params string?[] parts) =>
        string.Join(" ", parts.Where(p => !string.IsNullOrWhiteSpace(p)));

    // Their last few competitive results, oldest first, e.g. "Form: W W D L W."
    private static string? Form(IState state, int clubId)
    {
        var results = state.Competitions
            .Where(c => c.Type != CompetitionType.Friendly)
            .SelectMany(c => c.Fixtures)
            .Where(f => f.Concluded && f.GoalsHome != null && f.GoalsAway != null && (f.HomeClub.Id == clubId || f.AwayClub.Id == clubId))
            .OrderByDescending(f => f.Date)
            .Take(FormMatches)
            .Reverse()
            .Select(f =>
            {
                var (scored, conceded) = f.HomeClub.Id == clubId ? (f.GoalsHome, f.GoalsAway) : (f.GoalsAway, f.GoalsHome);
                return scored > conceded ? "W" : scored < conceded ? "L" : "D";
            })
            .ToList();
        return results.Count == 0 ? null : $"Form: {string.Join(" ", results)}.";
    }

    // A rough comparison of the two XIs, allowing for tired legs.
    private static string? StrengthLine(List<PlayerModel> theirXi, List<PlayerModel> myXi)
    {
        var theirs = theirXi.Sum(p => p.Rating * FitnessRules.StrengthFactor(p.Fitness));
        var ours = myXi.Sum(p => p.Rating * FitnessRules.StrengthFactor(p.Fitness));
        if (theirs <= 0 || ours <= 0)
            return null;

        var ratio = theirs / ours;
        return ratio switch
        {
            >= 1.06 => "On paper their likely XI is clearly stronger than ours.",
            >= 1.02 => "On paper their likely XI is a bit stronger than ours.",
            > 0.98 => "On paper the two sides look evenly matched.",
            > 0.94 => "On paper their likely XI is a bit weaker than ours.",
            _ => "On paper their likely XI is clearly weaker than ours."
        };
    }

    // Competitive goals this season (friendlies and shoot-out kicks don't count), if any.
    private static string GoalsLine(IState state, int playerId)
    {
        var goals = state.Competitions
            .Where(c => c.Type != CompetitionType.Friendly)
            .SelectMany(c => c.Fixtures)
            .Where(f => f.Concluded)
            .SelectMany(f => f.HomeScorers.Concat(f.AwayScorers))
            .Count(g => g.PlayerId == playerId && g.Minute <= 120);
        return goals == 0 ? "" : $": {goals} goal{(goals == 1 ? "" : "s")} this season";
    }

    private static string MentalityLine(Mentality mentality) => mentality switch
    {
        Mentality.Defensive => "Defensive: they'll sit back and try to keep it tight.",
        Mentality.Attacking => "Attacking: they'll come at us and get bodies forward.",
        _ => "Balanced: nothing unusual, a fairly even approach."
    };

    private static string PressingLine(Pressing pressing) => pressing switch
    {
        Pressing.High => "High press: they'll be in our faces early.",
        Pressing.Low => "Low press: they'll drop off and let us have the ball.",
        _ => "Normal pressing: nothing to worry about."
    };

    private static List<string> Advice(TeamInstructionsModel plan, List<PlayerModel> myXi)
    {
        var tips = new List<string>();

        if (plan.Mentality == Mentality.Defensive)
            tips.Add("They'll be hard to break down. If we're the better side, we may need to be more attacking.");
        else if (plan.Mentality == Mentality.Attacking)
            tips.Add("They'll leave space behind. If we're second best, sitting deeper could frustrate them.");

        if (plan.Pressing == Pressing.High)
            tips.Add("A press like that usually starts to fade after half-time. Once it does, we can hit them on the counter, or press them ourselves while our legs are fresher.");

        var tired = myXi
            .Where(p => p.Fitness < TiredFitness)
            .OrderBy(p => p.Fitness)
            .Take(MaxTiredPlayers)
            .ToList();
        if (tired.Count > 0)
        {
            var names = tired.Select((p, i) => $"{p.Name} ({Math.Round(p.Fitness):0}{(i == 0 ? " fitness" : "")})").ToList();
            var list = names.Count == 1 ? names[0] : $"{string.Join(", ", names.Take(names.Count - 1))} and {names[^1]}";
            tips.Add($"{list} {(tired.Count == 1 ? "is" : "are")} still feeling the last game. Worth thinking about a rest.");
        }

        if (tips.Count == 0)
            tips.Add("Nothing in their set-up we need to change our approach for.");

        return tips;
    }
}
