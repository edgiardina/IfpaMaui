using Microsoft.Maui.Handlers;

namespace Ifpa;

public partial class App : Application
{
    public App()
    {
        InitializeComponent();

        MainPage = new AppShell();

        WindowHandler.Mapper.ModifyMapping(nameof(IWindow.Content), OnWorkaround);
    }
        
    public static void HandleAppActions(AppAction appAction)
    {
        App.Current.Dispatcher.Dispatch(async () =>
        {
            await Shell.Current.GoToAsync($"//{appAction.Id}");
        });
    }


    //workaround for modal dialog presentation on android
    //https://github.com/dotnet/maui/issues/8062
    private void OnWorkaround(IWindowHandler arg1, IWindow arg2, Action<IElementHandler, IElement> arg3)
    {
        WindowHandler.MapContent(arg1, arg2);
    }
}
