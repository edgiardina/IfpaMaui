﻿using CommunityToolkit.Maui;
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
using Plugin.Maui.FitText;
using Plugin.Maui.NativeCalendar;
using Serilog;
using Shiny;
using Shiny.Infrastructure;
using SkiaSharp.Views.Maui.Controls.Hosting;
using Syncfusion.Maui.Toolkit.Hosting;
using The49.Maui.BottomSheet;

namespace Ifpa;

public static class MauiProgram
{
    public static MauiApp CreateMauiApp()
    {
        var builder = MauiApp.CreateBuilder();

        // pull in appsettings.json
        builder.Configuration.AddJsonPlatformBundle();
        var appSettings = builder.Configuration.GetRequiredSection(nameof(AppSettings)).Get<AppSettings>() ?? new AppSettings();
        builder.Services.AddSingleton(appSettings);

        builder
            .UseMauiApp<App>()
            // TODO: Maui Compatibility is required for iOS App Links; remove when the below bug is resolved
            // https://github.com/dotnet/maui/issues/12295
            .UseMauiCompatibility()
            .UseMauiCommunityToolkit()
            .UseMauiMaps()
            .UseShiny()
            .UseLiveCharts()
            .UseSkiaSharp()
            .UseBottomSheet()
            .UseNativeCalendar()
            .UseFitText()
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
#if IOS
                // Inset table view renderer is to make our settings look like native iOS settings
                handlers.AddHandler(typeof(InsetTableView), typeof(InsetTableViewRenderer));
#endif
            })
            .ConfigureLogging(appSettings)
            .ConfigureEssentials(essentials =>
            {
                essentials
                    .AddAppAction("calendar", Strings.AppShell_Calendar, "IFPA Tournament Calendar", "calendar.png")
                    .AddAppAction("my-stats", Strings.AppShell_MyStats, "Your IFPA player data", "person.png")
                    .AddAppAction("rankings", Strings.PlayerSearchPage_Title, "Search for other players in the IFPA database", "search.png")
                    .OnAppAction(App.HandleAppActions);

                essentials.UseVersionTracking();
            })
            // Show custom Tabbar Badges for iOS and Android
            .ConfigureMauiHandlers(h =>
            {
                h.AddHandler<Shell, IfpaShellRenderer>();
            })
            .RegisterShinyServices()
            .RegisterIfpaModels()
            .RegisterIfpaServices();


        return builder.Build();
    }

    static MauiAppBuilder RegisterIfpaModels(this MauiAppBuilder builder)
    {
        var s = builder.Services;

        // Add all viewmodels
        s.AddAllFromNamespace<BaseViewModel>();
        // Add all pages
        s.AddAllFromNamespace<RankingsPage>();
        // RankingsViewModel is used by multiple pages
        s.AddSingleton<RankingsViewModel>();

        return builder;
    }

    static MauiAppBuilder RegisterIfpaServices(this MauiAppBuilder builder)
    {
        var s = builder.Services;

        // Typed HttpClient for BlogPostService; remove any separate singleton registration
        s.AddHttpClient<BlogPostService>(c =>
        {
            c.DefaultRequestHeaders.UserAgent.ParseAdd("IfpaMaui/1.0");
        });

        s.AddSingleton<NotificationService>();
        s.AddSingleton<IToolbarBadgeService, ToolbarBadgeService>();
        s.AddSingleton<IDeepLinkService, DeepLinkService>();

        // IPinballRankingApi that uses AppSettings injected directly
        s.AddSingleton<IPinballRankingApi>(sp =>
        {
            var settings = sp.GetRequiredService<AppSettings>();
            var online = new PinballRankingApi(settings.IfpaApiKey);
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

    static MauiAppBuilder ConfigureLogging(this MauiAppBuilder builder, AppSettings appSettings)
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
            .WriteTo.File(
                Settings.LogFilePath,
                rollingInterval: RollingInterval.Day,
                retainedFileCountLimit: appSettings.LogRetentionDays
            )
            .CreateLogger();

        builder.Logging.AddSerilog(dispose: true);

        Log.Logger.Debug("Logger attached to services");

        return builder;
    }
}
