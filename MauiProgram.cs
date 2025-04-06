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
using MauiIcons.Fluent;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Maui.Controls.Compatibility.Hosting;
using PinballApi;
using PinballApi.Interfaces;
using Plugin.Maui.CalendarStore;
using Plugin.Maui.NativeCalendar;
using Polly;
using Serilog;
using Shiny;
using Shiny.Infrastructure;
using SkiaSharp.Views.Maui.Controls.Hosting;
using Syncfusion.Maui.Core.Hosting;
using The49.Maui.BottomSheet;

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
            .UseSkiaSharp()
            .UseBottomSheet()
            .UseNativeCalendar()
            .ConfigureSyncfusionCore()
            .ConfigureFonts(fonts =>
            {
                fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
                fonts.AddFont("OpenSans-Semibold.ttf", "OpenSansSemibold");
            })
            .ConfigureMauiHandlers((handlers) =>
            {
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
                h.AddHandler<Shell, TabbarBadgeRenderer>();
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
        s.AddSingleton<IToolbarBadgeService, ToolbarBadgeService>();

        s.AddSingleton(x => new PinballRankingApiV2(appSettings.IfpaApiKey));
        s.AddSingleton<PinballRankingApi>(x => new PinballRankingApi(appSettings.IfpaApiKey));

        // Set up caching for IPinballRankingApi
        builder.Services.AddSingleton<IPinballRankingApi>(provider =>
        {
            var api = provider.GetRequiredService<PinballRankingApi>();
            var logger = provider.GetRequiredService<ILogger<CachingProxy<IPinballRankingApi>>>();

            return CachingProxyFactory.Create<IPinballRankingApi>(api, logger);

            /*
             var dbPath = Path.Combine(FileSystem.AppDataDirectory, "app_cache.db");
            var sqliteCacheProvider = new SQLiteCacheProvider(dbPath);

            var cachingPolicy = CachingPolicyFactory.CreatePolicy(sqliteCacheProvider, logger);

            return CachingProxyFactory.Create<IPinballRankingApi>(api, cachingPolicy, logger);
             
             */
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
