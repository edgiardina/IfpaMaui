using Ifpa.Exceptions;
using Ifpa.Services;
using Serilog;
using Shiny.Notifications;
using System.Web;

namespace Ifpa;

public partial class App : Application
{
    protected INotificationManager NotificationManager { get; set; }
    protected readonly NotificationService NotificationService;  

    public App(INotificationManager notificationManager, NotificationService notificationService)
    {
        // Try not to crash the app when an unexpected exception is thrown
        MauiExceptions.UnhandledException += (sender, e) =>
        {
            // get logger from DI container
            Log.Error(e.ExceptionObject as Exception, "Unhandled exception");
        };

        NotificationManager = notificationManager;
        NotificationService = notificationService;

        InitializeComponent();

        MainPage = new AppShell();

        //TODO: this conditional compilation should be removed when this bug is fixed
        //https://github.com/dotnet/maui/issues/12295
#if IOS
        (Application.Current as IApplicationController)?.SetAppIndexingProvider(new Microsoft.Maui.Controls.Compatibility.Platform.iOS.IOSAppIndexingProvider());
#endif

        // From https://github.com/borrmann/AppThemeBindingFix
        // This git issue summarizes the bug with bottomsheets and AppTheme issues; 
        // if the user dismisses the app to the background with a bottomsheet open,
        // when restored the app switches themes unexpectedly
        // https://github.com/the49ltd/The49.Maui.BottomSheet/issues/89
        DeviceThemeService.Instance.ReloadRequestedTheme();

        Current.RequestedThemeChanged += (sender, args) => {
            DeviceThemeService.Instance.ReloadRequestedTheme();
        };
    }

    protected override async void OnStart()
    {
        base.OnStart();

        await NotificationManager.RequestAccess();

        await NotificationService.RecalculateActivityFeedAndUpdateBadges(null);
    }

    public static async void HandleAppActions(AppAction appAction)
    {
        var route = $"//{appAction.Id}";

        Current.Dispatcher.DispatchDelayed(TimeSpan.FromMilliseconds(500), async () =>
        {
            await Shell.Current.GoToAsync(route);
        });
        await Task.Delay(500);
        ((AppShell)Shell.Current).ConfirmSelectedTabIsCorrect(route);
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
                var route = $"//rankings/player-details?playerId={id}";

                Current.Dispatcher.DispatchDelayed(TimeSpan.FromMilliseconds(500), async () =>
                {
                    await Shell.Current.GoToAsync(route);                    
                });
                await Task.Delay(500);
                ((AppShell)Shell.Current).ConfirmSelectedTabIsCorrect(route);
            }
        }
        //tournaments/view.php?t=46773
        else if (uri.ToString().Contains("tournaments/view.php"))
        {
            var id = HttpUtility.ParseQueryString(uri.Query)["t"];
            if (!string.IsNullOrEmpty(id))
            {
                var route = $"//rankings/tournament-results?tournamentId={id}";

                Current.Dispatcher.DispatchDelayed(TimeSpan.FromMilliseconds(500), async () =>
                {
                    await Shell.Current.GoToAsync(route);
                });
                await Task.Delay(500);
                ((AppShell)Shell.Current).ConfirmSelectedTabIsCorrect(route);
            }
        }
    }
}
