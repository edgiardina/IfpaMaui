using Ifpa.ViewModels;
using PinballApi.Models.WPPR.v2.Players;
using PinballApi.Models.WPPR.v2.Tournaments;
using System.Collections.ObjectModel;

namespace Ifpa.Views
{
    [QueryProperty("PlayerId", "playerId")]
    [XamlCompilation(XamlCompilationOptions.Compile)]
    public partial class PlayerResultsPage : ContentPage
    {
        PlayerResultsViewModel ViewModel;
        public int PlayerId { get; set; }
        public ObservableCollection<string> Items { get; set; }

        public PlayerResultsPage(PlayerResultsViewModel viewModel)
        {
            InitializeComponent();

            BindingContext = this.ViewModel = viewModel;            
        }

        protected override void OnAppearing()
        {
            base.OnAppearing();

            if (ViewModel.ActiveResults.Count == 0)
            {
                ViewModel.PlayerId = PlayerId;
                ViewModel.LoadItemsCommand.Execute(null);
            }
        }

        private async void RankingProfileButton_Clicked(object sender, EventArgs e)
        {
            string action = await DisplayActionSheet("Ranking Profile", "Cancel", null, ViewModel.RankingTypeOptions.Select(a => a.ToString()).ToArray());
            
            if (action != "Cancel")
            {
                ViewModel.RankingType = (RankingType)Enum.Parse(typeof(RankingType), action);
                ViewModel.LoadItemsCommand.Execute(null);
            }
        }

        private async void ActiveListView_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            var tournament = e.CurrentSelection.FirstOrDefault() as PlayerResult;
            if (tournament == null)
                return;

            await Shell.Current.GoToAsync($"tournament-results?tournamentId={tournament.TournamentId}");

            //Deselect Item
            ((CollectionView)sender).SelectedItem = null;
        }
    }
}
