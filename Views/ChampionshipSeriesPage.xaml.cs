using Ifpa.ViewModels;

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

        protected override void OnAppearing()
        {
            base.OnAppearing();

            if (ViewModel.SeriesOverallResults.Count == 0)
            {
                ViewModel.Year = Year;
                ViewModel.SeriesCode = SeriesCode;
                ViewModel.LoadItems();
            }
        }

        private async void ToolbarItem_Clicked(object sender, EventArgs e)
        {
            string action = await DisplayActionSheet("Championship Series Year", "Cancel", null, ViewModel.AvailableYears.Select(n => n.ToString()).ToArray());

            if (int.TryParse(action, out var yearValue))
            {
                this.Year = yearValue;
                ViewModel.Year = yearValue;
                ViewModel.LoadItems();
            }
        }
    }
}
