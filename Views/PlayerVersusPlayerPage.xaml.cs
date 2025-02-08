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

        async void Handle_ItemTapped(object sender, ItemTappedEventArgs e)
        {            
            var pvp = e.Item as PlayerVersusRecord;
            if (pvp == null)
                return;

            await Shell.Current.GoToAsync($"pvp-detail?playerId={ViewModel.PlayerId}&comparePlayerId={pvp.PlayerId}");

            //Deselect Item
            ((ListView)sender).SelectedItem = null;
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

        private async void InfoButton_Clicked(object sender, System.EventArgs e)
        {
            string action = await DisplayActionSheet("PVP Type", null, null, "All", "Top 250");

            if(action == "All")
            {
                ViewModel.LoadAllItemsCommand.Execute(null);
                MyListView.SetBinding(ListView.ItemsSourceProperty, "AllResults"); 
            }
            else
            {
                ViewModel.LoadEliteItemsCommand.Execute(null);
                MyListView.SetBinding(ListView.ItemsSourceProperty, "EliteResults");
            }
        }
    }
}
