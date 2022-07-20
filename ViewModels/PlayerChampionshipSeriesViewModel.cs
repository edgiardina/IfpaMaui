using PinballApi.Models.v2.WPPR;
using System;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Maui;
using Microsoft.Extensions.Configuration;

namespace Ifpa.ViewModels
{
    public class PlayerChampionshipSeriesViewModel : BaseViewModel
    {
        public ObservableCollection<ChampionshipSeries> ChampionshipSeries { get; set; }
        public Command LoadItemsCommand { get; set; }

        public int PlayerId { get; set; }

        public PlayerChampionshipSeriesViewModel(IConfiguration config) : base(config)
        {
            ChampionshipSeries = new ObservableCollection<ChampionshipSeries>();

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
                var player = await PinballRankingApiV2.GetPlayer(PlayerId);

                foreach (var item in player.ChampionshipSeries.Where(n => n.Year == DateTime.Now.Year))
                {
                    ChampionshipSeries.Add(item);
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
