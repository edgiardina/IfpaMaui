using System.Collections.ObjectModel;
using System.Diagnostics;
using CommunityToolkit.Maui.ApplicationModel;
using Ifpa.Models;
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

        public ActivityFeedItem SelectedItem { get; set; }

        public ActivityFeedViewModel(PinballRankingApiV2 pinballRankingApiV2, ILogger<ActivityFeedViewModel> logger) : base(pinballRankingApiV2, logger)
        {
            Title = "Activity Feed";
            ActivityFeedItems = new ObservableCollection<ActivityFeedItem>();
            LoadItemsCommand = new Command(async () => await ExecuteLoadItemsCommand());
            MarkAllSeenCommand = new Command(async () => await ExecuteMarkAllSeenCommand());
            MarkItemSeenCommand = new Command(async () => await ExecuteMarkItemSeenCommand());
        }

        private async Task ExecuteMarkItemSeenCommand()
        {
            SelectedItem.HasBeenSeen = true;
            await Settings.LocalDatabase.UpdateActivityFeedRecord(SelectedItem);

            if (SelectedItem.ActivityType == ActivityFeedType.TournamentResult)
            {
                await Shell.Current.GoToAsync($"tournament-results?tournamentId={SelectedItem.RecordID.Value}");
            }

            var remainingUnreads = await Settings.LocalDatabase.GetUnreadActivityCount();

            Badge.SetCount((uint)remainingUnreads);

            SelectedItem = null;
            LoadItemsCommand.Execute(null);
        }

        private async Task ExecuteMarkAllSeenCommand()
        {
            foreach (var i in ActivityFeedItems.Where(n => !n.HasBeenSeen))
            {
                i.HasBeenSeen = true;
                await Settings.LocalDatabase.UpdateActivityFeedRecord(i);
            }

            Badge.SetCount(0);

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


    }
}