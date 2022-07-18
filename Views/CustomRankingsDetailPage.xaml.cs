using Microsoft.Maui;

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


        protected override void OnAppearing()
        {
            ViewModel.ViewId = ViewId;

            if (ViewModel.ViewResults.Count == 0)
                ViewModel.LoadItemsCommand.Execute(null);

            base.OnAppearing();
        }

        private async void TournamentListView_ItemSelected(object sender, SelectedItemChangedEventArgs e)
        {
            var tournament = e.SelectedItem as Tournament;
            if (tournament == null)
                return;


            await Shell.Current.GoToAsync($"tournament-results?tournamentId={tournament.TournamentId}");

            //Deselect Item
            ((ListView)sender).SelectedItem = null;
        }

        private async void RankingsListView_ItemSelected(object sender, SelectedItemChangedEventArgs e)
        {
            var result = e.SelectedItem as CustomRankingViewResult;
            if (result == null)
                return;

            await Shell.Current.GoToAsync($"player-details?playerId={result.PlayerId}");          

            //Deselect Item
            ((ListView)sender).SelectedItem = null;
        }
    }
}