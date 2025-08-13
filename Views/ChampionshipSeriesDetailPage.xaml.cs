using Ifpa.ViewModels;

namespace Ifpa.Views
{
    [QueryProperty("SeriesCode", "seriesCode")]
    [QueryProperty("RegionCode", "regionCode")]
    [QueryProperty("Year", "year")]
    [XamlCompilation(XamlCompilationOptions.Compile)]
    public partial class ChampionshipSeriesDetailPage : ContentPage
    {
        ChampionshipSeriesDetailViewModel ViewModel;

        public string SeriesCode { get; set; }
        public string RegionCode { get; set; }  
        public int Year { get; set; }

        public ChampionshipSeriesDetailPage(ChampionshipSeriesDetailViewModel viewModel)
        {
            InitializeComponent();

            BindingContext = this.ViewModel = viewModel;
        }

        protected override void OnAppearing()
        {
            base.OnAppearing();

            ViewModel.Year = Year;
            ViewModel.RegionCode = RegionCode;
            ViewModel.SeriesCode = SeriesCode;

            _ = ViewModel.LoadItems();
        }
    }
}
