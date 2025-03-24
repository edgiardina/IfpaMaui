using Ifpa.ViewModels;

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

        protected override void OnAppearing()
        {
            base.OnAppearing();

            if (ViewModel.PlayerVersusPlayer.Count == 0)
            {
                ViewModel.PlayerOneId = PlayerId;
                ViewModel.PlayerTwoId = ComparePlayerId;
                ViewModel.LoadItems();
            }
        }
    }
}
