using Ifpa.Platforms.Extensions;
using Ifpa.ViewModels;
using PinballApi.Models.WPPR.v2.Tournaments;

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
                ViewModel.LoadItemsCommand.Execute(null);
            }
        }

        private async void ShareButton_Clicked(object sender, EventArgs e)
        {
            await Share.RequestAsync(new ShareTextRequest
            {
                Uri = $"https://www.ifpapinball.com/tournaments/view.php?t={ViewModel.TournamentId}",
                Title = "Share Tournament Results"
            });
        }

        private async void InfoButton_Clicked(object sender, EventArgs e)
        {
            //Originally this was a seperate modal page. now its a bottom sheet. 
            var infoPage = new TournamentInfoPage(ViewModel);

            //await Navigation.PushModalAsync(infoPage);

            infoPage.ForceLayout();

            var view = (View)BottomSheetTemplate.CreateContent();
            view.BindingContext = BindingContext;
            

            this.ShowBottomSheet(view, true);

        }

        private async void MyListView_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            var tournament = e.CurrentSelection.FirstOrDefault() as TournamentResult;
            if (tournament == null)
                return;

            if (tournament.PlayerId.HasValue)
            {
                await Shell.Current.GoToAsync($"player-details?playerId={tournament.PlayerId.Value}");
            }

            //Deselect Item
            ((CollectionView)sender).SelectedItem = null;
        }
    }
}
