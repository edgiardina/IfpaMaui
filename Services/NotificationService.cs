using CommunityToolkit.Maui.ApplicationModel;
using Ifpa.Models;
using Microsoft.Extensions.Logging;
using PinballApi;
using PinballApi.Models.WPPR;
using PinballApi.Models.WPPR.Universal;
using Shiny.Notifications;

namespace Ifpa.Services
{
    public class NotificationService
    {
        protected BlogPostService BlogPostService { get; set; }

        readonly INotificationManager notificationManager;
        private readonly ILogger<NotificationService> logger;
        private readonly IBadge badge;

        private readonly IGeocoding Geocoding;

        public NotificationService(PinballRankingApiV2 pinballRankingApiV2, PinballRankingApi universalPinballRankingApi, IGeocoding geocoding, BlogPostService blogPostService, INotificationManager notificationManager, IBadge badge, ILogger<NotificationService> logger)
        {
            PinballRankingApiV2 = pinballRankingApiV2;
            UniversalPinballRankingApi = universalPinballRankingApi;
            Geocoding = geocoding;

            BlogPostService = blogPostService;
            this.notificationManager = notificationManager;
            this.logger = logger;
            this.badge = badge;
        }
        private PinballRankingApiV2 PinballRankingApiV2 { get; set; }
        private PinballRankingApi UniversalPinballRankingApi { get; set; }

        public static string NewTournamentNotificationTitle = Strings.NotificationService_NewTournamentNotificationTitle;
        protected readonly string NewTournamentNotificationDescription = Strings.NotificationService_NewTournamentNotificationDescription;

        public static string NewRankNotificationTitle = Strings.NotificationService_NewRankNotificationTitle;
        protected readonly string NewRankNotificationDescription = Strings.NotificationService_NewRankNotificationDescription;

        public static string NewBlogPostTitle = Strings.NotificationService_NewBlogPostTitle;
        protected readonly string NewBlogPostDescription = Strings.NotificationService_NewBlogPostDescription;

        public static string NewTournamentOnCalendarTitle = Strings.NotificationService_NewTournamentOnCalendarTitle;
        protected readonly string NewTournamentOnCalendarDescription = Strings.NotificationService_NewTournamentOnCalendarDescription;

        public event EventHandler<ActivityFeedNotificationChangedEventArgs> ActivityFeedNotificationChanged;

