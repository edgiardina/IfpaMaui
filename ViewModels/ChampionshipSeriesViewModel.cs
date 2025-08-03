using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.Logging;
using PinballApi.Interfaces;
using PinballApi.Models.WPPR.Universal.Series;

namespace Ifpa.ViewModels
{
    public partial class ChampionshipSeriesViewModel : BaseViewModel
    {
        [ObservableProperty]
        private List<SeriesOverallResult> seriesOverallResults = new List<SeriesOverallResult>();

        [ObservableProperty]
        private List<int> availableYears = new List<int>();

        [ObservableProperty]
        private SeriesOverallResult selectedSeriesOverallResult;

        private readonly IPinballRankingApi PinballRankingApi;

        public int Year { get; set; }

        public string SeriesCode { get; set; }

        public ChampionshipSeriesViewModel(IPinballRankingApi pinballRankingApi, ILogger<ChampionshipSeriesViewModel> logger) : base(logger)
        {
            this.PinballRankingApi = pinballRankingApi;
        }

        [RelayCommand]
        public async Task LoadItems()
        {
            if (IsBusy)
                return;

            Title = SeriesCode;

            IsBusy = true;

            try
            {
                SeriesOverallResults.Clear();
                var stateProvinceChampionshipSeries = await PinballRankingApi.GetSeriesOverallStanding(SeriesCode, Year);
                var seriesDetails = await PinballRankingApi.GetSeries();

                AvailableYears = seriesDetails.First(n => n.Code == SeriesCode).Years;

                if (stateProvinceChampionshipSeries != null)
                {
                    SeriesOverallResults = stateProvinceChampionshipSeries.OverallResults;
                }

                if (Year != DateTime.Now.Year)
                {
                    Title = $"{SeriesCode} {Year}";
                }
                else
                {
                    Title = SeriesCode;
                }
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error loading championship series {0} {1}", SeriesCode, Year);
            }
            finally
            {
                IsBusy = false;
            }
        }

        [RelayCommand]
        public async Task SelectChampSeriesDetail()
        {
            await Shell.Current.GoToAsync($"champ-series-detail?seriesCode={SeriesCode}&regionCode={SelectedSeriesOverallResult.RegionCode}&year={Year}");
            SelectedSeriesOverallResult = null;
        }
    }
}
