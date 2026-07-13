using Ifpa.ViewModels;
using PinballApi.Models.WPPR.Universal.Players;

namespace Ifpa.Views
{
    [QueryProperty("PlayerId", "playerId")]
    [XamlCompilation(XamlCompilationOptions.Compile)]
    public partial class PlayerVersusPlayerPage : ContentPage
    {
        PlayerVersusPlayerViewModel ViewModel;

        public int PlayerId { get; set; }

        public PlayerVersusPlayerPage(PlayerVersusPlayerViewModel viewModel)
        {
            InitializeComponent();

            BindingContext = this.ViewModel = viewModel;
        }

        protected override void OnAppearing()
        {
            base.OnAppearing();

            if (ViewModel.AllResults.Count == 0)
            {
                ViewModel.PlayerId = PlayerId;
                ViewModel.LoadAllItemsCommand.Execute(null);
            }
        }
    }
}
