using Microsoft.Maui.Handlers;
using Plugin.LocalNotification;
using Plugin.LocalNotification.EventArgs;
using System.Web;

namespace Ifpa;

public partial class App : Application
{
    public App()
    {
        InitializeComponent();

        MainPage = new AppShell();

        LocalNotificationCenter.Current.NotificationActionTapped += OnNotificationActionTapped;

        WindowHandler.Mapper.ModifyMapping(nameof(IWindow.Content), OnWorkaround);
    }
        
    public static void HandleAppActions(AppAction appAction)
    {
        App.Current.Dispatcher.Dispatch(async () =>
        {
            await Shell.Current.GoToAsync($"//{appAction.Id}");
        });
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
                Shell.Current.CurrentItem = Shell.Current.CurrentItem.Items[0];
                await Shell.Current.GoToAsync($"//rankings/player-details?playerId={id}");
            }
        }
        //tournaments/view.php?t=46773
        else if (uri.ToString().Contains("tournaments/view.php"))
        {
            var id = HttpUtility.ParseQueryString(uri.Query)["t"];
            if (!string.IsNullOrEmpty(id))
            {
                Shell.Current.CurrentItem = Shell.Current.CurrentItem.Items[0];
                await Shell.Current.GoToAsync($"//rankings/tournament-results?tournamentId={id}");
            }
        }
    }

    private async void OnNotificationActionTapped(NotificationEventArgs e)
    {
        await Shell.Current.GoToAsync(e.Request.ReturningData);
    }


    //workaround for modal dialog presentation on android
    //https://github.com/dotnet/maui/issues/8062
    private void OnWorkaround(IWindowHandler arg1, IWindow arg2, Action<IElementHandler, IElement> arg3)
    {
        WindowHandler.MapContent(arg1, arg2);
    }
}
