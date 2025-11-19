using Ifpa.Models;
using Ifpa.ViewModels; 

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
            ViewModel.UpdateCacheSize();
        }

        // TODO: this should live in the viewmodel and we should pass a IAlertService to the viewmodel
        private async void Button_Clicked(object sender, System.EventArgs e)
        {
            var shouldClear = await DisplayAlertAsync("Clear Selected Player", "Your 'My Stats' player selection will be cleared, are you sure you wish to do this?", Strings.OK, Strings.Cancel);
            if (shouldClear)
            {
                await Settings.SetMyStatsPlayer(0, 0);
                await DisplayAlertAsync("Selected Player Cleared", "Your 'My Stats' player selection has been cleared", Strings.OK);
                await ViewModel.LoadPlayer();
            }
        }
    }
}