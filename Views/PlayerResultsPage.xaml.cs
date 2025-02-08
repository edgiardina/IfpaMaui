using Ifpa.ViewModels;
using PinballApi.Models.WPPR.Universal.Players;

namespace Ifpa.Views
{
    [QueryProperty("PlayerId", "playerId")]
    [XamlCompilation(XamlCompilationOptions.Compile)]
    public partial class PlayerResultsPage : ContentPage
    {
        PlayerResultsViewModel ViewModel;
        public int PlayerId { get; set; }

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
                ViewModel.LoadItems();
            }
        }

        private async void RankingProfileButton_Clicked(object sender, EventArgs e)
        {
            string action = await DisplayActionSheet(Strings.PlayerResultsPage_RankingProfile, Strings.Cancel, null, ViewModel.RankingTypeOptions.Select(a => a.ToString()).ToArray());

            if (action != Strings.Cancel)
            {
                ViewModel.RankingType = (PlayerRankingSystem)Enum.Parse(typeof(PlayerRankingSystem), action);
                ViewModel.LoadItems();
            }
        }
    }
}
