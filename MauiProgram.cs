using CommunityToolkit.Maui;
using CommunityToolkit.Maui.ApplicationModel;
using Ifpa.BackgroundJobs;
using Ifpa.Caching;
using Ifpa.Controls;
using Ifpa.Interfaces;
using Ifpa.Models;
using Ifpa.Platforms.Renderers;
using Ifpa.Platforms.Services;
using Ifpa.Services;
using Ifpa.ViewModels;
using Ifpa.Views;
using LiveChartsCore.SkiaSharpView.Maui;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Maui.Controls.Compatibility.Hosting;
using PinballApi;
using PinballApi.Interfaces;
using Plugin.Maui.CalendarStore;
using Plugin.Maui.NativeCalendar;
using Serilog;
using Shiny;
using Shiny.Infrastructure;
using SkiaSharp.Views.Maui.Controls.Hosting;
using Syncfusion.Maui.Toolkit.Hosting;
using The49.Maui.BottomSheet;
using Microsoft.Extensions.DependencyInjection;

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
            .UseShiny()
            .UseLiveCharts()
            .UseSkiaSharp()
            .UseBottomSheet()
            .UseNativeCalendar()
            .ConfigureSyncfusionToolkit()
            .ConfigureFonts(fonts =>
            {
                fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
                fonts.AddFont("OpenSans-Semibold.ttf", "OpenSansSemibold");
                fonts.AddFont("FluentSystemIcons-Regular.ttf", "FluentRegular");
                fonts.AddFont("FluentSystemIcons-Filled.ttf", "FluentFilled");
            })
            .ConfigureMauiHandlers((handlers) =>
            {
                // Inset table view renderer is to make our settings look like native iOS settings
#if IOS
                handlers.AddHandler(typeof(InsetTableView), typeof(InsetTableViewRenderer));
#endif
            })
            .ConfigureLogging()
            .ConfigureEssentials(essentials =>
            {
                //TODO: it's unclear whether icons must be in the Resources/Images folder or in the Platforms/{platform} folder
                essentials
                    .AddAppAction("calendar", Strings.AppShell_Calendar, "IFPA Tournament Calendar", "calendar")
                    .AddAppAction("my-stats", Strings.AppShell_MyStats, "Your IFPA player data", "mystats")
                    .AddAppAction("rankings/player-search", Strings.PlayerSearchPage_Title, "Search for other players in the IFPA database", "search")
                    .OnAppAction(App.HandleAppActions);

                essentials.UseVersionTracking();
            })
            // Show custom Tabbar Badges for iOS and Android
            .ConfigureMauiHandlers(h =>
            {
                h.AddHandler<Shell, IfpaShellRenderer>();
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
        builder.Services.AddHttpClient<BlogPostService>(c =>
        {
            c.DefaultRequestHeaders.UserAgent.ParseAdd("IfpaMaui/1.0");
        });

        s.AddSingleton<NotificationService>();
        s.AddSingleton<IToolbarBadgeService, ToolbarBadgeService>();
        s.AddSingleton<IDeepLinkService, DeepLinkService>();
        s.AddSingleton<IPinballRankingApi>(sp =>
        {
            var online = new PinballRankingApi(appSettings.IfpaApiKey);
            var logger = sp.GetRequiredService<ILogger<CachingPinballRankingApi>>();
            return new CachingPinballRankingApi(online, logger);
        });
        s.AddSingleton(Geocoding.Default);
        s.AddSingleton(Badge.Default);
        s.AddSingleton(CalendarStore.Default);
        s.AddSingleton(Map.Default);

        return builder;
    }

    static MauiAppBuilder RegisterShinyServices(this MauiAppBuilder builder)
    {
        var s = builder.Services;

        s.AddJobs();
        s.AddShinyCoreServices();

        s.AddJob(typeof(NotificationJob), requiredNetwork: Shiny.Jobs.InternetAccess.Any);

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
        .WriteTo.File(Settings.LogFilePath,
            rollingInterval: RollingInterval.Day,
            retainedFileCountLimit: builder.Configuration.GetRequiredSection("AppSettings").Get<AppSettings>().LogRetentionDays)

        .CreateLogger();

        builder.Logging.AddSerilog(dispose: true);

        Log.Logger.Debug("Logger attached to services");

        return builder;
    }

}
