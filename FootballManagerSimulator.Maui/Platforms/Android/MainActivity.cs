using Android.App;
using Android.Content.PM;
using Android.OS;
using AndroidX.Core.View;
using FootballManagerSimulator.Maui.Services;

namespace FootballManagerSimulator.Maui;

[Activity(Theme = "@style/Maui.SplashTheme", MainLauncher = true, ConfigurationChanges = ConfigChanges.ScreenSize | ConfigChanges.Orientation | ConfigChanges.UiMode | ConfigChanges.ScreenLayout | ConfigChanges.SmallestScreenSize | ConfigChanges.Density)]
public class MainActivity : MauiAppCompatActivity
{
    protected override void OnCreate(Bundle? savedInstanceState)
    {
        base.OnCreate(savedInstanceState);

        if (Window != null)
        {
            // Android 15+ is edge-to-edge and ignores bar colours; the decor view shows through the bars instead.
            Window.DecorView.SetBackgroundColor(Android.Graphics.Color.ParseColor("#0e1116"));

            if (OperatingSystem.IsAndroidVersionAtLeast(29))
                Window.NavigationBarContrastEnforced = false;

            if (!OperatingSystem.IsAndroidVersionAtLeast(35))
            {
                Window.SetStatusBarColor(Android.Graphics.Color.ParseColor("#0e1116"));
                Window.SetNavigationBarColor(Android.Graphics.Color.ParseColor("#0e1116"));
            }

            var controller = WindowCompat.GetInsetsController(Window, Window.DecorView);
            controller.AppearanceLightStatusBars = false;
            controller.AppearanceLightNavigationBars = false;
        }

        var content = Window?.DecorView.FindViewById(Android.Resource.Id.Content);
        if (content == null) return;

        ViewCompat.SetOnApplyWindowInsetsListener(content, new InsetsListener());
        ViewCompat.RequestApplyInsets(content);
    }

    private sealed class InsetsListener : Java.Lang.Object, IOnApplyWindowInsetsListener
    {
        public WindowInsetsCompat OnApplyWindowInsets(Android.Views.View v, WindowInsetsCompat insets)
        {
            var bars = insets.GetInsets(WindowInsetsCompat.Type.SystemBars() | WindowInsetsCompat.Type.DisplayCutout());
            var ime = insets.GetInsets(WindowInsetsCompat.Type.Ime());
            // No top padding: the WebView draws behind the status bar so the page background shows there,
            // and the CSS pads its content by --safe-top instead.
            v.SetPadding(bars.Left, 0, bars.Right, Math.Max(bars.Bottom, ime.Bottom));
            SafeArea.SetTop(bars.Top / (v.Resources?.DisplayMetrics?.Density ?? 1f));
            return WindowInsetsCompat.Consumed;
        }
    }
}
