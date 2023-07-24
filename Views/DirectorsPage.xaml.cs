using Microsoft.Maui;

using Ifpa.ViewModels;
using PinballApi.Models.WPPR.v2.Directors;

namespace Ifpa.Views
{
    [XamlCompilation(XamlCompilationOptions.Compile)]
    public partial class DirectorsPage : ContentPage
    {
        DirectorsViewModel ViewModel;

        public DirectorsPage(DirectorsViewModel viewModel)
        {
            InitializeComponent();

            BindingContext = ViewModel = viewModel;
        }

        async void OnItemSelected(object sender, SelectedItemChangedEventArgs args)
        {
            var director = args.SelectedItem as Director;
            if (director == null)
                return;

            await Shell.Current.GoToAsync($"player-details?playerId={director.PlayerId}");

            // Manually deselect item.
            ((ListView)sender).SelectedItem = null;
        }

        protected override async void OnNavigatedTo(NavigatedToEventArgs args)
        {
            base.OnNavigatedTo(args);

            ViewModel.LoadItemsCommand.Execute(null);            
        }
    }
}