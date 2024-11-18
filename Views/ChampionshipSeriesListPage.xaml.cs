using Ifpa.ViewModels;

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
            {
                ViewModel.LoadItems();
            }
        }
    }
}