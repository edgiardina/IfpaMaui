using Ifpa.ViewModels;

namespace Ifpa.Views
{
    [QueryProperty("TournamentId", "tournamentId")]
    [XamlCompilation(XamlCompilationOptions.Compile)]
    public partial class TournamentResultsPage : ContentPage
    {
        TournamentResultsViewModel ViewModel;
        
        public int TournamentId { get; set; }

        public TournamentResultsPage(TournamentResultsViewModel viewModel)
        {
            InitializeComponent();

            BindingContext = this.ViewModel = viewModel;
        }

        protected override void OnAppearing()
        {
            base.OnAppearing();

            if (ViewModel.Results.Count == 0)
            {
                ViewModel.TournamentId = TournamentId;
                ViewModel.LoadItems();
            }
        }
    }
}
