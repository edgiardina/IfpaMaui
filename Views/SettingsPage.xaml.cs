using Ifpa.Models;
using Ifpa.ViewModels;
using Microsoft.Maui;


namespace Ifpa.Views
{
    [XamlCompilation(XamlCompilationOptions.Compile)]
	public partial class SettingsPage : ContentPage
	{
        SettingsViewModel ViewModel { get; set; }
		public SettingsPage (SettingsViewModel viewModel)
		{
			InitializeComponent ();
            this.BindingContext = ViewModel = viewModel;
		}

        protected override async void OnAppearing()
        {
            base.OnAppearing();
            await ViewModel.LoadPlayer();
        }

        private async void Button_Clicked(object sender, System.EventArgs e)
        {
            var shouldClear = await DisplayAlert("Clear Selected Player", "Your 'My Stats' player selection will be cleared, are you sure you wish to do this?", "OK", "Cancel");
            if (shouldClear)
            {
                await Settings.SetMyStatsPlayer(0, 0);
                await DisplayAlert("Selected Player Cleared", "Your 'My Stats' player selection has been cleared", "OK");
            }
        }
    }
}