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

        protected override void OnAppearing()
        {            
            base.OnAppearing();

            ViewModel.LoadItemsCommand.Execute(null);
        }

        private async void PlayersListView_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            var player = e.CurrentSelection.FirstOrDefault() as Player;
            if (player == null)
                return;

            await Shell.Current.GoToAsync($"player-details?playerId={player.PlayerId}");

            // Manually deselect item.
            PlayersListView.SelectedItem = null;
        }
    }
}