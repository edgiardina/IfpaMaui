using Ifpa.ViewModels;
using PinballApi.Models.WPPR.v2;
using PinballApi.Models.WPPR.v2.Rankings;
using System;
using Microsoft.Maui;


namespace Ifpa.Views
{
    [XamlCompilation(XamlCompilationOptions.Compile)]
    public partial class RankingsFilterModalPage : ContentPage
    {
        RankingsViewModel viewModel;
        public delegate void FilterSavedDelegate();
        public event FilterSavedDelegate FilterSaved;

        public RankingsFilterModalPage(RankingsViewModel viewModel)
        {
            InitializeComponent();

            //have to set selected item when picker is visible.
            CountryPicker.SelectedItem = Preferences.Get("CountryName", viewModel.DefaultCountry.CountryName);

            CountryPicker.IsVisible = false;
            CountryLabel.IsVisible = false;
            TypeLabel.IsVisible = false;
            TypePicker.IsVisible = false;

            BindingContext = this.viewModel = viewModel;

            viewModel.CountOfItemsToFetch = Preferences.Get("PlayerCount", viewModel.CountOfItemsToFetch);
            viewModel.StartingPosition = Preferences.Get("StartingRank", viewModel.StartingPosition);
            viewModel.CurrentRankingType = (RankingType)Enum.Parse(typeof(RankingType), Preferences.Get("RankingType", viewModel.CurrentRankingType.ToString()));
            viewModel.CurrentTournamentType = (TournamentType)Enum.Parse(typeof(TournamentType), Preferences.Get("TournamentType", viewModel.CurrentTournamentType.ToString()));

         
            RankingTypePicker.SelectedItem = viewModel.CurrentRankingType.ToString();
            TypePicker.SelectedItem = viewModel.CurrentTournamentType.ToString();
        }

        private async void CancelButton_Clicked(object sender, EventArgs e)
        {
            await Shell.Current.GoToAsync("..");
        }

        private void Picker_SelectedIndexChanged(object sender, System.EventArgs e)
        {
            //don't fire selectedIndexChanged when we are adding countries first time in
            if (viewModel.Countries.Count > 0 && !viewModel.IsBusy)
            {
                Preferences.Set("CountryName", viewModel.CountryToShow.CountryName);
                viewModel.LoadItemsCommand.Execute(null);
            }
        }

        private void Stepper_ValueChanged(object sender, ValueChangedEventArgs e)
        {
            if (viewModel.Players.Count > 0)
            {
                Preferences.Set("PlayerCount", viewModel.CountOfItemsToFetch);
                Preferences.Set("StartingRank", viewModel.StartingPosition);
                viewModel.LoadItemsCommand.Execute(null);
            }
        }

        private void RankingType_SelectedIndexChanged(object sender, System.EventArgs e)
        {
            var selectedType = (RankingType)Enum.Parse(typeof(RankingType), ((Picker)sender).SelectedItem as string);

            if (selectedType == RankingType.Country)
            {
                CountryPicker.IsVisible = true;
                CountryLabel.IsVisible = true;
                TypeLabel.IsVisible = false;
                TypePicker.IsVisible = false;
            }
            else if (selectedType == RankingType.Women)
            {
                CountryPicker.IsVisible = false;
                CountryLabel.IsVisible = false;
                TypeLabel.IsVisible = true;
                TypePicker.IsVisible = true;
            }
            else
            {
                CountryPicker.IsVisible = false;
                CountryLabel.IsVisible = false;
                TypeLabel.IsVisible = false;
                TypePicker.IsVisible = false;
                viewModel.CountryToShow = viewModel.DefaultCountry;
            }

            viewModel.CurrentRankingType = selectedType;

            Preferences.Set("RankingType", viewModel.CurrentRankingType.ToString());

            viewModel.LoadItemsCommand.Execute(null);
        }

        private void TypePicker_SelectedIndexChanged(object sender, EventArgs e)
        {
            viewModel.CurrentTournamentType = (TournamentType)Enum.Parse(typeof(TournamentType), ((Picker)sender).SelectedItem as string);

            Preferences.Set("TournamentType", viewModel.CurrentTournamentType.ToString());
            viewModel.LoadItemsCommand.Execute(null);

        }

        private async void Button_Clicked(object sender, EventArgs e)
        {
            await Navigation.PopModalAsync();
            FilterSaved?.Invoke();
        }
    }
}