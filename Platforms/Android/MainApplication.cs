using Android.App;
using Android.Runtime;
using CommunityToolkit.Maui.ApplicationModel;
using Ifpa.Platforms.Android;

namespace Ifpa;

[Application]
public class MainApplication : MauiApplication
{
	public MainApplication(IntPtr handle, JniHandleOwnership ownership)
		: base(handle, ownership)
	{
        var samsungProvider = new SamsungBadgeProvider();
        BadgeFactory.SetBadgeProvider("com.sec.android.app.launcher", samsungProvider);
        BadgeFactory.SetBadgeProvider("com.sec.android.app.twlauncher", samsungProvider);
    }

	protected override MauiApp CreateMauiApp() => MauiProgram.CreateMauiApp();
}
