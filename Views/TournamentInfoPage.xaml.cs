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

        protected override void OnAppearing()
        {
            base.OnAppearing();

            if (viewModel.Results.Count == 0)
                viewModel.LoadItemsCommand.Execute(null);
        }
    }
}