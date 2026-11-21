using Foundation;
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
    // SceneDelegate reads this to resend the player id when the app returns
    // to the foreground.
    internal WatchSessionService WatchSession { get; private set; }

    public override bool FinishedLaunching(UIApplication application, NSDictionary launchOptions)
    {
        var finished = base.FinishedLaunching(application, launchOptions);

        var logger = IPlatformApplication.Current?.Services?.GetService<ILogger<WatchSessionService>>();
        WatchSession = new WatchSessionService(logger);
        WatchSession.Start();

        return finished;
    }
}
