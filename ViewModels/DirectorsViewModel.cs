using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.Logging;
using PinballApi;
using PinballApi.Interfaces;
using PinballApi.Models.WPPR.Universal.Directors;

namespace Ifpa.ViewModels
{
    public partial class DirectorsViewModel : BaseViewModel
    {
        //[ObservableProperty]
        //private List<Director> nacsDirectors = new List<Director>();

        [ObservableProperty]
        private List<CountryDirector> countryDirectors = new List<CountryDirector>();

        [ObservableProperty]
        private CountryDirector selectedDirector;

        private readonly IPinballRankingApi PinballRankingApi;
        
        public DirectorsViewModel(IPinballRankingApi pinballRankingApi, ILogger<DirectorsViewModel> logger) : base(logger)
        {
            Title = "Directors";
            PinballRankingApi = pinballRankingApi;
        }

        [RelayCommand]
        public async Task LoadItems()
        {
            if (IsBusy)
                return;

            IsBusy = true;

            try
            {
                //NacsDirectors = await PinballRankingApi.getre;
                CountryDirectors = await PinballRankingApi.GetCountryDirectors();
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
            await Shell.Current.GoToAsync($"player-details?playerId={SelectedDirector.PlayerProfile.PlayerId}");
        }
    }
}