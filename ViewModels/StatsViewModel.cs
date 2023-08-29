using System.Collections.ObjectModel;
using System.Diagnostics;
using PinballApi.Models.WPPR.v1.Statistics;
using Ifpa.Models;
using PinballApi;
using LiveChartsCore.SkiaSharpView;
using LiveChartsCore;
using LiveChartsCore.SkiaSharpView.Painting;
using SkiaSharp;
using Microsoft.Extensions.Logging;

namespace Ifpa.ViewModels
{

    public class StatsViewModel : BaseViewModel
    {

        public List<ISeries> PlayersByCountrySeries { get; set; } = new List<ISeries>();

        public List<ISeries> EventsByYearSeries { get; set; } = new List<ISeries>();

        public List<Axis> EventsByYearAxis { get; set; } = new List<Axis>();

        public List<ISeries> PlayersByYearSeries { get; set; } = new List<ISeries>();

        public List<Axis> PlayersByYearAxis { get; set; } = new List<Axis>();

        public ObservableCollectionRange<PointsThisYearStat> MostPointsPlayers { get; set; }

        public ObservableCollectionRange<MostEventsStat> MostEventsPlayers { get; set; }

        public ObservableCollectionRange<BiggestMoversStat> BiggestMovers { get; set; }

        public StatsViewModel(PinballRankingApiV1 pinballRankingApiV1, PinballRankingApiV2 pinballRankingApiV2, ILogger<StatsViewModel> logger) : base(pinballRankingApiV1, pinballRankingApiV2, logger)
        {
            Title = "Stats";
            MostPointsPlayers = new ObservableCollectionRange<PointsThisYearStat>();
            MostEventsPlayers = new ObservableCollectionRange<MostEventsStat>();
            BiggestMovers = new ObservableCollectionRange<BiggestMoversStat>();
        }

        public async Task ExecuteLoadItemsCommand()
        {
            if (IsBusy)
                return;

            IsBusy = true;

            try
            {
                MostPointsPlayers.Clear();
                MostEventsPlayers.Clear();
                BiggestMovers.Clear();

                var playersByCountryTask = PinballRankingApi.GetPlayersByCountryStat();
                var eventsByYearTask = PinballRankingApi.GetEventsPerYearStat();
                var playersByYearTask = PinballRankingApi.GetPlayersPerYearStat();
                var mostPointsPlayersTask = PinballRankingApi.GetPointsThisYearStats();
                //var mostEventsPlayersTask = PinballRankingApi.GetMostEventsStats();
                var biggestMoversTask = PinballRankingApi.GetBiggestMoversStat();

                await Task.WhenAll(playersByCountryTask, 
                                   eventsByYearTask,
                                   playersByYearTask,
                                   mostPointsPlayersTask,
                                   //mostEventsPlayersTask,
                                   biggestMoversTask);

                var playersByCountry = await playersByCountryTask;
                var eventsByYear = await eventsByYearTask;
                var playersByYear = await playersByYearTask;
                var mostPointsPlayers = await mostPointsPlayersTask;
                //var mostEventsPlayers = await mostEventsPlayersTask;
                var biggestMovers = await biggestMoversTask;

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

                EventsByYearSeries.Add(new ColumnSeries<int>
                {
                    Name = "Events",
                    Values = eventsByYear.OrderBy(x => x.Year).Select(x => x.Count).ToArray()
                });

                EventsByYearAxis.Add(new Axis
                {
                    Labels = eventsByYear.OrderBy(x => x.Year).Select(x => x.Year.ToString()).ToList(),
                    LabelsRotation = 0,
                    SeparatorsAtCenter = false,
                    TicksAtCenter = true
                });

                PlayersByYearAxis.Add(new Axis
                {
                    Labels = playersByYear.OrderBy(x => x.Year).Select(x => x.Year.ToString()).ToList(),
                    LabelsRotation = 0,
                    SeparatorsAtCenter = false,
                    TicksAtCenter = true,
                });

                PlayersByYearSeries.Add(new ColumnSeries<int>
                {
                    Name = "Players",
                    Values = playersByYear.OrderBy(x => x.Year).Select(x => x.Count).ToArray()
                });

                OnPropertyChanged();

                MostPointsPlayers.AddRange(mostPointsPlayers);
                //MostEventsPlayers.AddRange(mostEventsPlayers);
                BiggestMovers.AddRange(biggestMovers);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error loading stats");
            }
            finally
            {
                IsBusy = false;
            }
        }
    }
}