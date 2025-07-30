using Android.App;
using Android.Content;
using Android.Content.PM;
using Android.OS;
using Ifpa.Services;

namespace Ifpa;

[Activity(Theme = "@style/Maui.SplashTheme", 
         MainLauncher = true, 
         LaunchMode = LaunchMode.SingleTop,
         ConfigurationChanges = ConfigChanges.ScreenSize | ConfigChanges.Orientation | ConfigChanges.UiMode | ConfigChanges.ScreenLayout | ConfigChanges.SmallestScreenSize | ConfigChanges.Density)]
[IntentFilter(new[] { Platform.Intent.ActionAppAction },
              Categories = new[] { Intent.CategoryDefault })]
//https://github.com/dotnet/maui/issues/11684
[IntentFilter(new[] { Intent.ActionView },
          Categories = new[] {
              Intent.CategoryDefault,
              Intent.CategoryBrowsable
          },
          DataSchemes = new string[] { "http", "https" },
          DataHost = "www.ifpapinball.com",
          DataPaths = new string[] { "/player.php", "/tournaments/view.php" },
          AutoVerify = true)]
public class MainActivity : MauiAppCompatActivity
{
    protected override void OnCreate(Bundle savedInstanceState)
    {
        base.OnCreate(savedInstanceState);

        HandleIntent(Intent);
    }

    protected override void OnNewIntent(Intent intent)
    {
        base.OnNewIntent(intent);
        HandleIntent(intent);
    }

    private void HandleIntent(Intent intent)
    {
        if (intent?.Data != null)
        {
            var uri = new Uri(intent.Data.ToString());
            var deepLinkService = IPlatformApplication.Current.Services.GetService<IDeepLinkService>();
            MainThread.BeginInvokeOnMainThread(async () =>
            {
                await deepLinkService.HandleDeepLink(uri);
            });
        }
    }
}