using Android.App;
using Android.Content.PM;
using Android.OS;
using AndroidX.Core.View;

namespace FootballManagerSimulator.Maui;

[Activity(Theme = "@style/Maui.SplashTheme", MainLauncher = true, ConfigurationChanges = ConfigChanges.ScreenSize | ConfigChanges.Orientation | ConfigChanges.UiMode | ConfigChanges.ScreenLayout | ConfigChanges.SmallestScreenSize | ConfigChanges.Density)]
public class MainActivity : MauiAppCompatActivity
{
    protected override void OnCreate(Bundle? savedInstanceState)
    {
        base.OnCreate(savedInstanceState);

        // Android 15+ draws edge-to-edge, which puts the app under the status and navigation bars.
        // Pad the content by the system bar (and keyboard) sizes so nothing is cut off.
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
            v.SetPadding(bars.Left, bars.Top, bars.Right, Math.Max(bars.Bottom, ime.Bottom));
            return WindowInsetsCompat.Consumed;
        }
    }
}
