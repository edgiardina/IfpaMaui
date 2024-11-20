using CommunityToolkit.Mvvm.Input;
using Ifpa.Models;
using Ifpa.Services;
using Microsoft.Extensions.Logging;
using PinballApi;
using System.Collections.ObjectModel;

namespace Ifpa.ViewModels
{
    public partial class ActivityFeedViewModel : BaseViewModel
    {
        public ObservableCollection<ActivityFeedItem> ActivityFeedItems { get; set; }

        public Command TestingShimCommand { get; set; }

        public ActivityFeedItem SelectedItem { get; set; }

        private readonly NotificationService notificationService;

        public ActivityFeedViewModel(PinballRankingApiV2 pinballRankingApiV2, NotificationService notificationService, ILogger<ActivityFeedViewModel> logger) : base(pinballRankingApiV2, logger)
        {
            Title = "Activity Feed";
            ActivityFeedItems = new ObservableCollection<ActivityFeedItem>();

            this.notificationService = notificationService;
        }

        [RelayCommand]
        public async Task ExecuteMarkItemSeen()
        {
            await notificationService.ClearNotificationForActivityFeedItem(SelectedItem);

            if (SelectedItem.ActivityType == ActivityFeedType.TournamentResult)
            {
                await Shell.Current.GoToAsync($"tournament-results?tournamentId={SelectedItem.RecordID.Value}");
            }

            SelectedItem = null;
            await ExecuteLoadItems();
        }

        [RelayCommand]
        public async Task ExecuteMarkAllSeen()
        {
            foreach (var i in ActivityFeedItems.Where(n => !n.HasBeenSeen))
            {
                await notificationService.ClearNotificationForActivityFeedItem(i);
            }

            await ExecuteLoadItems();
        }

        [RelayCommand]
        public async Task ExecuteLoadItems()
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

        [RelayCommand]
        public async Task ExecuteTestingShim()
        {
            await notificationService.TestingShim();
        }
    }
}