using Ifpa.ViewModels;

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
                _ = ViewModel.LoadItems();
            }
        }

    }
}
