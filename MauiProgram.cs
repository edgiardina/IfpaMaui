using Ifpa.ViewModels;
using Ifpa.Models;
using Microsoft.Extensions.Configuration;
using System.Reflection;
using Ifpa.Views;
using Syncfusion.Maui.Core.Hosting;
using CommunityToolkit.Maui;
using Ifpa.Services;
using Ifpa.Interfaces;
using Plugin.LocalNotification;

namespace Ifpa;

public static class MauiProgram
{
    public static MauiApp CreateMauiApp()
    {
        var builder = MauiApp.CreateBuilder();
        builder
            .UseMauiApp<App>()
            .UseMauiCommunityToolkit()
            .ConfigureSyncfusionCore()
            .ConfigureFonts(fonts =>
            {
                fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
                fonts.AddFont("OpenSans-Semibold.ttf", "OpenSansSemibold");
            })
            .ConfigureEssentials(essentials =>
            {
                //TODO: it's unclear whether icons must be in the Resources/Images folder or in the Platforms/{platform} folder
                essentials
                    .AddAppAction("calendar", "Calendar", "IFPA Tournament Calendar", "app_info_action_icon")
                    .AddAppAction("my-stats", "My Stats", "Your IFPA player data", "app_info_action_icon")
                    .AddAppAction("rankings/player-search", "Player Search", "Search for other players in the IFPA database", "search.png")
                    .OnAppAction(App.HandleAppActions);

                essentials.UseVersionTracking();
            })
            .UseLocalNotification()
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
