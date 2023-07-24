using Ifpa.ViewModels;
using PinballApi.Models.WPPR.v2.Series;
using Microsoft.Maui;


namespace Ifpa.Views
{

    [QueryProperty("SeriesCode", "seriesCode")]
    [QueryProperty("RegionCode", "regionCode")]
    [QueryProperty("Year", "year")]
    [XamlCompilation(XamlCompilationOptions.Compile)]
    public partial class ChampionshipSeriesDetailPage : ContentPage
    {
        ChampionshipSeriesDetailViewModel ViewModel;

        public string SeriesCode { get; set; }
        public string RegionCode { get; set; }  
        public int Year { get; set; }


        public ChampionshipSeriesDetailPage(ChampionshipSeriesDetailViewModel viewModel)
        {
            InitializeComponent();

            BindingContext = this.ViewModel = viewModel;
        }

        async void StandingsCollectionView_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            var playerStanding = e.CurrentSelection.FirstOrDefault() as RegionStanding;
            if (playerStanding == null)
                return;

            await Shell.Current.GoToAsync($"champ-series-player?seriesCode={SeriesCode}&regionCode={RegionCode}&year={Year}&playerId={playerStanding.PlayerId}");
         
            //Deselect Item
            ((CollectionView)sender).SelectedItem = null;
        }
        protected override async void OnNavigatedTo(NavigatedToEventArgs args)
        {
            base.OnNavigatedTo(args);

            ViewModel.Year = Year;
            ViewModel.RegionCode = RegionCode;
            ViewModel.SeriesCode = SeriesCode;

            ViewModel.LoadItemsCommand.Execute(null);
        }


        private async void TournamentResultsCollectionView_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            var tournament = e.CurrentSelection.FirstOrDefault() as SubmittedTournament;
            if (tournament == null)
                return;

            await Shell.Current.GoToAsync($"tournament-results?tournamentId={tournament.TournamentId}");

            //Deselect Item
            ((CollectionView)sender).SelectedItem = null;
        }
    }
}
