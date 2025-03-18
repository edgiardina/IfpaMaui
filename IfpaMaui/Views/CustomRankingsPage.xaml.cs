using Ifpa.ViewModels;
using PinballApi.Models.WPPR.v2.Rankings;

namespace Ifpa.Views
{
    [XamlCompilation(XamlCompilationOptions.Compile)]
    public partial class CustomRankingsPage : ContentPage
    {
        CustomRankingsViewModel ViewModel;

        public CustomRankingsPage(CustomRankingsViewModel viewModel)
        {
            InitializeComponent();

            BindingContext = ViewModel = viewModel;
        }

        protected override void OnAppearing()
        {
            if(ViewModel.CustomRankings.Count == 0)
                ViewModel.LoadItemsCommand.Execute(null);

            base.OnAppearing();
        }
    }
}