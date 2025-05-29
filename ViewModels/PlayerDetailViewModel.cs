using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Ifpa.Models;
using Ifpa.Models.Database;
using LiveChartsCore;
using LiveChartsCore.Kernel;
using LiveChartsCore.SkiaSharpView;
using LiveChartsCore.SkiaSharpView.Painting;
using Microsoft.Extensions.Logging;
using PinballApi.Interfaces;
using PinballApi.Models.WPPR.Universal.Players;
using PinballApi.Models.WPPR.Universal.Series;
using SkiaSharp;

namespace Ifpa.ViewModels
{
    public partial class PlayerDetailViewModel : BaseViewModel
    {
        [ObservableProperty]
        AppSettings appSettings;

        public int PlayerId { get; set; }

        [ObservableProperty]
        private Player playerRecord = new Player { PlayerStats = new PlayerStats { }, Series = new List<SeriesRank> { } };

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

        [ObservableProperty]
        public Axis[] rankAxis =
        {
            new Axis
            {
                IsInverted = true,
                Labeler = value => Math.Pow(s_logBase, value).ToString("F0"),
                MinLimit = 0, // log10(1)
                MaxLimit = 6, // log10(1,000,000) for example                
                CustomSeparators = new double[] { 0, 1, 2, 3, 4, 5 }, // These represent log10 values for 1, 10, 100, etc.
                MinStep = 1 // Ensure regular spacing for each base-10 step
            }
        };

        [ObservableProperty]
        public Axis[] ratingAxis =
        {
            new Axis
            {
                CustomSeparators = [0, 500, 1000, 1500, 2000, 2500],
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

        private readonly IPinballRankingApi PinballRankingApi;

        private const long TicksInADay = 864000000000;

        public PlayerDetailViewModel(IPinballRankingApi pinballRankingApi, AppSettings appSettings, ILogger<PlayerDetailViewModel> logger) : base(logger)
        {
            AppSettings = appSettings;

            PinballRankingApi = pinballRankingApi;

            // Start page busy to show loading indicator
            IsBusy = true;
        }

        [RelayCommand]
        public async Task LoadItems()
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
                    var playerData = await PinballRankingApi.GetPlayer(PlayerId);
                    var playerHistoryData = await PinballRankingApi.GetPlayerHistory(PlayerId);

                    // Determine min and max rank
                    var minRank = playerHistoryData.RankHistory.Min(h => h.RankPosition);
                    var maxRank = playerHistoryData.RankHistory.Max(h => h.RankPosition);

                    // Calculate logarithmic range
                    var logMin = Math.Floor(Math.Log10(minRank)); // Round down to nearest power of 10
                    var logMax = Math.Ceiling(Math.Log10(maxRank)); // Round up to nearest power of 10

                    // Ensure a minimum of 4 points
                    var logRange = (int)(logMax - logMin) + 1; // Current range
                    if (logRange < 4)
                    {
                        var padding = 4 - logRange; // Calculate how many extra points are needed

                        if(logMin > 0)
                            logMin -= padding; // Extend upward for better ranks
                        else
                            logMax += padding; // Extend downward for better ranks
                    }

                    // Generate log-based separators (log values)
                    var separators = Enumerable.Range((int)logMin, (int)(logMax - logMin) + 1)
                                                .Select(log => (double)log) // Use log values for axis
                                                .ToArray();

                    // Update the RankAxis dynamically
                    RankAxis = new Axis[]
                    {
                        new Axis
                        {
                            IsInverted = true,
                            Labeler = value => Math.Pow(s_logBase, value).ToString("F0"),
                            MinLimit = logMin,
                            MaxLimit = logMax,
                            CustomSeparators = separators,
                            MinStep = 1
                        }
                    };

                    var playerRankSeries = new LineSeries<RankHistory>
                    {
                        Values = playerHistoryData.RankHistory,
                        Fill = null,
                        GeometryFill = null,
                        GeometryStroke = null,
                        Stroke = new SolidColorPaint(SKColor.Parse(resourceColor.ToHex())) { StrokeThickness = 2 },

                        Mapping = (logPoint, index) =>
                                new(logPoint.RankDate.DayNumber * TicksInADay, Math.Log(logPoint.RankPosition, s_logBase))
                    };

                    PlayerRankHistoryLineSeries = new List<ISeries> { playerRankSeries };

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

                    PlayerRatingHistoryLineSeries = new List<ISeries> { playerRatingSeries };

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

                    Location = $"{PlayerRecord.City}{(!string.IsNullOrEmpty(PlayerRecord.City) && !string.IsNullOrEmpty(PlayerRecord.Stateprov) ? "," : string.Empty)} {PlayerRecord.Stateprov} {PlayerRecord.CountryName}".Trim().Replace("  ", " ");
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
                if(PlayerRecord.PlayerId != default)
                {
                    // if we don't get the user's record, keep the busy indicator up
                    IsBusy = false;
                }            
            }
        }

        public async Task PrepopulateTourneyResults(int playerId)
        {
            var results = await PinballRankingApi.GetPlayerResults(playerId);

            foreach (var result in results.Results)
            {
                var record = new ActivityFeedItem
                {
                    CreatedDateTime = result.EventDate.ToDateTime(TimeOnly.MinValue),
                    HasBeenSeen = true,
                    RecordID = result.TournamentId,
                    IntOne = result.Position,
                    ActivityType = ActivityFeedType.TournamentResult,
                    Description = result.TournamentName
                };
                await Settings.LocalDatabase.CreateActivityFeedRecord(record);
            }
        }

        [RelayCommand]
        public async Task ShowPlayerChampionshipSeries()
        {
            await Shell.Current.GoToAsync($"player-champ-series?playerId={PlayerId}");
        }

        [RelayCommand]
        public async Task ShowPlayerVsPlayer()
        {
            await Shell.Current.GoToAsync($"pvp?playerId={PlayerId}");
        }

        [RelayCommand]
        public async Task ShowPlayerTournamentResults()
        {
            await Shell.Current.GoToAsync($"player-results?playerId={PlayerId}");
        }

        private void AddPlayerToAppLinks()
        {
            var url = $"https://www.ifpapinball.com/player.php?p={PlayerId}";

            var entry = new AppLinkEntry
            {
                Title = PlayerRecord.FirstName + " " + PlayerRecord.LastName,
                Description = PlayerRecord.GetRank(),
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
