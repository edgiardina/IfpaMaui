using Ifpa.ViewModels;
using Ifpa.Models;
using Microsoft.Extensions.Configuration;
using Ifpa.Views;
using Syncfusion.Maui.Core.Hosting;
using CommunityToolkit.Maui;
using Ifpa.Services;
using Ifpa.Interfaces;
using MauiIcons.Fluent;
using Ifpa.BackgroundJobs;
using Ifpa.Controls;
using Shiny.Infrastructure;

namespace Ifpa;

public static class MauiProgram
{
    public static MauiApp CreateMauiApp()
    {
        var builder = MauiApp.CreateBuilder();

        //pull in appsettings.json
        builder.Configuration.AddJsonPlatformBundle();

        builder
            .UseMauiApp<App>()
            .UseMauiCommunityToolkit()
            .UseMauiMaps()
            .UseFluentMauiIcons()
            .UseShiny()
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
            .RegisterShinyServices()
            .Services
                //Add all viewmodels
                .AddAllFromNamespace<ViewModels.BaseViewModel>()
                //Add all pages
                .AddAllFromNamespace<RankingsPage>()
                //Adding RankingsViewModel as a singleton because it's injected into both RankingsPage
                //and RankingsFilterPage
                .AddSingleton<RankingsViewModel>()
                //Services
                .AddSingleton<BlogPostService>()
                .AddSingleton<NotificationService>()
                .AddTransient<IReminderService, ReminderService>();

        return builder.Build();
    }

    static MauiAppBuilder RegisterShinyServices(this MauiAppBuilder builder)
    {
        var s = builder.Services;

        s.AddJobs();
        s.AddShinyCoreServices();

        s.AddJob(typeof(NotificationJob));

        // shiny.notifications
        s.AddNotifications();

        return builder;
    }

}
