using PinballApi.Models.v2.WPPR;
using PinballApi.Models.WPPR.v2.Nacs;
using PinballApi.Models.WPPR.v2.Series;
using System;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Threading.Tasks;
using Microsoft.Maui;
using Microsoft.Extensions.Configuration;
using PinballApi;
using Microsoft.Extensions.Logging;

namespace Ifpa.ViewModels
{
    public class ChampionshipSeriesDetailViewModel : BaseViewModel
    {
        public RegionStandings RegionStandings { get; set; }
        public SeriesTournaments SeriesTournaments { get; set; }    

        public Command LoadItemsCommand { get; set; } 
        public string RegionCode {get; set;}
        public string SeriesCode { get; set; }
        public int Year { get; set; }

        public ChampionshipSeriesDetailViewModel(PinballRankingApiV1 pinballRankingApiV1, PinballRankingApiV2 pinballRankingApiV2, ILogger<ChampionshipSeriesDetailViewModel> logger) : base(pinballRankingApiV1, pinballRankingApiV2, logger)
        {

            RegionStandings = new RegionStandings();     

            LoadItemsCommand = new Command(async () => await ExecuteLoadItemsCommand());
        }

        async Task ExecuteLoadItemsCommand()
        {
            if (IsBusy)
                return;

            IsBusy = true;

            Title = $"{RegionCode} {SeriesCode} ({Year})";

            try
            {
                RegionStandings = await PinballRankingApiV2.GetSeriesStandingsForRegion(SeriesCode, RegionCode, Year);
                OnPropertyChanged("RegionStandings");

                SeriesTournaments = await PinballRankingApiV2.GetSeriesTournamentsForRegion(SeriesCode, RegionCode, Year);
                OnPropertyChanged("SeriesTournaments");
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
    }
}
