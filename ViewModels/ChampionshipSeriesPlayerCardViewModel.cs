using PinballApi.Models.WPPR.Universal.Series;
using System.Collections.ObjectModel;
using PinballApi;
using Microsoft.Extensions.Logging;
using CommunityToolkit.Mvvm.ComponentModel;

namespace Ifpa.ViewModels
{
    public partial class ChampionshipSeriesPlayerCardViewModel : BaseViewModel
    {
        [ObservableProperty]
        private List<PlayerCard> tournamentCardRecords = new List<PlayerCard>();
        public Command LoadItemsCommand { get; set; }
        public int Year { get; set; }
        public int PlayerId { get; set; }
        public string RegionCode { get; set; }
        public string SeriesCode { get; set; }

        public PinballRankingApi PinballRankingApi { get; set; }

        public ChampionshipSeriesPlayerCardViewModel(PinballRankingApiV2 pinballRankingApiV2, PinballRankingApi pinballRankingApi, ILogger<ChampionshipSeriesPlayerCardViewModel> logger) : base(pinballRankingApiV2, logger)
        {
            PinballRankingApi = pinballRankingApi;

            LoadItemsCommand = new Command(async () => await ExecuteLoadItemsCommand());
        }


        async Task ExecuteLoadItemsCommand()
        {
            Title = $"{RegionCode} Championship Series";

            if (IsBusy)
                return;

            IsBusy = true;

            try
            {
                TournamentCardRecords.Clear();
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
    }
}
