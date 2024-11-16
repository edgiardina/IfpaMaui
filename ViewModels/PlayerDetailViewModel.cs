using CommunityToolkit.Mvvm.ComponentModel;
using Ifpa.Models;
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

namespace Ifpa.ViewModels
{
    public partial class PlayerDetailViewModel : BaseViewModel
    {
        [ObservableProperty]
        AppSettings appSettings;

        public Command LoadItemsCommand { get; set; }

        public int PlayerId { get; set; }

        [ObservableProperty]
        private Player playerRecord = new Player { PlayerStats = new PlayerStats { }, ChampionshipSeries = new List<ChampionshipSeries> { } };

        private static readonly int s_logBase = 10;

        [ObservableProperty]
        private List<ISeries> playerRankHistoryLineSeries = new List<ISeries>();

        [ObservableProperty]
        private List<ISeries> playerRatingHistoryLineSeries = new List<ISeries>();

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

        [ObservableProperty]
        private string countryFlag;

        //Replace call at the end so that if a player is missing the 'state' we don't have an unsightly double space.
        [ObservableProperty]
        private string location;

        [ObservableProperty]
        private int badgeCount;

        [ObservableProperty]
        private string playerAvatar;

        public PlayerDetailViewModel(PinballRankingApiV2 pinballRankingApiV2, AppSettings appSettings, ILogger<PlayerDetailViewModel> logger) : base(pinballRankingApiV2, logger)
        {
            LoadItemsCommand = new Command(async () => await ExecuteLoadItemsCommand());
            AppSettings = appSettings;
        }

        public async Task ExecuteLoadItemsCommand()
        {
            Color resourceColor = null;
            if (App.Current.Resources.TryGetValue("IconAccentColor", out var color))
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
                        Mapping = (history, number) =>
                        {
                            return new Coordinate(history.RankDate.Ticks, Math.Log(history.RankPosition, s_logBase));
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
                        Mapping = (history, number) =>
                        {
                            return new Coordinate(history.RatingDate.Ticks, history.Rating);
                        }
                    };
                    PlayerRatingHistoryLineSeries.Clear();
                    PlayerRatingHistoryLineSeries.Add(playerRatingSeries);

                    PlayerRecord = playerData;

                    if (PlayerId == Settings.MyStatsPlayerId)
                    {
                        BadgeCount = await Settings.LocalDatabase.GetUnreadActivityCount();
                    }

                    if (PlayerRecord.ProfilePhoto != null && string.IsNullOrWhiteSpace(PlayerRecord.ProfilePhoto.ToString()) == false)
                    {
                        PlayerAvatar = PlayerRecord.ProfilePhoto?.ToString();
                    }
                    else
                    {
                        PlayerAvatar = AppSettings.IfpaPlayerNoProfilePicUrl;
                    }

                    Location = $"{PlayerRecord.City}{(!string.IsNullOrEmpty(PlayerRecord.City) && !string.IsNullOrEmpty(PlayerRecord.StateProvince) ? "," : string.Empty)} {PlayerRecord.StateProvince} {PlayerRecord.CountryName}".Trim().Replace("  ", " ");
                    CountryFlag = $"https://flagcdn.com/w80/{PlayerRecord.CountryCode?.ToLower()}.png";

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
                Title = PlayerRecord.FirstName + " " + PlayerRecord.LastName,
                Description = PlayerRecord.PlayerStats.CurrentWpprRank.OrdinalSuffix(),
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
