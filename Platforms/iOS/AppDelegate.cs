using Foundation;
using SQLitePCL;

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
}
