using Microsoft.Maui;

using Ifpa.ViewModels;
using PinballApi.Models.WPPR.v2.Players;

namespace Ifpa.Views
{
    [XamlCompilation(XamlCompilationOptions.Compile)]
    public partial class FavoritesPage : ContentPage
    {
        FavoritesViewModel ViewModel;

        public FavoritesPage(FavoritesViewModel viewModel)
        {
            InitializeComponent();

            BindingContext = ViewModel = viewModel;
        }

        async void OnItemSelected(object sender, SelectedItemChangedEventArgs args)
        {
            var player = args.SelectedItem as Player;
            if (player == null)
                return;

            await Shell.Current.GoToAsync($"player-details?playerId={player.PlayerId}");

            // Manually deselect item.
            PlayersListView.SelectedItem = null;
        }

        protected override void OnAppearing()
        {
            ViewModel.LoadItemsCommand.Execute(null);
            base.OnAppearing();
        }
    }
}