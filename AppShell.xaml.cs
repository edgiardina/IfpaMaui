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

        Routing.RegisterRoute("champ-series", typeof(ChampionshipSeriesPage));
        Routing.RegisterRoute("champ-series-detail", typeof(ChampionshipSeriesDetailPage));
        Routing.RegisterRoute("champ-series-player", typeof(ChampionshipSeriesPlayerCardPage));
    }
}
