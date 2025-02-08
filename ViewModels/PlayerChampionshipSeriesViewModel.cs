using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.Logging;
using PinballApi.Interfaces;
using PinballApi.Models.WPPR.Universal.Series;

namespace Ifpa.ViewModels
{
    public partial class PlayerChampionshipSeriesViewModel : BaseViewModel
    {
        [ObservableProperty]
        private List<SeriesRank> championshipSeries = new List<SeriesRank>();

        [ObservableProperty]
        private SeriesRank selectedChampionshipSeries;

        [ObservableProperty]
        private int year = DateTime.Now.Year;

        public int PlayerId { get; set; }

        private readonly IPinballRankingApi PinballRankingApi;

        public PlayerChampionshipSeriesViewModel(IPinballRankingApi pinballRankingApi, ILogger<PlayerChampionshipSeriesViewModel> logger) : base(logger)
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
                var player = await PinballRankingApi.GetPlayer(PlayerId);

                ChampionshipSeries = player.Series.Where(n => n.Year == Year).ToList();

                Title = $"{Strings.PlayerChampionshipSeriesPage_ChampionshipSeries} - {Year}";
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error loading championship series for player {0}", PlayerId);
            }
            finally
            {
                IsBusy = false;
            }
        }

        [RelayCommand]
        public async Task ViewChampSeriesDetails()
        {
            await Shell.Current.GoToAsync($"champ-series-detail?seriesCode={SelectedChampionshipSeries.SeriesCode}&regionCode={SelectedChampionshipSeries.RegionCode}&year={SelectedChampionshipSeries.Year}");
            SelectedChampionshipSeries = null;
        }

        [RelayCommand]
        public async Task FilterChampSeries()
        {
            var player = await PinballRankingApi.GetPlayer(PlayerId);

            var availableYears = player.Series.Select(n => n.Year).Distinct().ToList();

            string action = await Shell.Current.DisplayActionSheet(Strings.PlayerChampionshipSeriesPage_YearPrompt, Strings.Cancel, null, availableYears.Select(n => n.ToString()).ToArray());

            if (int.TryParse(action, out var yearValue))
            {
                Year = yearValue;
                await LoadItems();
            }
        }
    }
}
