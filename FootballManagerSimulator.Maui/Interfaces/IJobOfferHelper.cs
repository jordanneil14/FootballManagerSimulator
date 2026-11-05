using FootballManagerSimulator.Maui.Models;

namespace FootballManagerSimulator.Maui.Interfaces;

public interface IJobOfferHelper
{
    double? Gap(ClubModel club, IReadOnlyDictionary<int, double>? standings = null);
    JobChance ChanceFor(ClubModel club, IReadOnlyDictionary<int, double>? standings = null);
    double AcceptChance(ClubModel club, IReadOnlyDictionary<int, double>? standings = null);
    int DaysUnemployed();
    JobApplicationModel? ApplicationFor(int clubId);
    JobRejectionModel? RejectionFor(int clubId);
    string? ApplyRefusal(ClubModel club, IReadOnlyDictionary<int, double>? standings = null);
    bool Apply(int clubId);
    void ProcessApplications();
    void ProcessUnemployment();
    int ReachableVacancies(IReadOnlyDictionary<int, double>? standings = null);
    void ProcessJobOffers();
    bool TryMakeJobOffer();
    void AcceptJobOffer(NotificationModel notification);
    void DeclineJobOffer(NotificationModel notification);
    IEnumerable<ClubModel> Vacancies();
    IEnumerable<ClubModel> ManagersUnderThreat();
    void ProcessVacancies();
    bool JoinClub(int clubId);
    void SackUser();
    void ResignUser();
}
