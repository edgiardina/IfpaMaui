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
using PinballApi;
using Microsoft.Maui.Controls.Compatibility.Hosting;
using SkiaSharp.Views.Maui.Controls.Hosting;
using Serilog;
using Shiny;

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
            //TODO: Maui Compatibility is required for iOS App Links; remove when the below bug is resolved
            //https://github.com/dotnet/maui/issues/12295
            .UseMauiCompatibility()
            .UseMauiCommunityToolkit()
            .UseMauiMaps()
            .UseFluentMauiIcons()
            .UseShiny()
            .UseSkiaSharp(true)
            .ConfigureSyncfusionCore()
            .ConfigureFonts(fonts =>
            {
                fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
                fonts.AddFont("OpenSans-Semibold.ttf", "OpenSansSemibold");
            })
            .ConfigureMauiHandlers((handlers) =>
            {
#if IOS
                handlers.AddHandler(typeof(InsetTableView), typeof(iOS.Renderers.InsetTableViewRenderer));
#endif
            })
            .ConfigureLogging()
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
            /*
            .ConfigureLifecycleEvents(events =>
            {
#if IOS
                events.AddiOS(ios => ios
                    .OpenUrl((app,url,opion) => LogEvent(app, url, opion)));

                static bool LogEvent(UIKit.UIApplication application, Foundation.NSUrl url, Foundation.NSDictionary options)
                {
                    Microsoft.Maui.Controls.Application.Current.SendOnAppLinkRequestReceived(url);
                    return true;
                }
#endif
            })
            */
            .RegisterShinyServices()
            .RegisterIfpaModels()
            .RegisterIfpaServices();

        return builder.Build();
    }

    static MauiAppBuilder RegisterIfpaModels(this MauiAppBuilder builder)
    {
        var s = builder.Services;
        var c = builder.Configuration;
        var appSettings = c.GetRequiredSection("AppSettings").Get<AppSettings>();

        //Add all viewmodels
        s.AddAllFromNamespace<BaseViewModel>();
        //Add all pages
        s.AddAllFromNamespace<RankingsPage>();
        //Adding RankingsViewModel as a singleton because it's injected into both RankingsPage
        //and RankingsFilterPage
        s.AddSingleton<RankingsViewModel>();

        s.AddSingleton(appSettings);

        return builder;
    }

    static MauiAppBuilder RegisterIfpaServices(this MauiAppBuilder builder)
    {
        var s = builder.Services;
        var c = builder.Configuration;
        var appSettings = c.GetRequiredSection("AppSettings").Get<AppSettings>();

        s.AddSingleton<BlogPostService>();
        s.AddSingleton<NotificationService>();
        s.AddTransient<IReminderService, ReminderService>();

        s.AddSingleton(x => new PinballRankingApiV1(appSettings.IfpaApiKey));
        s.AddSingleton(x => new PinballRankingApiV2(appSettings.IfpaApiKey));

        return builder;
    }

    static MauiAppBuilder RegisterShinyServices(this MauiAppBuilder builder)
    {
        var s = builder.Services;

        s.AddJobs();
        s.AddShinyCoreServices();

        s.AddJob(typeof(NotificationJob), requiredNetwork: Shiny.Jobs.InternetAccess.Any, runInForeground: true);

        // shiny.notifications
        s.AddNotifications(typeof(NotificationDelegate));

        return builder;
    }

    static MauiAppBuilder ConfigureLogging(this MauiAppBuilder builder)
    {
        Log.Logger = new LoggerConfiguration()
        .MinimumLevel.Debug()
        .WriteTo.Console()
#if ANDROID
		.WriteTo.AndroidLog()
        .Enrich.WithProperty(Serilog.Core.Constants.SourceContextPropertyName, "IFPA")
#endif
#if IOS
        .WriteTo.NSLog()
        .Enrich.WithProperty(Serilog.Core.Constants.SourceContextPropertyName, "IFPA")  
#endif
#if DEBUG
        .WriteTo.Debug()
#endif
        .CreateLogger();

        builder.Logging.AddSerilog(dispose: true);

        Log.Logger.Debug("Logger attached to services");

        return builder;
    }

}
