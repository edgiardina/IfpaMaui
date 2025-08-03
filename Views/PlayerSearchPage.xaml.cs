using Ifpa.ViewModels;
using PinballApi.Models.WPPR.v2.Players;

namespace Ifpa.Views
{
    [XamlCompilation(XamlCompilationOptions.Compile)]
    public partial class PlayerSearchPage : ContentPage
    {
        PlayerSearchViewModel ViewModel;

        public PlayerSearchPage(PlayerSearchViewModel viewModel)
        {
            InitializeComponent();

            BindingContext = ViewModel = viewModel;
        }

        protected override void OnAppearing()
        {
            base.OnAppearing();
        }
    }
}