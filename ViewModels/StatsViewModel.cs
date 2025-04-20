using CommunityToolkit.Maui.Core.Extensions;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using LiveChartsCore;
using LiveChartsCore.SkiaSharpView;
using Microsoft.Extensions.Logging;
using PinballApi.Interfaces;
using PinballApi.Models.WPPR.Universal.Series;
using PinballApi.Models.WPPR.Universal.Stats;
using System.Collections.ObjectModel;

namespace Ifpa.ViewModels
{

    public partial class StatsViewModel : BaseViewModel
    {

        public List<ISeries> PlayersByCountrySeries { get; set; } = new List<ISeries>();

        public List<ISeries> EventsByYearSeries { get; set; } = new List<ISeries>();

        public List<Axis> EventsByYearAxis { get; set; } = new List<Axis>();

        public List<ISeries> PlayersByYearSeries { get; set; } = new List<ISeries>();

        public List<Axis> PlayersByYearAxis { get; set; } = new List<Axis>();

        public ObservableCollection<PlayersPointsByGivenPeriodStatistics> MostPointsPlayers { get; set; }

        public ObservableCollection<PlayersEventsAttendedByGivenPeriodStatistics> MostEventsPlayers { get; set; }

        //public ObservableCollectionRange<BiggestMoversStat> BiggestMovers { get; set; }

        private readonly IPinballRankingApi PinballRankingApi;

        [ObservableProperty]
        private int selectedYear = DateTime.Now.Year;

        public StatsViewModel(IPinballRankingApi pinballRankingApi, ILogger<StatsViewModel> logger) : base(logger)
        {
            Title = "Stats";
            MostPointsPlayers = new ObservableCollection<PlayersPointsByGivenPeriodStatistics>();
            MostEventsPlayers = new ObservableCollection<PlayersEventsAttendedByGivenPeriodStatistics>();
            //BiggestMovers = new ObservableCollectionRange<BiggestMoversStat>();
            PinballRankingApi = pinballRankingApi;
        }

        [RelayCommand]
        public async Task LoadItems()
        {
            if (IsBusy)
                return;

            IsBusy = true;

            try
            {
                MostPointsPlayers.Clear();
                MostEventsPlayers.Clear();
                //BiggestMovers.Clear();

                var firstDay = new DateOnly(SelectedYear, 1, 1);
                var lastDay = new DateOnly(SelectedYear, 12, 31);

                var playersByCountryTask = PinballRankingApi.GetPlayersByCountryStatistics();
                var eventsByYearTask = PinballRankingApi.GetEventsByYearStatistics();
                var playersByYearTask = PinballRankingApi.GetPlayersByYearStatistics();
                var mostPointsPlayersTask = PinballRankingApi.GetPlayersPointsByGivenPeriod(firstDay, lastDay);
                var mostEventsPlayersTask = PinballRankingApi.GetPlayersEventsAttendedByGivenPeriod(firstDay, lastDay);
                //var biggestMoversTask = PinballRankingApiV2..GetBiggestMoversStat();

                await Task.WhenAll(playersByCountryTask,
                                   eventsByYearTask,
                                   playersByYearTask,
                                   mostPointsPlayersTask,
                                   mostEventsPlayersTask
                                   //biggestMoversTask
                                   );

                var playersByCountry = await playersByCountryTask;
                var eventsByYear = await eventsByYearTask;
                var playersByYear = await playersByYearTask;
                var mostPointsPlayers = await mostPointsPlayersTask;
                var mostEventsPlayers = await mostEventsPlayersTask;
                //var biggestMovers = await biggestMoversTask;

                var groupedStats = playersByCountry.GroupBy(
                    stat => stat.PlayerCount < 100 ? "Other" : stat.CountryName,
                    (key, group) => new PlayersByCountryStatistics
                    {
                        PlayerCount = key == "Other" ? group.Sum(item => item.PlayerCount) : group.First().PlayerCount,
                        CountryName = key
                    });

                foreach (var item in groupedStats)
                {
                    var series = new PieSeries<int>
                    {
                        Values = new List<int> { item.PlayerCount },
                        Name = item.CountryName
                    };

                    PlayersByCountrySeries.Add(series);
                }

                EventsByYearSeries.Add(new ColumnSeries<int>
                {
                    Name = "Events",
                    Values = eventsByYear.OrderBy(x => x.Year).Select(x => x.PlayerCount).ToArray()
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
                    Values = playersByYear.OrderBy(x => x.Year).Select(x => x.CurrentYearCount).ToArray()
                });

                OnPropertyChanged();

                MostPointsPlayers = mostPointsPlayers.ToObservableCollection();
                MostEventsPlayers = mostEventsPlayers.ToObservableCollection();
                //BiggestMovers.AddRange(biggestMovers);

                OnPropertyChanged(nameof(MostPointsPlayers));
                OnPropertyChanged(nameof(MostEventsPlayers));

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

        [RelayCommand]
        public async Task SelectYear()
        {
            var seriesDetails = await PinballRankingApi.GetSeries();
            var availableYears = seriesDetails.Where(sd => sd.Years != null)
                                              .SelectMany(sd => sd.Years)
                                              .OrderBy(y => y)
                                              .Distinct()
                                              .ToList();

            string action = await Shell.Current.DisplayActionSheet("Select Year", "Cancel", null, availableYears.Select(y => y.ToString()).ToArray());

            if (int.TryParse(action, out var year))
            {
                SelectedYear = year;
            }

            logger.LogInformation($"Selected year: {SelectedYear}");
        }
    }
}