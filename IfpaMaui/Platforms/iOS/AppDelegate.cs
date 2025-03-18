using CoreSpotlight;
using Foundation;
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
