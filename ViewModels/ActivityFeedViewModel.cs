using System.Collections.ObjectModel;
using System.Diagnostics;
using CommunityToolkit.Maui.ApplicationModel;
using Ifpa.Models;
using Ifpa.Services;
using Microsoft.Extensions.Logging;
using PinballApi;

namespace Ifpa.ViewModels
{
    public class ActivityFeedViewModel : BaseViewModel
    {
        public ObservableCollection<ActivityFeedItem> ActivityFeedItems { get; set; }
        public Command LoadItemsCommand { get; set; }
        public Command MarkAllSeenCommand { get; set; }
        public Command MarkItemSeenCommand { get; set; }

        public Command TestingShimCommand { get; set; }

        public ActivityFeedItem SelectedItem { get; set; }

        private readonly NotificationService notificationService;

        public ActivityFeedViewModel(PinballRankingApiV2 pinballRankingApiV2, NotificationService notificationService, ILogger<ActivityFeedViewModel> logger) : base(pinballRankingApiV2, logger)
        {
            Title = "Activity Feed";
            ActivityFeedItems = new ObservableCollection<ActivityFeedItem>();
            LoadItemsCommand = new Command(async () => await ExecuteLoadItemsCommand());
            MarkAllSeenCommand = new Command(async () => await ExecuteMarkAllSeenCommand());
            MarkItemSeenCommand = new Command(async () => await ExecuteMarkItemSeenCommand());
            TestingShimCommand = new Command(async () => await ExecuteTestingShimCommand());

            this.notificationService = notificationService;
        }

        private async Task ExecuteMarkItemSeenCommand()
        {
            await notificationService.ClearNotificationForActivityFeedItem(SelectedItem);

            if (SelectedItem.ActivityType == ActivityFeedType.TournamentResult)
            {
                await Shell.Current.GoToAsync($"tournament-results?tournamentId={SelectedItem.RecordID.Value}");
            }
           
            SelectedItem = null;
            LoadItemsCommand.Execute(null);
        }

        private async Task ExecuteMarkAllSeenCommand()
        {
            foreach (var i in ActivityFeedItems.Where(n => !n.HasBeenSeen))
            {
                await notificationService.ClearNotificationForActivityFeedItem(i);
            }

            LoadItemsCommand.Execute(null);
        }

        async Task ExecuteLoadItemsCommand()
        {
            if (IsBusy)
                return;

            IsBusy = true;

            try
            {
                var activityFeedItems = await Settings.LocalDatabase.GetActivityFeedRecords();
                ActivityFeedItems.Clear();

                foreach (var item in activityFeedItems)
                {
                    ActivityFeedItems.Add(item);
                }

                logger.LogDebug("loaded {0} activity feed items", ActivityFeedItems.Count);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error loading activity feed");
            }
            finally
            {
                IsBusy = false;
            }
        }

        private async Task ExecuteTestingShimCommand()
        {
            await notificationService.TestingShim();
        }
    }
}