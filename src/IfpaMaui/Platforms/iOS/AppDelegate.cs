using CoreSpotlight;
using Foundation;
using Ifpa.Models;
using Ifpa.Platforms.Services;
using Microsoft.Extensions.Logging;
using SQLitePCL;
using UIKit;

namespace Ifpa;

[Register("AppDelegate")]
public class AppDelegate : MauiUIApplicationDelegate
{
    //modified for iOS for SQLite
    //https://vladislavantonyuk.azurewebsites.net/articles/Adding-SQLite-to-the-.NET-MAUI-application
    protected override MauiApp CreateMauiApp()
    {
        raw.SetProvider(new SQLite3Provider_sqlite3());
        return MauiProgram.CreateMauiApp();
    }

    // Held for the lifetime of the app. WeakReferenceMessenger keeps only a
    // weak reference to its recipient, so letting this be collected would
    // silently stop the watch from being told about player changes.
    private WatchSessionService watchSession;

    public override bool FinishedLaunching(UIApplication application, NSDictionary launchOptions)
    {
        var finished = base.FinishedLaunching(application, launchOptions);

        var logger = IPlatformApplication.Current?.Services?.GetService<ILogger<WatchSessionService>>();
        watchSession = new WatchSessionService(logger);
        watchSession.Start();

        Settings.SyncCalendarFilterToAppGroup();

        return finished;
    }

    // A tap on a widget opens the app with the widget's URL. Those URLs use
    // the same ifpapinball.com links as a shared tournament, so they go
    // through the app-link handler.
    public override bool OpenUrl(UIApplication application, NSUrl url, NSDictionary options)
    {
        var handled = base.OpenUrl(application, url, options);

        if (url?.Host?.EndsWith("ifpapinball.com", StringComparison.OrdinalIgnoreCase) == true)
        {
            App.Current.SendOnAppLinkRequestReceived(new Uri(url.AbsoluteString));
            return true;
        }

        return handled;
    }

    // Covers the case where the watch app is installed while this app is in
    // the background. WatchStateDidChange handles it when the app is running,
    // and this catches the next launch or foreground either way.
    public override void WillEnterForeground(UIApplication application)
    {
        base.WillEnterForeground(application);
        watchSession?.SendPlayerId();
    }

    //TODO: these methods should be deprecated in favor of ConfigureLifeCycleEvents stuff
    //https://github.com/dotnet/maui/issues/14671
    public override bool ContinueUserActivity(UIApplication application, NSUserActivity userActivity, UIApplicationRestorationHandler completionHandler)
    {
        // Later when I do URL linking
        CheckForAppLink(userActivity);
        return true;
    }

    void CheckForAppLink(NSUserActivity userActivity)
    {
        var strLink = string.Empty;

        switch (userActivity.ActivityType)
        {
            case "NSUserActivityTypeBrowsingWeb":
                strLink = userActivity.WebPageUrl.AbsoluteString;
                break;
            case "com.apple.corespotlightitem":
                if (userActivity.UserInfo.ContainsKey(CSSearchableItem.ActivityIdentifier))
                    strLink = userActivity.UserInfo.ObjectForKey(CSSearchableItem.ActivityIdentifier).ToString();
                break;
            default:
                if (userActivity.UserInfo.ContainsKey(new NSString("link")))
                    strLink = userActivity.UserInfo[new NSString("link")].ToString();
                break;
        }

        if (!string.IsNullOrEmpty(strLink))
            App.Current.SendOnAppLinkRequestReceived(new Uri(strLink));
    }
}
