using Ifpa.ViewModels;
using System;
using Microsoft.Maui;
using Ifpa.Models;

namespace Ifpa.Views
{
    [XamlCompilation(XamlCompilationOptions.Compile)]
    public partial class AboutPage : ContentPage
    {
        private const int creatorIfpaNumber = 16927;
        private readonly AboutViewModel ViewModel;

        public AboutPage(AboutViewModel viewModel)
        {
            InitializeComponent();

            BindingContext = this.ViewModel = viewModel;
        }

        private async void CreatorLabel_Tapped(object sender, EventArgs e)
        {
            await Shell.Current.GoToAsync($"player-details?playerId={creatorIfpaNumber}");
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