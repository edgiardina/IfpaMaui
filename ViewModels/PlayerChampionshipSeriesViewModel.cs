using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.Logging;
using PinballApi;
using PinballApi.Models.v2.WPPR;

namespace Ifpa.ViewModels
{
    public partial class PlayerChampionshipSeriesViewModel : BaseViewModel
    {
        [ObservableProperty]
        private List<ChampionshipSeries> championshipSeries = new List<ChampionshipSeries>();

        [ObservableProperty]
        private ChampionshipSeries selectedChampionshipSeries;

        [ObservableProperty]
        private int year = DateTime.Now.Year;

        public int PlayerId { get; set; }

        public PlayerChampionshipSeriesViewModel(PinballRankingApiV2 pinballRankingApiV2, ILogger<PlayerChampionshipSeriesViewModel> logger) : base(pinballRankingApiV2, logger)
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
                var player = await PinballRankingApiV2.GetPlayer(PlayerId);

                ChampionshipSeries = player.ChampionshipSeries.Where(n => n.Year == Year).ToList();
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
            var player = await PinballRankingApiV2.GetPlayer(PlayerId);

            var availableYears = player.ChampionshipSeries.Select(n => n.Year).Distinct().ToList();

            string action = await Shell.Current.DisplayActionSheet(Strings.PlayerChampionshipSeriesPage_YearPrompt, Strings.Cancel, null, availableYears.Select(n => n.ToString()).ToArray());

            if (int.TryParse(action, out var yearValue))
            {
                Year = yearValue;
                await LoadItems();
            }
        }
    }
}
