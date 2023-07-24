using Ifpa.ViewModels;
using PinballApi.Models.WPPR.v2.Nacs;
using PinballApi.Models.WPPR.v2.Series;
using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Maui;


namespace Ifpa.Views
{
    [QueryProperty("SeriesCode", "seriesCode")]
    [QueryProperty("Year", "year")]
    [XamlCompilation(XamlCompilationOptions.Compile)]
    public partial class ChampionshipSeriesPage : ContentPage
    {
        ChampionshipSeriesViewModel ViewModel;
        public int Year { get; set; } = DateTime.Now.Year;
        public string SeriesCode { get; set; }

        public ChampionshipSeriesPage(ChampionshipSeriesViewModel viewModel)
        {
            InitializeComponent();

            //TODO: allow user to pick the year            

            BindingContext = this.ViewModel = viewModel;
        }

        protected override async void OnNavigatedTo(NavigatedToEventArgs args)
        {
            base.OnNavigatedTo(args);

            if (ViewModel.SeriesOverallResults.Count == 0)
            {
                ViewModel.Year = Year;
                ViewModel.SeriesCode = SeriesCode;
                ViewModel.LoadItemsCommand.Execute(null);
            }
        }

        private async void ToolbarItem_Clicked(object sender, EventArgs e)
        {
            string action = await DisplayActionSheet("Championship Series Year", "Cancel", null, ViewModel.AvailableYears.Select(n => n.ToString()).ToArray());

            if (int.TryParse(action, out var yearValue))
            {
                this.Year = yearValue;
                ViewModel.Year = yearValue;
                ViewModel.LoadItemsCommand.Execute(null);
            }
        }

        private async void MyListView_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            var championshipStandings = e.CurrentSelection.FirstOrDefault() as SeriesOverallResult;
            if (championshipStandings == null)
                return;

            await Shell.Current.GoToAsync($"champ-series-detail?seriesCode={ViewModel.SeriesCode}&regionCode={championshipStandings.RegionCode}&year={Year}");

            //Deselect Item
            ((CollectionView)sender).SelectedItem = null;
        }
    }
}
