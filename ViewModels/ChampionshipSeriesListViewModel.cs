using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.Logging;
using PinballApi;
using PinballApi.Models.WPPR.v2.Series;

namespace Ifpa.ViewModels
{
    public partial class ChampionshipSeriesListViewModel : BaseViewModel
    {
        [ObservableProperty]
        private List<Series> championshipSeries = new List<Series>();

        [ObservableProperty]
        private Series selectedChampionshipSeries;

        public ChampionshipSeriesListViewModel(PinballRankingApiV2 pinballRankingApiV2, ILogger<ChampionshipSeriesListViewModel> logger) : base(pinballRankingApiV2, logger)
        {
        }

        [RelayCommand]
        public async Task LoadItems()
        {
            if (IsBusy)
                return;

            IsBusy = true;

            try
            {
                ChampionshipSeries = await PinballRankingApiV2.GetSeries();
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error loading championship series");
            }
            finally
            {
                IsBusy = false;
            }
        }

        [RelayCommand]
        public async Task ViewChampionshipSeriesDetail(Series series)
        {
            await Shell.Current.GoToAsync($"champ-series?seriesCode={SelectedChampionshipSeries.Code}&year={SelectedChampionshipSeries.Years.Max()}");
        }
    }
}
