using Microsoft.Maui;
using Ifpa.ViewModels;
using PinballApi.Models.WPPR.v1.Players;

namespace Ifpa.Views
{
    [XamlCompilation(XamlCompilationOptions.Compile)]
    public partial class PlayerSearchPage : ContentPage
    {
        PlayerSearchViewModel ViewModel;

        public PlayerSearchPage(PlayerSearchViewModel viewModel)
        {
            InitializeComponent();

            BindingContext = ViewModel = viewModel;
        }

        protected override void OnAppearing()
        {
            base.OnAppearing();
        }

        private async void PlayersListView_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (e.CurrentSelection.FirstOrDefault() != null)
            {
                var player = e.CurrentSelection.FirstOrDefault() as Search;
                if (player == null)
                    return;

                await Shell.Current.GoToAsync($"player-details?playerId={player.PlayerId}");

                // Manually deselect item.
                PlayersListView.SelectedItem = null;
            }
        }
    }
}