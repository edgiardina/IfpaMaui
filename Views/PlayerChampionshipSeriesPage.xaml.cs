using Ifpa.ViewModels;
using PinballApi.Models.v2.WPPR;
using System.Collections.ObjectModel;

namespace Ifpa.Views
{
    [QueryProperty("PlayerId", "playerId")]
    [XamlCompilation(XamlCompilationOptions.Compile)]
    public partial class PlayerChampionshipSeriesPage : ContentPage
    {
        PlayerChampionshipSeriesViewModel viewModel;
        public int PlayerId { get; set; }

        public ObservableCollection<string> Items { get; set; }

        public PlayerChampionshipSeriesPage(PlayerChampionshipSeriesViewModel viewModel)
        {
            InitializeComponent();

            BindingContext = this.viewModel = viewModel;
        }

        protected override async void OnNavigatedTo(NavigatedToEventArgs args)
        {
            base.OnNavigatedTo(args);

            viewModel.PlayerId = PlayerId;

            if (viewModel.ChampionshipSeries.Count == 0)
                viewModel.LoadItemsCommand.Execute(null);
        }

        private async void MyListView_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            var state = e.CurrentSelection.FirstOrDefault() as ChampionshipSeries;
            if (state == null)
                return;

            await Shell.Current.GoToAsync($"champ-series-detail?seriesCode={state.SeriesCode}&regionCode={state.RegionCode}&year={state.Year}");

            //Deselect Item
            ((CollectionView)sender).SelectedItem = null;
        }
    }
}
