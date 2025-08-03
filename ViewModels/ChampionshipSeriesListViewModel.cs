using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.Logging;
using PinballApi.Interfaces;
using PinballApi.Models.WPPR.Universal.Series;

namespace Ifpa.ViewModels
{
    public partial class ChampionshipSeriesListViewModel : BaseViewModel
    {
        [ObservableProperty]
        private List<Series> championshipSeries = new List<Series>();

        [ObservableProperty]
        private Series selectedChampionshipSeries;

        private readonly IPinballRankingApi PinballRankingApi;

        public ChampionshipSeriesListViewModel(IPinballRankingApi pinballRankingApi, ILogger<ChampionshipSeriesListViewModel> logger) : base(logger)
        {
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
                ChampionshipSeries = await PinballRankingApi.GetSeries();
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
            SelectedChampionshipSeries = null;
        }
    }
}
