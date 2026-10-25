namespace FootballManagerSimulator.Maui.Models;

// The summer window runs 15 June - 31 August and the winter window covers January.
public static class TransferWindowRules
{
    public static bool IsOpen(DateOnly date) =>
        (date.Month >= 6 && date.Month <= 8 && !(date.Month == 6 && date.Day < 15))
        || date.Month == 1;

    // The last day of the window that is open on the given date.
    public static DateOnly ClosesOn(DateOnly date) =>
        date.Month == 1 ? new DateOnly(date.Year, 1, 31) : new DateOnly(date.Year, 8, 31);

    // The first day of the next window after the given (closed) date.
    public static DateOnly NextOpening(DateOnly date) =>
        date.Month < 6 || (date.Month == 6 && date.Day < 15)
            ? new DateOnly(date.Year, 6, 15)
            : new DateOnly(date.Year + 1, 1, 1);
}
