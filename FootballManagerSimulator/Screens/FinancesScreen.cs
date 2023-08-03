using FootballManagerSimulator.Enums;
using FootballManagerSimulator.Interfaces;
using FootballManagerSimulator.Structures;

namespace FootballManagerSimulator.Screens;

public class FinancesScreen : BaseScreen
{
    private readonly IState State;

    public FinancesScreen(IState state) : base(state)
    {
        State = state;
    }

    public override ScreenType Screen => ScreenType.Finances;

    public override void HandleInput(string input)
    {
        switch(input)
        {
            case "B":
                State.ScreenStack.Pop();
                break;
            case "C":
                State.UserFeedbackUpdates.Add("Request has been submitted");
                State.Notifications.Add(new Notification()
                {
                    Date = State.Date.AddDays(2),
                    Recipient = "Chairman",
                    Subject = "Extended Transfer Budget Request",
                    Message = $"Your request for the transfer budget to be extended from {State.MyClub.TransferBudgetFriendly} " + 
                        "has been rejected. We feel that the current allowance is enough for you to achieve your goals this season"
                });
                break;
            default:
                break;
        }
    }

    public override void RenderOptions()
    {
        Console.WriteLine("Options:");
        Console.WriteLine("B) Back");
        Console.WriteLine("C) Request a Higher Transfer Budget");
    }

    public override void RenderSubscreen()
    {
        Console.WriteLine("Finances\n");

        Console.WriteLine($"Transfer budget: {State.MyClub.TransferBudgetFriendly}");
        Console.WriteLine($"Wage Budget: {State.MyClub.WageBudgetFriendly}");
    }
}