        public async Task NotifyIfUserHasNewlySubmittedTourneyResults()
        {
            if (Settings.HasConfiguredMyStats)
            {
                logger.LogInformation("Checking for user's new tournament results");

                try
                {

                    var results = await PinballRankingApiV2.GetPlayerResults(Settings.MyStatsPlayerId);

                    var unseenTournaments = await Settings.FindUnseenTournaments(results.Results);

                    if (unseenTournaments.Any())
                    {
                        // Five here is a proxy for 
                        // "did we switch users and therefore are adding all historical events to activity log"
                        // We need historical data to know when a user should get alerted due to a new tournament

                        // Note: this is deprecated as we pre-populate tournaments at the time of user selection. However, we'll leave 
                        // it here in case somehow a user is added without tourney results
                        var isHistoricalEventPopulation = unseenTournaments.Count() >= 5;

                        foreach (var unseenTournament in unseenTournaments.OrderBy(n => n))
                        {
                            var result = results.Results.Single(n => n.TournamentId == unseenTournament);

                            var record = new ActivityFeedItem
                            {
                                CreatedDateTime = isHistoricalEventPopulation ? result.EventDate : DateTime.Now,
                                HasBeenSeen = isHistoricalEventPopulation,
                                RecordID = result.TournamentId,
                                IntOne = result.Position,
                                ActivityType = ActivityFeedType.TournamentResult,
                                Description = result.TournamentName
                            };

                            await Settings.LocalDatabase.CreateActivityFeedRecord(record);

                            if (Settings.NotifyOnTournamentResult && !isHistoricalEventPopulation)
                            {
                                await SendNotification(NewTournamentNotificationTitle, string.Format(NewTournamentNotificationDescription, result.TournamentName), $"///my-stats/tournament-results?tournamentId={result.TournamentId}");

                                await RecalculateActivityFeedAndUpdateBadges(record);
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    logger.LogError(ex, "Error in NotifyIfUserHasNewlySubmittedTourneyResults");
                }
            }
        }

        public async Task NotifyIfUsersRankChanged()
        {
            if (Settings.HasConfiguredMyStats)
            {
                logger.LogInformation("Checking for user's rank change");

                try
                {
                    var results = await PinballRankingApiV2.GetPlayer(Settings.MyStatsPlayerId);

                    var currentWpprRank = results.PlayerStats.CurrentWpprRank;
                    var lastRecordedWpprRank = Settings.MyStatsCurrentWpprRank;

                    if (currentWpprRank != lastRecordedWpprRank && lastRecordedWpprRank != 0)
                    {
                        var record = new ActivityFeedItem
                        {
                            CreatedDateTime = DateTime.Now,
                            HasBeenSeen = false,
                            IntOne = currentWpprRank,
                            IntTwo = lastRecordedWpprRank,
                            ActivityType = ActivityFeedType.RankChange
                        };

                        Settings.MyStatsCurrentWpprRank = currentWpprRank;

                        await Settings.LocalDatabase.CreateActivityFeedRecord(record);

                        if (Settings.NotifyOnRankChange)
                        {
                            await RecalculateActivityFeedAndUpdateBadges(record);
                        }
                    }
                }
                catch (Exception ex)
                {
                    logger.LogError(ex, "Error in NotifyIfUsersRankChanged");
                }
            }
        }

        public async Task NotifyIfNewBlogItemPosted()
        {
            if (Settings.NotifyOnNewBlogPost)
            {
                logger.LogInformation("Checking for new blog posts");

                try
                {
                    var latestPosts = await BlogPostService.GetBlogPosts();

                    var latestGuidInPosts = latestPosts.Max(n => BlogPostService.ParseBlogPostIdFromInternalIdUrl(n.Id));

                    var latestPost = latestPosts.Single(n => n.Id.EndsWith(latestGuidInPosts.ToString()));

                    if (latestGuidInPosts > Settings.LastBlogPostGuid)
                    {
                        if (Settings.LastBlogPostGuid > 0)
                        {
                            await SendNotification(NewBlogPostTitle, string.Format(NewBlogPostDescription, latestPost.Title.Text), $"///more/news/news-detail?newsUri={latestPost.Links.FirstOrDefault().Uri}");
                        }

                        Settings.LastBlogPostGuid = latestGuidInPosts;
                    }

                }
                catch (Exception ex)
                {
                    logger.LogError(ex, "Error in NotifyIfNewBlogItemPosted");
                }
            }
        }

        public async Task NotifyIfNewCalendarEntry()
        {
            if (Settings.NotifyOnNewCalendarEntry)
            {
                logger.LogInformation("Checking for new calendar entries");

                try
                {
                    var geoLocation = await Geocoding.GetLocationsAsync(Settings.LastCalendarLocation);

                    var longitude = geoLocation.FirstOrDefault()?.Longitude;
                    var latitude = geoLocation.FirstOrDefault()?.Latitude;
                    var tournamentType = (TournamentType?)(Settings.CalendarRankingSystem == "All" ? null : Enum.Parse(typeof(TournamentType), Settings.CalendarRankingSystem));

                    var items = await UniversalPinballRankingApi.TournamentSearch(latitude,
                                                                 longitude,
                                                                 Settings.LastCalendarDistance,
                                                                 DistanceType.Miles,
                                                                 startDate: DateTime.Now,
                                                                 endDate: DateTime.Now.AddYears(1),
                                                                 tournamentType: tournamentType,
                                                                 totalReturn: 500);

                    var newestCalendarItemId = items.Tournaments.Max(n => n.TournamentId);

                    if (newestCalendarItemId > Settings.LastCalendarIdSeen && Settings.LastCalendarIdSeen > 0)
                    {
                        foreach (var calendarItem in items.Tournaments.Where(n => n.TournamentId > Settings.LastCalendarIdSeen))
                        {
                            await SendNotification(NewTournamentOnCalendarTitle,
                                                   string.Format(NewTournamentOnCalendarDescription, calendarItem.TournamentName, calendarItem.EventStartDate.DateTime.ToShortDateString()),
                                                   $"///calendar/calendar-detail?tournamentId={calendarItem.TournamentId}");
                        }

                        //TODO: Add badge to calendar tab item
                        //await UpdateBadgeIfNeeded();
                    }

                    Settings.LastCalendarIdSeen = newestCalendarItemId;
                }
                catch (Exception ex)
                {
                    logger.LogError(ex, "Error in NotifyIfNewCalendarEntry");
                }
            }
        }

        public async Task ClearNotificationForActivityFeedItem(ActivityFeedItem item)
        {
            item.HasBeenSeen = true;
            await Settings.LocalDatabase.UpdateActivityFeedRecord(item);

            await RecalculateActivityFeedAndUpdateBadges(item);
        }

        public async Task RecalculateActivityFeedAndUpdateBadges(ActivityFeedItem activityFeedItem)
        {
            var unreads = await Settings.LocalDatabase.GetUnreadActivityCount();

            ActivityFeedNotificationChanged?.Invoke(this, new ActivityFeedNotificationChangedEventArgs
            {
                ActivityFeedItem = activityFeedItem,
                UnreadCount = unreads
            });

            await UpdateBadgeIfNeeded();
        }

        public async Task TestingShim()
        {
            var newItem = new ActivityFeedItem
            {
                ActivityType = ActivityFeedType.TournamentResult,
                RecordID = 28089,
                CreatedDateTime = DateTime.Now,
                HasBeenSeen = false
            };

            await Settings.LocalDatabase.CreateActivityFeedRecord(newItem);

            await RecalculateActivityFeedAndUpdateBadges(newItem);
        }

        private async Task UpdateBadgeIfNeeded()
        {
            try
            {
                var unreads = await Settings.LocalDatabase.GetUnreadActivityCount();

                badge.SetCount((uint)unreads);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error in UpdateBadgeIfNeeded");
            }
        }

        private async Task SendNotification(string title, string description, string url)
        {
            logger.LogInformation("Sending notification: {0} - {1}", title, description);

            var payload = new Dictionary<string, string>
            {
                { "url", url }
            };

            var notification = new Notification
            {
                Title = title,
                Message = description,
                Payload = payload
            };


            var result = await notificationManager.RequestRequiredAccess(notification);
            if (result == Shiny.AccessState.Available)
            {
                await notificationManager.Send(notification);
            }
        }
    }

    public class ActivityFeedNotificationChangedEventArgs : EventArgs
    {
        public ActivityFeedItem ActivityFeedItem { get; set; }
        public int UnreadCount { get; set; }
    }

}
