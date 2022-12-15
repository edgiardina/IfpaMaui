using Ifpa.ViewModels;
using Ifpa.Models;
using Microsoft.Extensions.Configuration;
using System.Reflection;
using Ifpa.Views;
using Syncfusion.Maui.Core.Hosting;
using CommunityToolkit.Maui;
using Ifpa.Services;
using Ifpa.Interfaces;
using Microsoft.Maui.LifecycleEvents;
using MauiIcons.Fluent;
using Ifpa.Controls;
using Microsoft.Maui.Controls.Compatibility.Hosting;

namespace Ifpa;

public static class MauiProgram
{
    public static MauiApp CreateMauiApp()
    {
        var builder = MauiApp.CreateBuilder();
        builder
            .UseMauiApp<App>()
            .UseMauiCommunityToolkit()
            .UseMauiMaps()
            .UseFluentMauiIcons()
            .ConfigureSyncfusionCore()
            .ConfigureFonts(fonts =>
            {
                fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
                fonts.AddFont("OpenSans-Semibold.ttf", "OpenSansSemibold");
            })
            .ConfigureMauiHandlers((handlers) => {
#if IOS
               handlers.AddHandler(typeof(InsetTableView), typeof(Ifpa.iOS.Renderers.InsetTableViewRenderer)); 
#endif
            })
            .ConfigureEssentials(essentials =>
            {
                //TODO: it's unclear whether icons must be in the Resources/Images folder or in the Platforms/{platform} folder
                essentials
                    .AddAppAction("calendar", "Calendar", "IFPA Tournament Calendar", "calendar")
                    .AddAppAction("my-stats", "My Stats", "Your IFPA player data", "mystats")
                    .AddAppAction("rankings/player-search", "Player Search", "Search for other players in the IFPA database", "search")
                    .OnAppAction(App.HandleAppActions);

                essentials.UseVersionTracking();
            })
            .ConfigureLifecycleEvents(events =>
            {
                //Set up android Background Recevier
                //https://stackoverflow.com/questions/71766108/how-to-use-a-broadcastreceiver-from-net-maui-on-android
#if ANDROID
                events.AddAndroid(android => android
                      .OnCreate((activity, bundle) => Ifpa.Platforms.Droid.AndroidAlarmManager.CreateAlarm()));                      
#elif IOS
                //events.AddiOS(ios => ios.DidEnterBackground*);    
                
#endif


            })
            //.UseLocalNotification()
            .Services
                //Add all viewmodels
                .AddAllFromNamespace<BaseViewModel>()
                //Add all pages
                .AddAllFromNamespace<RankingsPage>()
                //Adding RankingsViewModel as a singleton because it's injected into both RankingsPage
                //and RankingsFilterPage
                .AddSingleton<RankingsViewModel>()
                //Services
                .AddSingleton<BlogPostService>()
                .AddSingleton<NotificationService>()
                .AddTransient<IReminderService, ReminderService>();
        

        var a = Assembly.GetExecutingAssembly();
        using var stream = a.GetManifestResourceStream("Ifpa.appsettings.json");

        var config = new ConfigurationBuilder()
                    .AddJsonStream(stream)
                    .Build();

        builder.Configuration.AddConfiguration(config);

        return builder.Build();
    }

}
