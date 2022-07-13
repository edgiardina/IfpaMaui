using PinballApi.Models.WPPR.v2.Nacs;
using PinballApi.Models.WPPR.v2.Series;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Maui;
using Microsoft.Extensions.Configuration;

namespace Ifpa.ViewModels
{
    public class ChampionshipSeriesViewModel : BaseViewModel
    {
        public ObservableCollection<SeriesOverallResult> SeriesOverallResults { get; set; }
        public Command LoadItemsCommand { get; set; }

        public List<int> AvailableYears { get; set; }

        public int Year { get; set; }

        public string SeriesCode { get; set; }
        
        public ChampionshipSeriesViewModel(IConfiguration config) : base(config)
        {    
            this.AvailableYears = new List<int>();

            SeriesOverallResults = new ObservableCollection<SeriesOverallResult>();

            LoadItemsCommand = new Command(async () => await ExecuteLoadItemsCommand());
        }

        async Task ExecuteLoadItemsCommand()
        {
            if (IsBusy)
                return;

            Title = SeriesCode;

            IsBusy = true;

            try
            {
                SeriesOverallResults.Clear();
                var stateProvinceChampionshipSeries = await PinballRankingApiV2.GetSeriesOverallStanding(SeriesCode, Year);
                var seriesDetails = await PinballRankingApiV2.GetSeries();

                AvailableYears = seriesDetails.First(n => n.Code == SeriesCode).Years;

                if (stateProvinceChampionshipSeries != null)
                {
                    foreach (var item in stateProvinceChampionshipSeries.OverallResults)
                    {
                        SeriesOverallResults.Add(item);
                    }
                }
                
                if(Year != DateTime.Now.Year)
                {
                    Title = $"{SeriesCode} {Year}";
                    OnPropertyChanged("Title");
                }
                else
                {
                    Title = SeriesCode;
                    OnPropertyChanged("Title");
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
