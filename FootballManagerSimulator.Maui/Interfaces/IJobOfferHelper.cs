using FootballManagerSimulator.Maui.Models;

namespace FootballManagerSimulator.Maui.Interfaces;

public interface IJobOfferHelper
{
    void ProcessJobOffers();
    bool TryMakeJobOffer();
    void AcceptJobOffer(NotificationModel notification);
    void DeclineJobOffer(NotificationModel notification);
}
