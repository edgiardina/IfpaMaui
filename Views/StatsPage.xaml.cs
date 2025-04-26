using Ifpa.ViewModels;


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

        protected override async void OnAppearing()
        {
            base.OnAppearing();

            await ViewModel.LoadItems();
        }
    }
}