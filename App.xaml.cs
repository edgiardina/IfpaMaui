using Ifpa.Exceptions;
using Ifpa.Services;
using Serilog;
using Shiny.Notifications;

namespace Ifpa;

public partial class App : Application
{
    protected INotificationManager NotificationManager { get; set; }
    protected readonly NotificationService NotificationService;
    protected readonly IDeepLinkService DeepLinkService;

    public App(INotificationManager notificationManager, 
              NotificationService notificationService,
              IDeepLinkService deepLinkService)
    {
        // Try not to crash the app when an unexpected exception is thrown
        MauiExceptions.UnhandledException += (sender, e) =>
        {
            // get logger from DI container
            Log.Error(e.ExceptionObject as Exception, "Unhandled exception");
        };

        NotificationManager = notificationManager;
        NotificationService = notificationService;
        DeepLinkService = deepLinkService;

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
        if (Current is App app)
        {
            await app.DeepLinkService.HandleAppAction(appAction.Id);
        }
    }

    protected override async void OnAppLinkRequestReceived(Uri uri)
    {
        base.OnAppLinkRequestReceived(uri);
        await DeepLinkService.HandleDeepLink(uri);
    }
}
