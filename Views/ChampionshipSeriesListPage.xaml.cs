using Ifpa.ViewModels;
using PinballApi.Models.WPPR.v2.Series;
using System.Linq;
using Microsoft.Maui;


namespace Ifpa.Views
{
    [XamlCompilation(XamlCompilationOptions.Compile)]
    public partial class ChampionshipSeriesListPage : ContentPage
    {
        ChampionshipSeriesListViewModel ViewModel;

        public ChampionshipSeriesListPage(ChampionshipSeriesListViewModel viewModel)
        {
            InitializeComponent();

            BindingContext = this.ViewModel = viewModel;
        }

        protected override void OnAppearing()
        {
            base.OnAppearing();

            if (ViewModel.ChampionshipSeries.Count == 0)
                ViewModel.LoadItemsCommand.Execute(null);
        }

        private async void ChampionshipSeriesCollectionView_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (e.CurrentSelection.FirstOrDefault() != null)
            {
                var championshipSeries = e.CurrentSelection.FirstOrDefault() as Series;

                await Shell.Current.GoToAsync($"champ-series?seriesCode={championshipSeries.Code}&year={championshipSeries.Years.Max()}");

                //Deselect Item
                ((CollectionView)sender).SelectedItem = null;
            }
        }

    }
}