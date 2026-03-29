using Ifpa.Views;
using Ifpa.Models;
using Serilog;

namespace Ifpa;

public partial class AppShell : Shell
{
    public AppShell()
    {
        InitializeComponent();

        Routing.RegisterRoute("rankings-filter", typeof(RankingsFilterModalPage));
        Routing.RegisterRoute("player-details", typeof(PlayerDetailPage));
        Routing.RegisterRoute("player-details-no-player-selected", typeof(PlayerDetailNoPlayerSelectedPage));
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
        Routing.RegisterRoute("related-tournaments", typeof(RelatedTournamentsPage));

        // more menu
        Routing.RegisterRoute("favorites", typeof(FavoritesPage));
        Routing.RegisterRoute("custom-rankings", typeof(CustomRankingsPage));
        Routing.RegisterRoute("custom-ranking-details", typeof(CustomRankingsDetailPage));
        Routing.RegisterRoute("news", typeof(NewsPage));
        Routing.RegisterRoute("news-detail", typeof(NewsDetailPage));
        Routing.RegisterRoute("rules", typeof(RulesPage));
        Routing.RegisterRoute("stats", typeof(StatsPage));
        Routing.RegisterRoute("directors", typeof(DirectorsPage));
        Routing.RegisterRoute("settings", typeof(SettingsPage));
        Routing.RegisterRoute("about", typeof(AboutPage));
        Routing.RegisterRoute("tournament-search", typeof(TournamentSearchPage));

        // modals
        Routing.RegisterRoute("tournament-info", typeof(TournamentInfoPage));
    }

    protected override async void OnNavigating(ShellNavigatingEventArgs args)
    {
        base.OnNavigating(args);

        ShellNavigatingDeferral token = args.GetDeferral();

        // Prevent user from re-navigating to empty My Stats page
        try
        {
            if (!Settings.HasConfiguredMyStats
                && args.Target.Location.ToString().Contains("my-stats")
                && args.Current.Location.ToString().Contains("player-details-no-player-selected")
            )
            {
                 args.Cancel();
            }

        }
        catch (Exception ex)
        {
            //TODO: dependency inject this?
            Log.Logger.Error(ex, "Error checking if user has configured my stats");
        }

        token?.Complete();

        await Task.CompletedTask;
    }
    // TODO: this is a hack to get the correct tab to show when navigating to a page
    // https://github.com/dotnet/maui/issues/16568
    public void ConfirmSelectedTabIsCorrect(string route)
    {
        if (route.Contains("rankings"))
        {
            MainTabBar.CurrentItem = RankingsTab;
        }
        else if (route.Contains("champ-series-list"))
        {
            MainTabBar.CurrentItem = ChampionshipSeriesTab;
        }
        else if (route.Contains("calendar"))
        {
            MainTabBar.CurrentItem = CalendarTab;
        }
        else if (route.Contains("my-stats"))
        {
            MainTabBar.CurrentItem = MyStatsTab;
        }
        else if (route.Contains("more"))
        {
            MainTabBar.CurrentItem = MoreTab;
        }
    }
}
