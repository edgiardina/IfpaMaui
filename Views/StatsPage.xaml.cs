using Ifpa.ViewModels;
using PinballApi.Models.WPPR.v1.Statistics;


namespace Ifpa.Views
{
    [XamlCompilation(XamlCompilationOptions.Compile)]
    public partial class StatsPage : ContentPage
    {
        private StatsViewModel ViewModel;

        public StatsPage(StatsViewModel viewModel)
        {
            InitializeComponent();

            BindingContext = this.ViewModel = viewModel;
        }

        protected override async void OnNavigatedTo(NavigatedToEventArgs args)
        {
            base.OnNavigatedTo(args);

            ViewModel.LoadItemsCommand.Execute(null);
        }

        private async void PlayersListView_ItemSelected(object sender, SelectedItemChangedEventArgs e)
        {
            var player = e.SelectedItem as PointsThisYearStat;
            if (player == null)
                return;

            await Shell.Current.GoToAsync($"player-details?playerId={player.PlayerId}");

            // Manually deselect item.
            PlayersListView.SelectedItem = null;
        }

        private async void MostEventsListView_ItemSelected(object sender, SelectedItemChangedEventArgs e)
        {
            var player = e.SelectedItem as MostEventsStat;
            if (player == null)
                return;

            await Shell.Current.GoToAsync($"player-details?playerId={player.PlayerId}");

            // Manually deselect item.
            MostEventsListView.SelectedItem = null;
        }

        
        private async void BiggestMoversListView_ItemSelected(object sender, SelectedItemChangedEventArgs e)
        {
            var player = e.SelectedItem as BiggestMoversStat;
            if (player == null)
                return;

            await Shell.Current.GoToAsync($"player-details?playerId={player.PlayerId}");

            // Manually deselect item.
            BiggestMoversListView.SelectedItem = null;
        }
    }
}