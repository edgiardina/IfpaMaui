using Ifpa.Interfaces;
using Ifpa.Models;
using Ifpa.Services;
using LiveChartsCore;
using LiveChartsCore.Kernel;
using LiveChartsCore.SkiaSharpView;
using LiveChartsCore.SkiaSharpView.Painting;
using Microsoft.Extensions.Logging;
using PinballApi;
using PinballApi.Extensions;
using PinballApi.Models.v2.WPPR;
using PinballApi.Models.WPPR.v2.Players;
using SkiaSharp;
using Syncfusion.Maui.Core.Carousel;
using System.Collections.ObjectModel;
using System.Diagnostics;

namespace Ifpa.ViewModels
{
    public class PlayerDetailViewModel : BaseViewModel
    {
        AppSettings AppSettings { get; set; }

        public Command LoadItemsCommand { get; set; }

        public int PlayerId { get; set; }

        private Player playerRecord = new Player { PlayerStats = new PlayerStats { }, ChampionshipSeries = new List<ChampionshipSeries> { } };

        private static readonly int s_logBase = 10;

        public Player PlayerRecord
        {
            get { return playerRecord; }
            set
            {
                playerRecord = value;
                OnPropertyChanged(null);
            }
        }
        public ObservableCollection<ISeries> PlayerRankHistoryLineSeries { get; set; } = new ObservableCollection<ISeries>();

        public ObservableCollection<ISeries> PlayerRatingHistoryLineSeries { get; set; } = new ObservableCollection<ISeries>();

        public Axis[] TimeAxis { get; set; } =
        {
            new Axis
            {
                Labeler = value => value.AsDate().ToString("yyyy"),
                UnitWidth = TimeSpan.FromDays(365.25).Ticks,
                MinStep = TimeSpan.FromDays(365.25).Ticks
            }
        };

        public Axis[] RankAxis { get; set; } =
        {
            new Axis
            {
                IsInverted = true,
                Labeler = value => Math.Pow(s_logBase, value).ToString("F0")                
            }
        };

        public Axis[] RatingAxis { get; set; } =
        {
            new Axis
            {
                CustomSeparators = new double[] { 0, 500, 1000, 1500, 2000, 2500 },
                MinLimit = 1,
                MinStep = 1,
                ForceStepToMin = true
            }
        };

        public string Name => PlayerRecord.FirstName + " " + PlayerRecord.LastName;

        public string Initials => PlayerRecord.Initials;

        public int Rank => PlayerRecord.PlayerStats.CurrentWpprRank;

        public string Rating => PlayerRecord.PlayerStats.RatingsRank.HasValue ? PlayerRecord.PlayerStats.RatingsRank.Value.OrdinalSuffix() : "Not Ranked";

        public double? RatingValue => PlayerRecord.PlayerStats.RatingsValue;

        public string EffPercent => PlayerRecord.PlayerStats.EfficiencyRank.HasValue ? PlayerRecord.PlayerStats.EfficiencyRank.Value.OrdinalSuffix() : "Not Ranked";

        public double? EfficiencyValue => PlayerRecord.PlayerStats.EfficiencyValue;

        public double CurrentWpprValue => PlayerRecord.PlayerStats.CurrentWpprValue;

        public string LastMonthRank => PlayerRecord.PlayerStats.LastMonthRank.OrdinalSuffix();

        public string LastYearRank => PlayerRecord.PlayerStats.LastYearRank.OrdinalSuffix();

        public string HighestRank => PlayerRecord.PlayerStats.HighestRank.OrdinalSuffix();

        public DateTime? HighestRankDate => PlayerRecord.PlayerStats.HighestRankDate;

        public double TotalWpprs => PlayerRecord.PlayerStats.WpprPointsAllTime;

        public string BestFinish => PlayerRecord.PlayerStats.BestFinish.OrdinalSuffix();

        public int BestFinishCount => PlayerRecord.PlayerStats.BestFinishCount;

        public int AvgFinish => PlayerRecord.PlayerStats.AverageFinish;

        public int AvgFinishLastYear => PlayerRecord.PlayerStats.AverageFinishLastYear;

        public int TotalEvents => PlayerRecord.PlayerStats.TotalEventsAllTime;

        public int TotalActiveEvents => PlayerRecord.PlayerStats.TotalActiveEvents;

        public int EventsOutsideCountry => PlayerRecord.PlayerStats.TotalEventsAway;

        public string PlayerAvatar
        {
            get
            {
                if (PlayerRecord.ProfilePhoto != null)
                    return PlayerRecord.ProfilePhoto?.ToString();
                else
                    return AppSettings.IfpaPlayerNoProfilePicUrl;
            }
        }

