using System.Collections.ObjectModel;
using System.Diagnostics;
using PinballApi.Models.WPPR.v1.Statistics;
using Ifpa.Models;
using PinballApi;
using LiveChartsCore.SkiaSharpView;
using LiveChartsCore;
using LiveChartsCore.SkiaSharpView.Painting;
using SkiaSharp;

namespace Ifpa.ViewModels
{

    public class StatsViewModel : BaseViewModel
    {

        public List<ISeries> PlayersByCountrySeries { get; set; } = new List<ISeries>();

        public List<ISeries> EventsByYearSeries { get; set; } = new List<ISeries>();

        public List<Axis> EventsByYearAxis { get; set; } = new List<Axis>();

        public List<ISeries> PlayersByYearSeries { get; set; } = new List<ISeries> {
            new ColumnSeries<int>
            {
                    Name = "Players" 
            }
        };

        public List<Axis> PlayersByYearAxis { get; set; } = new List<Axis>();

        public ObservableCollectionRange<PointsThisYearStat> MostPointsPlayers { get; set; }

        public ObservableCollectionRange<MostEventsStat> MostEventsPlayers { get; set; }

        public ObservableCollectionRange<BiggestMoversStat> BiggestMovers { get; set; }

        public Command LoadItemsCommand { get; set; }

        public StatsViewModel(PinballRankingApiV1 pinballRankingApiV1, PinballRankingApiV2 pinballRankingApiV2) : base(pinballRankingApiV1, pinballRankingApiV2)
        {
            Title = "Stats";
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
                MostPointsPlayers.Clear();
                MostEventsPlayers.Clear();
                BiggestMovers.Clear();

                var playersByCountry = await PinballRankingApi.GetPlayersByCountryStat();

                var groupedStats = playersByCountry.GroupBy(
                    stat => stat.Count < 100 ? "Other" : stat.CountryName,
                    (key, group) => new PlayersByCountryStat
                    {
                        Count = key == "Other" ? group.Sum(item => item.Count) : group.First().Count,
                        CountryName = key
                    });

                foreach (var item in groupedStats)
                {
                    var series = new PieSeries<int>
                    {
                        Values = new List<int> { item.Count },
                        Name = item.CountryName
                    };

                    PlayersByCountrySeries.Add(series);
                }

                var eventsByYear = await PinballRankingApi.GetEventsPerYearStat();

                var eventsByYearColumnSeries = new ColumnSeries<int>
                {
                    Name = "Events",
                    Values = eventsByYear.OrderBy(x => x.Year).Select(x => x.Count).ToArray()
                };

                var eventsByYearAxis = new Axis
                {
                    Labels = eventsByYear.OrderBy(x => x.Year).Select(x => x.Year.ToString()).ToList(),
                    LabelsRotation = 0,
                    SeparatorsAtCenter = false,
                    TicksAtCenter = true
                };

                EventsByYearSeries.Add(eventsByYearColumnSeries);
                EventsByYearAxis.Add(eventsByYearAxis);

                var playersByYear = await PinballRankingApi.GetPlayersPerYearStat();

                PlayersByYearSeries[0].Values = playersByYear.OrderBy(x => x.Year).Select(x => x.Count).ToArray();           

                var playersByYearAxis = new Axis
                {
                    Labels = playersByYear.OrderBy(x => x.Year).Select(x => x.Year.ToString()).ToList(),
                    LabelsRotation = 0,
                    SeparatorsAtCenter = false,
                    TicksAtCenter = true,
                };

                PlayersByYearAxis.Add(playersByYearAxis);

                OnPropertyChanged();

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