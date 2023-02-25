using Ifpa.ViewModels;
using System;
using Microsoft.Maui;
using Ifpa.Models;

namespace Ifpa.Views
{
    [XamlCompilation(XamlCompilationOptions.Compile)]
    public partial class AboutPage : ContentPage
    {
        
        private readonly AboutViewModel ViewModel;

        public AboutPage(AboutViewModel viewModel)
        {
            InitializeComponent();

            BindingContext = this.ViewModel = viewModel;
        }

        protected override void OnAppearing()
        {
            base.OnAppearing();
            Task.Run(ViewModel.LoadSponsors);
        }

        private async void PlayerLabel_Tapped(object sender, TappedEventArgs e)
        {
            await Shell.Current.GoToAsync($"player-details?playerId={e.Parameter}");
        }

        private async void ReviewButton_Clicked(object sender, EventArgs e)
        {
            await ViewModel.OpenReview();
        }

        private async void LearnMore_Clicked(object sender, EventArgs e)
        {
            await Browser.OpenAsync("http://tiltforums.com/t/ifpa-app-now-available-on-the-app-store/4543", BrowserLaunchMode.External);
        }

        private async void Flagpedia_Tapped(object sender, EventArgs e)
        {
            await Browser.OpenAsync("https://flagpedia.net/", BrowserLaunchMode.External);
        }
    }
}