using System.Collections.ObjectModel;
using System.Diagnostics;
using Ifpa.Models;
using Microsoft.Extensions.Logging;
using PinballApi;

namespace Ifpa.ViewModels
{
    public class ActivityFeedViewModel : BaseViewModel
    {
        public ObservableCollection<ActivityFeedItem> ActivityFeedItems { get; set; }
        public Command LoadItemsCommand { get; set; }
                
        public ActivityFeedViewModel(PinballRankingApiV1 pinballRankingApiV1, PinballRankingApiV2 pinballRankingApiV2, ILogger<ActivityFeedViewModel> logger) : base(pinballRankingApiV2, logger)
        {
            Title = "Activity Feed";
            ActivityFeedItems = new ObservableCollection<ActivityFeedItem>();
            LoadItemsCommand = new Command(async () => await ExecuteLoadItemsCommand());
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

                foreach(var item in activityFeedItems)
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