using Android.App;
using Android.Content;
using Android.Content.PM;
using Android.OS;
using CommunityToolkit.Mvvm.Messaging;
using Ifpa.Models;
using Ifpa.Platforms.Android.Widgets;
using Ifpa.Services;
using Microsoft.Extensions.Logging;

namespace Ifpa;

[Activity(Theme = "@style/Maui.SplashTheme",
         MainLauncher = true,
         LaunchMode = LaunchMode.SingleTop,
         ConfigurationChanges = ConfigChanges.ScreenSize | ConfigChanges.Orientation | ConfigChanges.UiMode | ConfigChanges.ScreenLayout | ConfigChanges.SmallestScreenSize | ConfigChanges.Density)]

// App Actions (long press on Icon)
[IntentFilter(new[] { Platform.Intent.ActionAppAction },
              Categories = new[] { Intent.CategoryDefault })]
//https://github.com/dotnet/maui/issues/11684
// Deep linking from Web Content
[IntentFilter(new[] { Intent.ActionView },
          Categories = new[] {
              Intent.CategoryDefault,
              Intent.CategoryBrowsable
          },
          DataSchemes = new string[] { "http", "https" },
          DataHost = "www.ifpapinball.com",
          DataPaths = new string[] { "/player.php", "/tournaments/view.php" },
          AutoVerify = true)]
// Deep linking from Android Widgets
[IntentFilter(new[] { Intent.ActionView },
    Categories = new[] {
        Intent.CategoryDefault,
        Intent.CategoryBrowsable
    },
    DataScheme = "ifpa",
    DataPathPrefix = "/tournaments/view.php")]
public class MainActivity : MauiAppCompatActivity
{
    private ILogger<MainActivity> _logger;

    protected override void OnCreate(Bundle savedInstanceState)
    {
        base.OnCreate(savedInstanceState);
        _logger = IPlatformApplication.Current.Services.GetService<ILogger<MainActivity>>();

        if (!WeakReferenceMessenger.Default.IsRegistered<MyStatsPlayerChangedMessage>(this))
            WeakReferenceMessenger.Default.Register<MyStatsPlayerChangedMessage>(this, (_, _) =>
            {
                _logger?.LogDebug("MyStatsPlayerChangedMessage received — requesting RankWidget update");
                RankWidget.RequestUpdate(ApplicationContext);
            });

        if (!WeakReferenceMessenger.Default.IsRegistered<CalendarFilterChangedMessage>(this))
            WeakReferenceMessenger.Default.Register<CalendarFilterChangedMessage>(this, (_, _) =>
            {
                _logger?.LogDebug("CalendarFilterChangedMessage received — requesting CalendarWidget update");
                CalendarWidget.RequestUpdate(ApplicationContext);
            });

        HandleIntent(Intent);
    }

    protected override void OnDestroy()
    {
        WeakReferenceMessenger.Default.UnregisterAll(this);
        base.OnDestroy();
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