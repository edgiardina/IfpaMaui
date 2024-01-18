using Ifpa.Models;
using Microsoft.Extensions.Configuration;
using Microsoft.Maui.Handlers;
using Shiny.Notifications;
using System.Web;

namespace Ifpa;

public partial class App : Application
{
    protected INotificationManager NotificationManager { get; set; }

    public App(AppSettings appSettings, INotificationManager notificationManager)
    {
        //TODO: can we move this to MauiProgram.cs?
        Syncfusion.Licensing.SyncfusionLicenseProvider.RegisterLicense(appSettings.SyncFusionLicenseKey);

        NotificationManager = notificationManager;

        InitializeComponent();

        MainPage = new AppShell();

        //TODO: this conditional compilation should be removed when this bug is fixed
        //https://github.com/dotnet/maui/issues/12295
#if IOS
        (Application.Current as IApplicationController)?.SetAppIndexingProvider(new Microsoft.Maui.Controls.Compatibility.Platform.iOS.IOSAppIndexingProvider());
#endif
    }

    protected override async void OnStart()
    {
        base.OnStart();

        await NotificationManager.RequestAccess();
    }

    public static void HandleAppActions(AppAction appAction)
    {
        App.Current.Dispatcher.Dispatch(async () =>
        {
            await Shell.Current.GoToAsync($"//{appAction.Id}");
        });
    }

    //Some places we can't Dependency Inject so we add this static helper
    public static AppSettings GetAppSettings()
    {
        var config = new ConfigurationBuilder()
           .SetBasePath(AppContext.BaseDirectory)
           .AddJsonPlatformBundle()
           .Build();

        return config.GetSection("AppSettings").Get<AppSettings>();
    }

    protected override async void OnAppLinkRequestReceived(Uri uri)
    {
        base.OnAppLinkRequestReceived(uri);

        //DeepLinks
        if (uri.ToString().Contains("player.php"))
        {
            //extract player ID from querystring
            var id = HttpUtility.ParseQueryString(uri.Query)["p"];

            if (!string.IsNullOrEmpty(id))
            {
                await Shell.Current.GoToAsync($"//rankings/player-details?playerId={id}");
            }
        }
        //tournaments/view.php?t=46773
        else if (uri.ToString().Contains("tournaments/view.php"))
        {
            var id = HttpUtility.ParseQueryString(uri.Query)["t"];
            if (!string.IsNullOrEmpty(id))
            {
                await Shell.Current.GoToAsync($"//rankings/tournament-results?tournamentId={id}");
            }
        }
    }
}
