using CoreSpotlight;
using Foundation;
using Microsoft.Extensions.Logging;
using UIKit;

namespace Ifpa;

// Apps built with Xcode 27 must use the UIScene lifecycle, otherwise iOS kills
// them right after the splash screen. Once scenes are on, UIKit stops calling the
// app delegate's URL, user-activity and foreground callbacks, so that handling
// lives here now. Always call base: MAUI creates the window and forwards
// lifecycle events from these methods.
// https://devblogs.microsoft.com/dotnet/migrate-dotnet-maui-apps-uikit-scene-lifecycle/
[Register("SceneDelegate")]
public class SceneDelegate : MauiUISceneDelegate
{
    // A cold start delivers the launching URL or user activity here instead of
    // through OpenUrlContexts/ContinueUserActivity. base creates the MAUI window,
    // and the links are dispatched so Shell is ready before navigation starts.
    public override void WillConnect(UIScene scene, UISceneSession session, UISceneConnectionOptions connectionOptions)
    {
        base.WillConnect(scene, session, connectionOptions);

        var urls = connectionOptions?.UrlContexts?.ToArray().Select(c => c.Url).ToList() ?? [];
        var activities = connectionOptions?.UserActivities?.ToArray() ?? [];

        if (urls.Count == 0 && activities.Length == 0)
            return;

        MainThread.BeginInvokeOnMainThread(() =>
        {
            foreach (var url in urls)
                HandleUrl(url);

            foreach (var activity in activities)
                HandleUserActivity(activity);
        });
    }

    // A tap on a widget opens the app with the widget's URL. Those URLs use
    // the same ifpapinball.com links as a shared tournament, so they go
    // through the app-link handler.
    public override bool OpenUrl(UIScene scene, NSSet<UIOpenUrlContext> urlContexts)
    {
        var handled = base.OpenUrl(scene, urlContexts);

        if (urlContexts == null)
            return handled;

        foreach (var context in urlContexts.ToArray())
            handled |= HandleUrl(context.Url);

        return handled;
    }

    public override bool ContinueUserActivity(UIScene scene, NSUserActivity userActivity)
    {
        base.ContinueUserActivity(scene, userActivity);
        HandleUserActivity(userActivity);
        return true;
    }

    // Covers the case where the watch app is installed while this app is in
    // the background. WatchStateDidChange handles it when the app is running,
    // and this catches the next foreground.
    public override void WillEnterForeground(UIScene scene)
    {
        base.WillEnterForeground(scene);
        (UIApplication.SharedApplication.Delegate as AppDelegate)?.WatchSession?.SendPlayerId();
    }

    static bool HandleUrl(NSUrl url)
    {
        if (url?.Host?.EndsWith("ifpapinball.com", StringComparison.OrdinalIgnoreCase) != true)
            return false;

        SendAppLink(url.AbsoluteString);
        return true;
    }

    static void HandleUserActivity(NSUserActivity userActivity)
    {
        if (userActivity == null)
            return;

        var link = userActivity.ActivityType switch
        {
            "NSUserActivityTypeBrowsingWeb" =>userActivity.WebPageUrl?.AbsoluteString,
            "com.apple.corespotlightitem" => userActivity.UserInfo?.ObjectForKey(CSSearchableItem.ActivityIdentifier)?.ToString(),
            _ => userActivity.UserInfo?.ObjectForKey(new NSString("link"))?.ToString(),
        };

        SendAppLink(link);
    }

    static void SendAppLink(string link)
    {
        if (string.IsNullOrEmpty(link))
            return;

        if (!Uri.TryCreate(link, UriKind.Absolute, out var uri))
        {
            IPlatformApplication.Current?.Services?.GetService<ILogger<SceneDelegate>>()
                ?.LogWarning("Ignoring app link that isn't an absolute URI: {Link}", link);
            return;
        }

        Application.Current?.SendOnAppLinkRequestReceived(uri);
    }
}
