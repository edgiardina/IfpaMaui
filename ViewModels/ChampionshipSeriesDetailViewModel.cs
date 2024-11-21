using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.Logging;
using PinballApi;
using PinballApi.Models.WPPR.v2.Series;

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

        public string RegionCode { get; set; }
        public string SeriesCode { get; set; }
        public int Year { get; set; }

        public ChampionshipSeriesDetailViewModel(PinballRankingApiV2 pinballRankingApiV2, ILogger<ChampionshipSeriesDetailViewModel> logger) : base(pinballRankingApiV2, logger)
        {
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
                RegionStandings = await PinballRankingApiV2.GetSeriesStandingsForRegion(SeriesCode, RegionCode, Year);
                SeriesTournaments = await PinballRankingApiV2.GetSeriesTournamentsForRegion(SeriesCode, RegionCode, Year);
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
