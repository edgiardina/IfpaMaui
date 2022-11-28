using Android.App;
using Android.Content;
using Android.Content.PM;
using Android.OS;

namespace Ifpa;

[Activity(Theme = "@style/Maui.SplashTheme", MainLauncher = true, ConfigurationChanges = ConfigChanges.ScreenSize | ConfigChanges.Orientation | ConfigChanges.UiMode | ConfigChanges.ScreenLayout | ConfigChanges.SmallestScreenSize | ConfigChanges.Density)]
[IntentFilter(new[] { Platform.Intent.ActionAppAction },
              Categories = new[] { Intent.CategoryDefault })]
//TODO: Support Deep linking?
//https://github.com/dotnet/maui/issues/11684
//[IntentFilter(new[] { Intent.ActionView },
//          Categories = new[] {
//              Intent.CategoryDefault,
//              Intent.CategoryBrowsable
//          },
//          DataSchemes = new string[] { "http", "https" },
//          DataHost = "www.ifpapinball.com",
//          DataPaths = new string[] { "/player.php", "/tournaments/view.php" },
//          AutoVerify = true)]
public class MainActivity : MauiAppCompatActivity
{
}