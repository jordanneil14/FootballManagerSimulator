namespace FootballManagerSimulator.Maui.Services;

/// <summary>
/// The status bar height (in CSS pixels) that the WebView draws underneath. The native layer sets it
/// from the window insets and <c>SafeAreaStyle</c> exposes it to CSS as <c>--safe-top</c>.
/// </summary>
public static class SafeArea
{
    public static double Top { get; private set; }

    public static event Action? Changed;

    public static void SetTop(double top)
    {
        if (Math.Abs(Top - top) < 0.5) return;
        Top = top;
        Changed?.Invoke();
    }
}
