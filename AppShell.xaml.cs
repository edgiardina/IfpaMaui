using Ifpa.Views;

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

        Routing.RegisterRoute("champ-series", typeof(ChampionshipSeriesPage));
        Routing.RegisterRoute("champ-series-detail", typeof(ChampionshipSeriesDetailPage));
        Routing.RegisterRoute("champ-series-player", typeof(ChampionshipSeriesPlayerCardPage));

        Routing.RegisterRoute("calendar-detail", typeof(CalendarDetailPage));


        //more menu
        Routing.RegisterRoute("custom-ranking-details", typeof(CustomRankingsDetailPage));
        Routing.RegisterRoute("news-detail", typeof(NewsDetailPage));
    }
}
