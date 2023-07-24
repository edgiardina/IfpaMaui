using Ifpa.ViewModels;
using PinballApi.Models.WPPR.v2.Players;
using Microsoft.Maui;


namespace Ifpa.Views
{
    [QueryProperty("PlayerId", "playerId")]
    [QueryProperty("ComparePlayerId", "comparePlayerId")]
    [XamlCompilation(XamlCompilationOptions.Compile)]
    public partial class PlayerVersusPlayerDetailPage : ContentPage
    {
        PlayerVersusPlayerDetailViewModel ViewModel;

        public int PlayerId { get; set; }

        public int ComparePlayerId { get; set; }

        public PlayerVersusPlayerDetailPage(PlayerVersusPlayerDetailViewModel viewModel)
        {
            InitializeComponent();

            BindingContext = this.ViewModel = viewModel; 
        }

        async void Handle_ItemTapped(object sender, ItemTappedEventArgs e)
        {            
            var tournament = e.Item as PlayerVersusPlayerComparisonRecord;
            if (tournament == null)
                return;

            await Shell.Current.GoToAsync($"tournament-results?tournamentId={tournament.TournamentId}");

            //Deselect Item
            ((ListView)sender).SelectedItem = null;
        }
        protected override async void OnNavigatedTo(NavigatedToEventArgs args)
        {
            base.OnNavigatedTo(args);

            if (ViewModel.PlayerVersusPlayer.Count == 0)
            {
                ViewModel.PlayerOneId = PlayerId;
                ViewModel.PlayerTwoId = ComparePlayerId;
                ViewModel.LoadItemsCommand.Execute(null);
            }
        }
    }
}
