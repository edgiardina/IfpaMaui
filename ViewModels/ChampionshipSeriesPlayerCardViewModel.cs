using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.Logging;
using PinballApi;
using PinballApi.Models.WPPR.Universal.Series;

namespace Ifpa.ViewModels
{
    public partial class ChampionshipSeriesPlayerCardViewModel : BaseViewModel
    {
        [ObservableProperty]
        private List<PlayerCard> tournamentCardRecords = new List<PlayerCard>();

        [ObservableProperty]
        private PlayerCard selectedTournamentCard;

        public int Year { get; set; }
        public int PlayerId { get; set; }
        public string RegionCode { get; set; }
        public string SeriesCode { get; set; }

        public PinballRankingApi PinballRankingApi { get; set; }

        public ChampionshipSeriesPlayerCardViewModel(PinballRankingApiV2 pinballRankingApiV2, PinballRankingApi pinballRankingApi, ILogger<ChampionshipSeriesPlayerCardViewModel> logger) : base(pinballRankingApiV2, logger)
        {
            PinballRankingApi = pinballRankingApi;
        }

        [RelayCommand]
        public async Task LoadItems()
        {
            Title = $"{RegionCode} Championship Series";

            if (IsBusy)
                return;

            IsBusy = true;

            try
            {
                var tournamentCard = await PinballRankingApi.GetSeriesPlayerCard(PlayerId, SeriesCode, RegionCode, Year);

                TournamentCardRecords = tournamentCard.PlayerCard;

                Title = $"{RegionCode} {SeriesCode} ({Year}) - {tournamentCard.PlayerName}";
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error loading championship series player card {0} {1} {2} {3}", PlayerId, SeriesCode, RegionCode, Year);
            }
            finally
            {
                IsBusy = false;
            }
        }

        [RelayCommand]
        public async Task SelectPlayerCard()
        {
            await Shell.Current.GoToAsync($"tournament-results?tournamentId={SelectedTournamentCard.TournamentId}");
            SelectedTournamentCard = null;
        }
    }
}
