using FootballManagerSimulator.Maui.Interfaces;
using FootballManagerSimulator.Maui.Models;

namespace FootballManagerSimulator.Maui.Helpers;

public class AnalystHelper(IState state) : IAnalystHelper
{
    private readonly IState State = state;

    private ClubModel? MyClub => State.Clubs.FirstOrDefault(p => p.Id == State.MyClubId);

    public string HireAnalyst()
    {
        if (MyClub is not { } club)
            return "You need a club to hire an analyst.";
        if (State.Analysts.Count >= AnalystRules.MaxAnalysts)
            return "You can only employ one analyst.";
        if (club.TransferBudget < AnalystRules.MonthlyWage)
            return $"The board cannot fund this. An analyst costs £{AnalystRules.MonthlyWage:n0} per month.";

        var analyst = new AnalystModel
        {
            Id = State.Analysts.Count == 0 ? 1 : State.Analysts.Max(p => p.Id) + 1,
            Name = new PlayerGenerator.PlayerGenerator().GeneratePlayer(State.Date.ToString("yyyy-MM-dd")).Name,
            HiredDate = State.Date
        };

        State.Analysts.Add(analyst);
        club.TransferBudget -= AnalystRules.MonthlyWage;
        return $"{analyst.Name} has joined as your analyst for £{AnalystRules.MonthlyWage:n0} per month. You'll get a pre-match report the day before each competitive match.";
    }

    public string DismissAnalyst(int analystId)
    {
        var analyst = State.Analysts.FirstOrDefault(p => p.Id == analystId);
        if (analyst == null)
            return "Analyst not found.";

        State.Analysts.Remove(analyst);
        return $"{analyst.Name} has been dismissed. You will no longer receive pre-match reports.";
    }
}
