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
    public class ChampionshipSeriesListViewModel : BaseViewModel
    {
        public ObservableCollection<Series> ChampionshipSeries { get; set; }
        public Command LoadItemsCommand { get; set; }
        
        public ChampionshipSeriesListViewModel(PinballRankingApiV2 pinballRankingApiV2, ILogger<ChampionshipSeriesListViewModel> logger) : base(pinballRankingApiV2, logger)
        {

            ChampionshipSeries = new ObservableCollection<Series>();

            LoadItemsCommand = new Command(async () => await ExecuteLoadItemsCommand());
        }


        async Task ExecuteLoadItemsCommand()
        {
            if (IsBusy)
                return;

            IsBusy = true;

            try
            {
                ChampionshipSeries.Clear();
                var series = await PinballRankingApiV2.GetSeries();

                if (series != null)
                {
                    foreach (var item in series)
                    {
                        ChampionshipSeries.Add(item);
                    }
                }       
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
    }
}