        public bool? HasChampionshipSeriesData => PlayerRecord.ChampionshipSeries?.Any();

        public string CountryFlag => $"https://flagcdn.com/w80/{PlayerRecord.CountryCode?.ToLower()}.png";

        //Replace call at the end so that if a player is missing the 'state' we don't have an unsightly double space.
        public string Location => $"{PlayerRecord.City}{(!string.IsNullOrEmpty(PlayerRecord.City) && !string.IsNullOrEmpty(PlayerRecord.StateProvince) ? "," : string.Empty)} {PlayerRecord.StateProvince} {PlayerRecord.CountryName}".Trim().Replace("  ", " ");

        public bool IsRegistered => PlayerRecord.IfpaRegistered;

        public int BadgeCount { get; set; }

        public PlayerDetailViewModel(PinballRankingApiV2 pinballRankingApiV2, AppSettings appSettings, ILogger<PlayerDetailViewModel> logger) : base(pinballRankingApiV2, logger)
        {
            LoadItemsCommand = new Command(async () => await ExecuteLoadItemsCommand());
            AppSettings = appSettings;
        }

        public async Task ExecuteLoadItemsCommand()
        {
            Color resourceColor = null;
            if(App.Current.Resources.TryGetValue("IconAccentColor", out var color))
            {
                resourceColor = color as Color;
            };       

            try
            {
                if (PlayerId > 0)
                {
                    IsBusy = true;
                    var playerData = await PinballRankingApiV2.GetPlayer(PlayerId);
                    var playerHistoryData = await PinballRankingApiV2.GetPlayerHistory(PlayerId);

                    var playerRankSeries = new LineSeries<RankHistory>
                    {
                        Values = playerHistoryData.RankHistory,
                        Fill = null,
                        GeometryFill = null,
                        GeometryStroke = null,
                        Stroke = new SolidColorPaint(SKColor.Parse(resourceColor.ToHex())) { StrokeThickness = 2 },
                        Mapping = (history, point) =>
                        {
                            point.Coordinate = new Coordinate(history.RankDate.Ticks, Math.Log(history.RankPosition, s_logBase));
                        }
                    };
                    PlayerRankHistoryLineSeries.Clear();
                    PlayerRankHistoryLineSeries.Add(playerRankSeries);


                    var playerRatingSeries = new LineSeries<RatingHistory>
                    {
                        Values = playerHistoryData.RatingHistory,
                        Fill = null,
                        GeometryFill = null,
                        GeometryStroke = null,
                        Stroke = new SolidColorPaint(SKColor.Parse(resourceColor.ToHex())) { StrokeThickness = 2 },
                        Mapping = (history, point) =>
                        {
                            point.Coordinate = new Coordinate(history.RatingDate.Ticks, history.Rating);
                        }
                    };
                    PlayerRatingHistoryLineSeries.Clear();
                    PlayerRatingHistoryLineSeries.Add(playerRatingSeries);

                    OnPropertyChanged();
                    PlayerRecord = playerData;

                    if(PlayerId == Settings.MyStatsPlayerId)
                    {
                        BadgeCount = await Settings.LocalDatabase.GetUnreadActivityCount();
                        OnPropertyChanged(nameof(BadgeCount));
                    }

                    AddPlayerToAppLinks();                   
                }
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error loading player details", PlayerId);
            }
            finally
            {
                IsBusy = false;
            }
        }

        public async Task PrepopulateTourneyResults(int playerId)
        {
            var results = await PinballRankingApiV2.GetPlayerResults(playerId);

            foreach (var result in results.Results)
            {
                var record = new ActivityFeedItem
                {
                    CreatedDateTime = result.EventDate,
                    HasBeenSeen = true,
                    RecordID = result.TournamentId,
                    IntOne = result.Position,
                    ActivityType = ActivityFeedType.TournamentResult,
                    Description = result.TournamentName
                };
                await Settings.LocalDatabase.CreateActivityFeedRecord(record);
            }
        }

        private void AddPlayerToAppLinks()
        {
            var url = $"https://www.ifpapinball.com/player.php?p={PlayerId}";

            var entry = new AppLinkEntry
            {
                Title = Name,
                Description = Rank.OrdinalSuffix(),
                AppLinkUri = new Uri(url, UriKind.RelativeOrAbsolute),
                IsLinkActive = true,
                Thumbnail = ImageSource.FromUri(new Uri(PlayerAvatar, UriKind.RelativeOrAbsolute))
            };

            entry.KeyValues.Add("contentType", "Player");
            entry.KeyValues.Add("appName", "IFPA Companion");
            try
            {
                Application.Current.AppLinks.RegisterLink(entry);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error registering app link {0}", entry);
            }
        }

    }
}
