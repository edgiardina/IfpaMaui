using Ifpa.Models;
using PinballApi;
using PinballApi.Extensions;
using Shiny.Notifications;
using PinballApi.Models.WPPR.v1.Calendar;
using Microsoft.Extensions.Logging;
using CommunityToolkit.Maui.ApplicationModel;
using Microsoft.Maui.Devices.Sensors;
using System.Net;
using PinballApi.Interfaces;
using PinballApi.Models.WPPR.Universal;
using PinballApi.Models.WPPR.v2.Calendar;

namespace Ifpa.Services
{
    public class NotificationService
    {
        protected BlogPostService BlogPostService { get; set; }

        readonly INotificationManager notificationManager;
        private readonly ILogger<NotificationService> logger;
        private readonly IBadge badge;

        private readonly IGeocoding Geocoding;

        public NotificationService(PinballRankingApiV1 pinballRankingApi, PinballRankingApi universalPinballRankingApi, IGeocoding geocoding, BlogPostService blogPostService, INotificationManager notificationManager, IBadge badge, ILogger<NotificationService> logger)
        {
            PinballRankingApi = pinballRankingApi;
            UniversalPinballRankingApi = universalPinballRankingApi;
            Geocoding = geocoding;

            BlogPostService = blogPostService;
            this.notificationManager = notificationManager;
            this.logger = logger;
            this.badge = badge;
        }
        private PinballRankingApiV1 PinballRankingApi { get; set; }
        private PinballRankingApi UniversalPinballRankingApi { get; set; }

        public static string NewTournamentNotificationTitle = Strings.NotificationService_NewTournamentNotificationTitle;
        protected readonly string NewTournamentNotificationDescription = Strings.NotificationService_NewTournamentNotificationDescription;

        public static string NewRankNotificationTitle = Strings.NotificationService_NewRankNotificationTitle;
        protected readonly string NewRankNotificationDescription = Strings.NotificationService_NewRankNotificationDescription;

        public static string NewBlogPostTitle = Strings.NotificationService_NewBlogPostTitle;
        protected readonly string NewBlogPostDescription = Strings.NotificationService_NewBlogPostDescription;

        public static string NewTournamentOnCalendarTitle = Strings.NotificationService_NewTournamentOnCalendarTitle;
        protected readonly string NewTournamentOnCalendarDescription = Strings.NotificationService_NewTournamentOnCalendarDescription;

        public async Task NotifyIfUserHasNewlySubmittedTourneyResults()
        {
            if (Settings.HasConfiguredMyStats)
            {
                try
                {
                    var results = await PinballRankingApi.GetPlayerResults(Settings.MyStatsPlayerId);
                    
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
                                await UpdateBadgeIfNeeded();
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
                try
                {
                    var results = await PinballRankingApi.GetPlayerRecord(Settings.MyStatsPlayerId);

                    var currentWpprRank = results.PlayerStats.CurrentWpprRank;
                    var lastRecordedWpprRank = Settings.MyStatsCurrentWpprRank;

                    if (currentWpprRank != lastRecordedWpprRank && lastRecordedWpprRank != 0)
                    {
                        if (Settings.NotifyOnRankChange)
                        {
                            await SendNotification(NewRankNotificationTitle, string.Format(NewRankNotificationDescription, lastRecordedWpprRank.OrdinalSuffix(), currentWpprRank.OrdinalSuffix()), "///my-stats/activity-feed");
                            await UpdateBadgeIfNeeded();
                        }

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
            if(Settings.NotifyOnNewBlogPost)
            {
                try
                {
                    var latestPosts = await BlogPostService.GetBlogPosts();

                    var latestGuidInPosts = latestPosts.Max(n => BlogPostService.ParseBlogPostIdFromInternalIdUrl(n.Id));

                    var latestPost = latestPosts.Single(n => n.Id.EndsWith(latestGuidInPosts.ToString()));

                    if (latestGuidInPosts > Settings.LastBlogPostGuid)
                    {
                        if(Settings.LastBlogPostGuid > 0)
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
                try
                {
                    var geoLocation = await Geocoding.GetLocationsAsync(Settings.LastCalendarLocation);

                    var longitude = geoLocation.FirstOrDefault()?.Longitude;
                    var latitude = geoLocation.FirstOrDefault()?.Latitude;
                    var rankingSystem = (RankingSystem?)(Settings.CalendarRankingSystem == "All" ? null : Enum.Parse(typeof(RankingSystem), Settings.CalendarRankingSystem));

                    var items = await UniversalPinballRankingApi.TournamentSearch(latitude,
                                                                 longitude,
                                                                 Settings.LastCalendarDistance,
                                                                 DistanceType.Miles,
                                                                 startDate: DateTime.Now,
                                                                 endDate: DateTime.Now.AddYears(1),
                                                                 rankingSystem: rankingSystem,
                                                                 totalReturn: 500);

                    var newestCalendarItemId = items.Tournaments.Max(n => n.TournamentId);

                    if(newestCalendarItemId > Settings.LastCalendarIdSeen && Settings.LastCalendarIdSeen > 0)
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

        private async Task UpdateBadgeIfNeeded()
        {
            if (DeviceInfo.Current.Platform == DevicePlatform.iOS)
            {
                var unreads = await Settings.LocalDatabase.GetUnreadActivityCount();
           
                badge.SetCount((uint)unreads);
            }
        }

        public async Task SendNotification(string title, string description, string url)
        {
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
}
