using FootballManagerSimulator.Maui.Interfaces;
using FootballManagerSimulator.Maui.Models;

namespace FootballManagerSimulator.Maui.Helpers;

public class PhysioHelper(IState state) : IPhysioHelper
{
    private readonly IState State = state;

    private ClubModel? MyClub => State.Clubs.FirstOrDefault(p => p.Id == State.MyClubId);

    public string HirePhysio()
    {
        if (MyClub is not { } club)
            return "You need a club to hire physios.";
        if (State.Physios.Count >= PhysioRules.MaxPhysios)
            return $"You can employ at most {PhysioRules.MaxPhysios} physios.";
        if (club.TransferBudget < PhysioRules.MonthlyWage)
            return $"The board cannot fund this. A physio costs £{PhysioRules.MonthlyWage:n0} per month.";

        var physio = new PhysioModel
        {
            Id = State.Physios.Count == 0 ? 1 : State.Physios.Max(p => p.Id) + 1,
            Name = new PlayerGenerator.PlayerGenerator().GeneratePlayer(State.Date.ToString("yyyy-MM-dd")).Name,
            HiredDate = State.Date
        };

        State.Physios.Add(physio);
        club.TransferBudget -= PhysioRules.MonthlyWage;
        return $"{physio.Name} has joined as a physio for £{PhysioRules.MonthlyWage:n0} per month. Your injury recovery is now reduced by {PhysioRules.InjuryReductionLabel(State.Physios.Count)}.";
    }

    public string DismissPhysio(int physioId)
    {
        var physio = State.Physios.FirstOrDefault(p => p.Id == physioId);
        if (physio == null)
            return "Physio not found.";

        State.Physios.Remove(physio);
        return $"{physio.Name} has been dismissed. Your injury recovery reduction is now {PhysioRules.InjuryReductionLabel(State.Physios.Count)}.";
    }
}
