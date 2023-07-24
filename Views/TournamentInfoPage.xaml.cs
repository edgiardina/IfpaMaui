using Ifpa.ViewModels;
using Microsoft.Maui;


namespace Ifpa.Views
{
    [XamlCompilation(XamlCompilationOptions.Compile)]
    public partial class TournamentInfoPage : ContentPage
    {

        TournamentResultsViewModel viewModel;
        public TournamentInfoPage(TournamentResultsViewModel viewModel)
        {
            InitializeComponent();

            BindingContext = this.viewModel = viewModel;
        }

        protected override async void OnNavigatedTo(NavigatedToEventArgs args)
        {
            base.OnNavigatedTo(args);

            if (viewModel.Results.Count == 0)
                viewModel.LoadItemsCommand.Execute(null);
        }

        private async void CloseButton_Clicked(object sender, EventArgs e)
        {
            //TODO: Remove all PopModals in favor of Shell.Current.GoToAsync("..");
            await Navigation.PopModalAsync();
        }
    }
}