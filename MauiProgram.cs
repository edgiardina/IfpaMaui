using Ifpa.ViewModels;
using Ifpa.Models;
using Microsoft.Extensions.Configuration;
using System.Reflection;
using Ifpa.Views;
using Syncfusion.Maui.Core.Hosting;

namespace Ifpa;

public static class MauiProgram
{
    public static MauiApp CreateMauiApp()
    {
        var builder = MauiApp.CreateBuilder();
        builder
            .UseMauiApp<App>()
            .ConfigureSyncfusionCore()
            .ConfigureFonts(fonts =>
            {
                fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
                fonts.AddFont("OpenSans-Semibold.ttf", "OpenSansSemibold");
            })
            .Services
                //Add all viewmodels
                .AddAllFromNamespace<BaseViewModel>()
                //Add all pages
                .AddAllFromNamespace<RankingsPage>();

        var a = Assembly.GetExecutingAssembly();
        using var stream = a.GetManifestResourceStream("Ifpa.appsettings.json");

        var config = new ConfigurationBuilder()
                    .AddJsonStream(stream)
                    .Build();

        builder.Configuration.AddConfiguration(config);

        return builder.Build();
    }
}
