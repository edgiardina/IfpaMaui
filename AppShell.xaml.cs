using Ifpa.Views;
using Ifpa.Models;

namespace Ifpa;

public partial class AppShell : Shell
{
	public AppShell()
	{
		InitializeComponent();

        Routing.RegisterRoute("rankings-filter", typeof(RankingsFilterModalPage));
        Routing.RegisterRoute("player-search", typeof(PlayerSearchPage));
        Routing.RegisterRoute("player-details", typeof(PlayerDetailPage));
        Routing.RegisterRoute("player-results", typeof(PlayerResultsPage));
        Routing.RegisterRoute("activity-feed", typeof(ActivityFeedPage));
        Routing.RegisterRoute("pvp", typeof(PlayerVersusPlayerPage));
        Routing.RegisterRoute("pvp-detail", typeof(PlayerVersusPlayerDetailPage));        
        Routing.RegisterRoute("tournament-results", typeof(TournamentResultsPage));
        Routing.RegisterRoute("player-champ-series", typeof(PlayerChampionshipSeriesPage));

        Routing.RegisterRoute("champ-series", typeof(ChampionshipSeriesPage));
        Routing.RegisterRoute("champ-series-detail", typeof(ChampionshipSeriesDetailPage));
        Routing.RegisterRoute("champ-series-player", typeof(ChampionshipSeriesPlayerCardPage));

        Routing.RegisterRoute("calendar-detail", typeof(CalendarDetailPage));

        //more menu
        Routing.RegisterRoute("custom-ranking-details", typeof(CustomRankingsDetailPage));
        Routing.RegisterRoute("news-detail", typeof(NewsDetailPage));
    }

    protected override async void OnNavigating(ShellNavigatingEventArgs args)
    {
        base.OnNavigating(args);

        ShellNavigatingDeferral token = args.GetDeferral();

        //If a user hasn't set up my stats, redirect to player search

        try
        {
            if (!Settings.HasConfiguredMyStats
                && args.Target.Location.ToString().Contains("my-stats")
            )
            {
                await DisplayAlert("Configure your Stats", "Looks like you haven't configured your 'My Stats' page. Use the Player Search under 'Rankings' to find your Player, and press the Star to configure your Stats", "OK");
                args.Cancel();
            }

        }
        catch (Exception ex)
        {
            Console.WriteLine(ex.Message);
        }

        token?.Complete();
    }
}
