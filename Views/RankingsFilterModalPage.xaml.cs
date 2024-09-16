using Ifpa.ViewModels;
using PinballApi.Models.WPPR;
using PinballApi.Models.WPPR.Universal;
using PinballApi.Models.WPPR.Universal.Rankings;

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
            viewModel.CurrentRankingSystem = (RankingSystem)Enum.Parse(typeof(RankingSystem), Preferences.Get("RankingSystem", viewModel.CurrentRankingSystem.ToString()));
            viewModel.CurrentProRankingType = (TournamentType)Enum.Parse(typeof(TournamentType), Preferences.Get("ProRankingType", viewModel.CurrentProRankingType.ToString()));

            RankingTypePicker.SelectedItem = viewModel.CurrentRankingType.ToString();
            TypePicker.SelectedItem = viewModel.CurrentRankingSystem.ToString();
            ProTypeFilter.SelectedItem = viewModel.CurrentProRankingType.ToString();
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
            }
        }

        private void Stepper_ValueChanged(object sender, ValueChangedEventArgs e)
        {
            if (viewModel.Players.Count > 0)
            {
                Preferences.Set("PlayerCount", viewModel.CountOfItemsToFetch);
                Preferences.Set("StartingRank", viewModel.StartingPosition);
            }
        }

        private void RankingType_SelectedIndexChanged(object sender, System.EventArgs e)
        {
            var selectedType =
                ((Picker)sender).SelectedItem is string ?
                (RankingType)Enum.Parse(typeof(RankingType), ((Picker)sender).SelectedItem as string) :
                (RankingType)((Picker)sender).SelectedItem;

            if (selectedType == RankingType.Country)
            {
                CountryPicker.IsVisible = true;
                CountryLabel.IsVisible = true;
                TypeLabel.IsVisible = false;
                TypePicker.IsVisible = false;
                ProTypeFilter.IsVisible = false;
                ProTypeLabel.IsVisible = false;
            }
            else if (selectedType == RankingType.Women)
            {
                CountryPicker.IsVisible = false;
                CountryLabel.IsVisible = false;
                TypeLabel.IsVisible = true;
                TypePicker.IsVisible = true;
                ProTypeFilter.IsVisible = false;
                ProTypeLabel.IsVisible = false;
            }
            else if (selectedType == RankingType.Pro)
            {
                CountryPicker.IsVisible = false;
                CountryLabel.IsVisible = false;
                TypeLabel.IsVisible = false;
                TypePicker.IsVisible = false;
                ProTypeFilter.IsVisible = true;
                ProTypeLabel.IsVisible = true;
            }
            else
            {
                CountryPicker.IsVisible = false;
                CountryLabel.IsVisible = false;
                TypeLabel.IsVisible = false;
                TypePicker.IsVisible = false;

                ProTypeFilter.IsVisible = false;
                ProTypeLabel.IsVisible = false;
                viewModel.CountryToShow = viewModel.DefaultCountry;
            }

            viewModel.CurrentRankingType = selectedType;

            Preferences.Set("RankingType", viewModel.CurrentRankingType.ToString());
        }

        private void TypePicker_SelectedIndexChanged(object sender, EventArgs e)
        {
            viewModel.CurrentRankingSystem = (RankingSystem)Enum.Parse(typeof(RankingSystem), ((Picker)sender).SelectedItem.ToString());

            Preferences.Set("RankingSystem", viewModel.CurrentRankingSystem.ToString());
        }

        private async void Button_Clicked(object sender, EventArgs e)
        {
            await Task.Run(() => viewModel.LoadItemsCommand.Execute(null));

            await Navigation.PopModalAsync();
            FilterSaved?.Invoke();
        }

        private void ProTypeFilter_SelectedIndexChanged(object sender, EventArgs e)
        {
            viewModel.CurrentProRankingType = (TournamentType)Enum.Parse(typeof(TournamentType), ((Picker)sender).SelectedItem.ToString());

            Preferences.Set("ProRankingType", viewModel.CurrentProRankingType.ToString());
        }
    }
}