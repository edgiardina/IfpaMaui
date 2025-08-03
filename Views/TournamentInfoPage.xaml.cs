using Ifpa.ViewModels;

namespace Ifpa.Views
{
    [QueryProperty("TournamentId", "tournamentId")]
    [XamlCompilation(XamlCompilationOptions.Compile)]
    public partial class TournamentInfoPage : ContentPage
    {
        public int TournamentId { get; set; }

        TournamentResultsViewModel viewModel;
        public TournamentInfoPage(TournamentResultsViewModel viewModel)
        {
            InitializeComponent();

            BindingContext = this.viewModel = viewModel;
        }

        protected override void OnAppearing()
        {
            base.OnAppearing();

            viewModel.TournamentId = TournamentId;
            viewModel.LoadItems();
        }

        private async void CloseButton_Clicked(object sender, System.EventArgs e)
        {
            await Shell.Current.GoToAsync("..");
        }
    }
}