using Ifpa.ViewModels;
using PinballApi.Models.WPPR.Universal;
using PinballApi.Models.WPPR.Universal.Rankings;

namespace Ifpa.Views
{
    [XamlCompilation(XamlCompilationOptions.Compile)]
    public partial class RankingsPage : ContentPage
    {
        RankingsViewModel ViewModel;

        public RankingsPage(RankingsViewModel viewModel)
        {
            InitializeComponent();

            BindingContext = ViewModel = viewModel;
        }

        protected override async void OnAppearing()
        {
            if (ViewModel.Players.Count == 0)
            {
                ViewModel.CountOfItemsToFetch = Preferences.Get("PlayerCount", ViewModel.CountOfItemsToFetch);
                ViewModel.StartingPosition = Preferences.Get("StartingRank", ViewModel.StartingPosition);

                // if ranking type preference is 'Main', reset to 'Wppr' type
                if (Preferences.Get("RankingType", ViewModel.CurrentRankingType.ToString()) == "Main")
                {
                    Preferences.Set("RankingType", "Wppr");
                }

                ViewModel.CurrentRankingType = (RankingType)Enum.Parse(typeof(RankingType), Preferences.Get("RankingType", ViewModel.CurrentRankingType.ToString()));
                ViewModel.CurrentRankingSystem = (RankingSystem)Enum.Parse(typeof(RankingSystem), Preferences.Get("RankingSystem", ViewModel.CurrentRankingSystem.ToString()));
                ViewModel.CurrentProRankingType = (TournamentType)Enum.Parse(typeof(TournamentType), Preferences.Get("ProRankingType", ViewModel.CurrentProRankingType.ToString()));

                ViewModel.CountryToShow = new Country { CountryName = Preferences.Get("CountryName", ViewModel.DefaultCountry.CountryName) };

                ViewModel.LoadItemsCommand.Execute(null);
            }
            base.OnAppearing();
        }

        private async void FilterButton_Clicked(object sender, EventArgs e)
        {
            await Shell.Current.GoToAsync("rankings-filter");
        }

        private async void PlayersListView_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            var player = e.CurrentSelection.FirstOrDefault() as BaseRanking;
            if (player == null)
                return;

            await Shell.Current.GoToAsync($"player-details?playerId={player.PlayerId}");

            // Manually deselect item.
            PlayersListView.SelectedItem = null;
        }
    }
}