using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.Logging;
using PinballApi.Interfaces;
using PinballApi.Models.WPPR.Universal.Series;

namespace Ifpa.ViewModels
{
    public partial class ChampionshipSeriesDetailViewModel : BaseViewModel
    {
        [ObservableProperty]
        private RegionStandings regionStandings;

        [ObservableProperty]
        private SeriesTournaments seriesTournaments;

        [ObservableProperty]
        private RegionStanding selectedRegionStandings;

        [ObservableProperty]
        private SubmittedTournament selectedTournament;

        private IPinballRankingApi PinballRankingApi { get; set; }

        public string RegionCode { get; set; }
        public string SeriesCode { get; set; }
        public int Year { get; set; }

        public ChampionshipSeriesDetailViewModel(IPinballRankingApi pinballRankingApi, ILogger<ChampionshipSeriesDetailViewModel> logger) : base(logger)
        {
            PinballRankingApi = pinballRankingApi;
        }

        [RelayCommand]
        public async Task LoadItems()
        {
            if (IsBusy)
                return;

            IsBusy = true;

            Title = $"{RegionCode} {SeriesCode} ({Year})";

            try
            {
                RegionStandings = await PinballRankingApi.GetSeriesStandingsForRegion(SeriesCode, RegionCode, Year);
                SeriesTournaments = await PinballRankingApi.GetSeriesTournamentsForRegion(SeriesCode, RegionCode, Year);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error loading championship series detail {0} {1} {2}", SeriesCode, RegionCode, Year);
            }
            finally
            {
                IsBusy = false;
            }
        }

        [RelayCommand]
        public async Task SelectRegionStandings()
        {
            await Shell.Current.GoToAsync($"champ-series-player?seriesCode={SeriesCode}&regionCode={RegionCode}&year={Year}&playerId={SelectedRegionStandings.PlayerId}");
            SelectedRegionStandings = null;
        }

        [RelayCommand]
        public async Task SelectTournament()
        {
            await Shell.Current.GoToAsync($"tournament-results?tournamentId={SelectedTournament.TournamentId}");
            SelectedTournament = null;
        }
    }
}
