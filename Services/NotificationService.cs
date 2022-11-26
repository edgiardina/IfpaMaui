using Ifpa.Models;
using PinballApi;
using PinballApi.Extensions;
using Plugin.LocalNotification;
using Microsoft.Extensions.Configuration;

namespace Ifpa.Services
{
    public class NotificationService
    {
        protected AppSettings AppSettings { get; set; }
        protected BlogPostService BlogPostService { get; set; } 

        public NotificationService(IConfiguration config, BlogPostService blogPostService)
        {
            AppSettings = config.GetRequiredSection("AppSettings").Get<AppSettings>();

            PinballRankingApi = new PinballRankingApiV1(AppSettings.IfpaApiKey);

            BlogPostService = blogPostService;
        }
        private PinballRankingApiV1 PinballRankingApi { get; set; }

        public static string NewTournamentNotificationTitle = "New Tournament Result";
        protected readonly string NewTournamentNotificationDescription = @"Tournament results for ""{0}"" have been posted to your IFPA profile";

        public static string NewRankNotificationTitle = "IFPA Rank Change";
        protected readonly string NewRankNotificationDescription = "Your IFPA rank has changed from {0} to {1}";

        public static string NewBlogPostTitle = "New News Item";
        protected readonly string NewBlogPostDescription = @"News item ""{0}"" has been published";

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
                    Console.WriteLine(ex.Message);
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
                    Console.WriteLine(ex.Message);
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

                    var latestGuidInPosts = latestPosts.Max(n => BlogPostService.ParseGuidFromInternalId(n.Id));

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
                    Console.WriteLine(ex.Message);
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
            var notification = new NotificationRequest
            {
                Title = title,
                Description = description,
                ReturningData = url
            };

            await LocalNotificationCenter.Current.Show(notification);
        }
    }
}
