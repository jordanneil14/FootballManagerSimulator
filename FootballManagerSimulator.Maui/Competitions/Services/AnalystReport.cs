using FootballManagerSimulator.Maui.Interfaces;
using FootballManagerSimulator.Maui.Models;

namespace FootballManagerSimulator.Maui.Competitions.Services;

// The analyst's pre-match report, sent the day before a competitive match. Only clubs with an
// analyst get one. The analyst is a capable but not outstanding one: the formation and the
// opponent's plan are right, but the advice is general rather than detailed.
public static class AnalystReport
{
    public const int PlayersToWatch = 3;

    // situation: a line about the competition, e.g. "They sit 4th in the league."
    public static void Send(IState state, INotificationFactory notificationFactory, FixtureModel fixture, string situation)
    {
        var analyst = state.Analysts.FirstOrDefault();
        if (analyst == null)
            return;

        notificationFactory.AddNotification(state.Date, analyst.Name, "Pre-Match Report", Build(state, fixture, situation));
    }

    public static string Build(IState state, FixtureModel fixture, string situation)
    {
        var opponentIsHome = fixture.HomeClub.Id != state.MyClubId;
        var opponent = state.Clubs.FirstOrDefault(c => c.Id == (opponentIsHome ? fixture.HomeClub.Id : fixture.AwayClub.Id))
            ?? (opponentIsHome ? fixture.HomeClub : fixture.AwayClub);
        var plan = (opponentIsHome ? fixture.HomePlannedInstructions : fixture.AwayPlannedInstructions)
            ?? opponent.Instructions
            ?? new TeamInstructionsModel();

        var playersToWatch = state.Players
            .Where(p => p.Contract != null && p.Contract.ClubId == opponent.Id)
            .OrderByDescending(p => p.Rating)
            .Take(PlayersToWatch)
            .Select(p => $"{p.Name} ({p.PreferredPosition})");

        return $"I've had a look at {opponent.Name} ahead of tomorrow's match. {situation}\n\n" +
               $"Players to watch:\n\t{string.Join("\n\t", playersToWatch)}\n\n" +
               "How I expect them to set up:\n" +
               $"\tFormation: {opponent.Formation}\n" +
               $"\tMentality: {plan.Mentality}. {MentalityLine(plan.Mentality)}\n" +
               $"\tPressing: {plan.Pressing}. {PressingLine(plan.Pressing)}\n\n" +
               $"{Advice(plan)}\n" +
               "That's what I could pick up from their recent games, so treat it as a guide rather than gospel.";
    }

    private static string MentalityLine(Mentality mentality) => mentality switch
    {
        Mentality.Defensive => "They'll sit back and try to keep it tight.",
        Mentality.Attacking => "They'll come at us and look to get forward in numbers.",
        _ => "Nothing unusual. They'll play a fairly balanced game."
    };

    private static string PressingLine(Pressing pressing) => pressing switch
    {
        Pressing.High => "They'll press us high, so expect a busy first half.",
        Pressing.Low => "They'll drop off and let us have the ball.",
        _ => "A normal amount of pressing, nothing to worry about."
    };

    private static string Advice(TeamInstructionsModel plan)
    {
        var tips = new List<string>();

        if (plan.Mentality == Mentality.Defensive)
            tips.Add("If we're the better side, we might need to be more attacking to break them down.");
        else if (plan.Mentality == Mentality.Attacking)
            tips.Add("They'll leave gaps at the back. If we're up against it, sitting deeper could frustrate them.");

        if (plan.Pressing == Pressing.High)
            tips.Add("A side that presses like that usually tires in the second half. Once they do, we can either sit off and hit them on the counter, or switch to a high press ourselves and squeeze them while our legs are fresher.");

        return tips.Count == 0
            ? "I don't see anything we need to change our approach for."
            : string.Join(" ", tips);
    }
}
