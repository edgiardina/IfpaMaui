using PinballApi.Models.WPPR.v2.Nacs;
using PinballApi.Models.WPPR.v2.Series;
using System;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Threading.Tasks;
using Microsoft.Maui;
using Microsoft.Extensions.Configuration;
using PinballApi;

namespace Ifpa.ViewModels
{
    public class ChampionshipSeriesPlayerCardViewModel : BaseViewModel
    {
        public ObservableCollection<PlayerCard> TournamentCardRecords { get; set; }
        public Command LoadItemsCommand { get; set; }
        public int Year { get; set; }
        public int PlayerId { get; set; }
        public string RegionCode { get; set; }
        public string SeriesCode { get; set; }

        public ChampionshipSeriesPlayerCardViewModel(PinballRankingApiV1 pinballRankingApiV1, PinballRankingApiV2 pinballRankingApiV2) : base(pinballRankingApiV1, pinballRankingApiV2)
        {
            TournamentCardRecords = new ObservableCollection<PlayerCard>();

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
                var tournamentCard = await PinballRankingApiV2.GetSeriesPlayerCard(PlayerId, SeriesCode, RegionCode, Year);

                foreach (var item in tournamentCard.PlayerCard)
                {
                    TournamentCardRecords.Add(item);
                }

                Title = $"{RegionCode} {SeriesCode} ({Year}) - {tournamentCard.PlayerName}";
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex);
            }
            finally
            {
                IsBusy = false;
            }
        }
    }
}
