using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.Logging;
using PinballApi;
using PinballApi.Models.WPPR.v2.Directors;

namespace Ifpa.ViewModels
{
    public partial class DirectorsViewModel : BaseViewModel
    {
        [ObservableProperty]
        private List<Director> nacsDirectors = new List<Director>();

        [ObservableProperty]
        private List<Director> countryDirectors = new List<Director>();

        [ObservableProperty]
        private Director selectedDirector;

        public DirectorsViewModel(PinballRankingApiV2 pinballRankingApiV2, ILogger<DirectorsViewModel> logger) : base(pinballRankingApiV2, logger)
        {
            Title = "Directors";
        }

        [RelayCommand]
        public async Task LoadItems()
        {
            if (IsBusy)
                return;

            IsBusy = true;

            try
            {
                NacsDirectors = await PinballRankingApiV2.GetNacsDirectors();
                CountryDirectors = await PinballRankingApiV2.GetCountryDirectors();
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error loading directors");
            }
            finally
            {
                IsBusy = false;
            }
        }

        [RelayCommand]
        public async Task ViewDirectorDetail()
        {
            await Shell.Current.GoToAsync($"player-details?playerId={SelectedDirector.PlayerId}");
        }
    }
}