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
    public class ChampionshipSeriesListViewModel : BaseViewModel
    {
        public ObservableCollection<Series> ChampionshipSeries { get; set; }
        public Command LoadItemsCommand { get; set; }
        
        public ChampionshipSeriesListViewModel(PinballRankingApiV1 pinballRankingApiV1, PinballRankingApiV2 pinballRankingApiV2) : base(pinballRankingApiV1, pinballRankingApiV2)
        {

            ChampionshipSeries = new ObservableCollection<Series>();

            Title = "Championship Series";

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
                Debug.WriteLine(ex);
            }
            finally
            {
                IsBusy = false;
            }
        }
    }
}
