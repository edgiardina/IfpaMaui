using Ifpa.ViewModels;

namespace Ifpa.Views
{
    [QueryProperty("TournamentId", "tournamentId")]
    [XamlCompilation(XamlCompilationOptions.Compile)]
    public partial class RelatedTournamentsPage : ContentPage
    {
        RelatedTournamentsViewModel ViewModel;
        
        public int TournamentId { get; set; }

        public RelatedTournamentsPage(RelatedTournamentsViewModel viewModel)
        {
            InitializeComponent();

            BindingContext = this.ViewModel = viewModel;
        }

        protected override void OnAppearing()
        {
            base.OnAppearing();

            if (ViewModel.RelatedTournaments.Count == 0)
            {
                ViewModel.TournamentId = TournamentId;
                _ = ViewModel.LoadItems();
            }
        }
    }
}