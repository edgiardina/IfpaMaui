using System.Collections.ObjectModel;
using System.Diagnostics;
using PinballApi.Models.WPPR.v1.Statistics;
using Ifpa.Models;
using PinballApi;

namespace Ifpa.ViewModels
{

    public class StatsViewModel : BaseViewModel
    {
        public ObservableCollection<PlayersByCountryStat> PlayersByCountry { get; set; }
        public ObservableCollection<EventsByYearStat> EventsByYear { get; set; }

        public ObservableCollection<PlayersByYearStat> PlayersByYear { get; set; }

        public ObservableCollectionRange<PointsThisYearStat> MostPointsPlayers { get; set; }

        public ObservableCollectionRange<MostEventsStat> MostEventsPlayers { get; set; }

        public ObservableCollectionRange<BiggestMoversStat> BiggestMovers { get; set; }

        public Command LoadItemsCommand { get; set; }

        public StatsViewModel(PinballRankingApiV1 pinballRankingApiV1, PinballRankingApiV2 pinballRankingApiV2) : base(pinballRankingApiV1, pinballRankingApiV2)
        {
            Title = "Stats";
            PlayersByCountry = new ObservableCollection<PlayersByCountryStat>();
            EventsByYear = new ObservableCollection<EventsByYearStat>();
            PlayersByYear = new ObservableCollection<PlayersByYearStat>();
            MostPointsPlayers = new ObservableCollectionRange<PointsThisYearStat>();
            MostEventsPlayers = new ObservableCollectionRange<MostEventsStat>();
            BiggestMovers = new ObservableCollectionRange<BiggestMoversStat>();

            LoadItemsCommand = new Command(async () => await ExecuteLoadItemsCommand());
        }

        async Task ExecuteLoadItemsCommand()
        {
            if (IsBusy)
                return;

            IsBusy = true;

            try
            {
                PlayersByCountry.Clear();
                EventsByYear.Clear();
                PlayersByYear.Clear();
                MostPointsPlayers.Clear();
                MostEventsPlayers.Clear();
                BiggestMovers.Clear();

                var playersByCountry = await PinballRankingApi.GetPlayersByCountryStat();
                foreach (var item in playersByCountry)
                {
                    PlayersByCountry.Add(item);
                }

                var eventsByYear = await PinballRankingApi.GetEventsPerYearStat();
                foreach (var item in eventsByYear)
                {
                    EventsByYear.Add(item);
                }

                var playersByYear = await PinballRankingApi.GetPlayersPerYearStat();
                foreach (var item in playersByYear)
                {
                    PlayersByYear.Add(item);
                }

                var mostPointsPlayers = await PinballRankingApi.GetPointsThisYearStats();
                MostPointsPlayers.AddRange(mostPointsPlayers);

                var mostEventsPlayers = await PinballRankingApi.GetMostEventsStats();
                MostEventsPlayers.AddRange(mostEventsPlayers);

                var biggestMovers = await PinballRankingApi.GetBiggestMoversStat();
                BiggestMovers.AddRange(biggestMovers);

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