using Ifpa.ViewModels;
using PinballApi.Models.WPPR.v2.Rankings;

namespace Ifpa.Views
{
    [QueryProperty("ViewId", "viewId")]
    [XamlCompilation(XamlCompilationOptions.Compile)]
    public partial class CustomRankingsDetailPage : ContentPage
    {
        CustomRankingsDetailViewModel ViewModel;

        public int ViewId { get; set; }

        public CustomRankingsDetailPage(CustomRankingsDetailViewModel viewModel)
        {
            InitializeComponent();

            BindingContext = ViewModel = viewModel;
        }

        protected override async void OnNavigatedTo(NavigatedToEventArgs args)
        {
            base.OnNavigatedTo(args);

            ViewModel.ViewId = ViewId;

            if (ViewModel.ViewResults.Count == 0)
                ViewModel.LoadItemsCommand.Execute(null);
        }


        private async void RankingsListView_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            var result = e.CurrentSelection.FirstOrDefault() as CustomRankingViewResult;
            if (result == null)
                return;

            await Shell.Current.GoToAsync($"player-details?playerId={result.PlayerId}");

            //Deselect Item
            ((CollectionView)sender).SelectedItem = null;
        }

        private async void TournamentListView_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            var tournament = e.CurrentSelection.FirstOrDefault() as Tournament;
            if (tournament == null)
                return;


            await Shell.Current.GoToAsync($"tournament-results?tournamentId={tournament.TournamentId}");

            //Deselect Item
            ((CollectionView)sender).SelectedItem = null;
        }
    }
}