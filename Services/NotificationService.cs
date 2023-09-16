using Ifpa.Models;
using PinballApi;
using PinballApi.Extensions;
using Shiny.Notifications;
using PinballApi.Models.WPPR.v1.Calendar;
using Microsoft.Extensions.Logging;

namespace Ifpa.Services
{
    public class NotificationService
    {
        protected BlogPostService BlogPostService { get; set; }

        readonly INotificationManager notificationManager;
        private readonly ILogger<NotificationService> logger;

        public NotificationService(PinballRankingApiV1 pinballRankingApi, BlogPostService blogPostService, INotificationManager notificationManager, ILogger<NotificationService> logger)
        {
            PinballRankingApi = pinballRankingApi;

            BlogPostService = blogPostService;
            this.notificationManager = notificationManager;
            this.logger = logger;
        }
        private PinballRankingApiV1 PinballRankingApi { get; set; }

        public static string NewTournamentNotificationTitle = "New Tournament Result";
        protected readonly string NewTournamentNotificationDescription = @"Tournament results for ""{0}"" have been posted to your IFPA profile";

        public static string NewRankNotificationTitle = "IFPA Rank Change";
        protected readonly string NewRankNotificationDescription = "Your IFPA rank has changed from {0} to {1}";

        public static string NewBlogPostTitle = "New News Item";
        protected readonly string NewBlogPostDescription = @"News item ""{0}"" has been published";

        public static string NewTournamentOnCalendarTitle = "New Tournament on Calendar";
        protected readonly string NewTournamentOnCalendarDescription = @"Tournament ""{0}"" on {1} added to the IFPA calendar";

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
                    var items = await PinballRankingApi.GetCalendarSearch(Settings.LastCalendarLocation, Settings.LastCalendarDistance, DistanceUnit.Miles);

                    var newestCalendarItemId = items.Calendar.Max(n => n.CalendarId);

                    if(newestCalendarItemId > Settings.LastCalendarIdSeen)
                    {
                        foreach (var calendarItem in items.Calendar.Where(n => n.CalendarId > Settings.LastCalendarIdSeen))
                        {
                            await SendNotification(NewTournamentOnCalendarTitle, 
                                                   string.Format(NewTournamentOnCalendarDescription, calendarItem.TournamentName, calendarItem.StartDate.ToShortDateString()), 
                                                   $"///calendar/calendar-detail?calendarId={calendarItem.CalendarId}");
                        }

                        Settings.LastCalendarIdSeen = newestCalendarItemId;

                        //TODO: Add badge to calendar tab item
                        //await UpdateBadgeIfNeeded();
                    }
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
                //TODO: restore badge
                //CrossBadge.Current.SetBadge(unreads);
            }
        }

        public async Task SendNotification(string title, string description, string url)
        {
            var payload = new Dictionary<string, string>();
            payload.Add("url", url);

            var notification = new Notification
            {
                Title = title,
                Message = description,
                Payload = payload
            };

            var result = await notificationManager.RequestRequiredAccess(notification);
            if (result == AccessState.Available)
            {
                await notificationManager.Send(notification);
            }
        }
    }
}
