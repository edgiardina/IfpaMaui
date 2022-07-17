using Ifpa.Models;
using Ifpa.ViewModels;
//using Plugin.Badge;


namespace Ifpa.Views
{
    [XamlCompilation(XamlCompilationOptions.Compile)]
    public partial class ActivityFeedPage : ContentPage
    {
        private ActivityFeedViewModel activityFeedViewModel;

        public ActivityFeedPage(ActivityFeedViewModel viewModel)
        {
            InitializeComponent();

            BindingContext = activityFeedViewModel = viewModel;
        }

        protected override void OnAppearing()
        {
            base.OnAppearing();

            activityFeedViewModel.LoadItemsCommand.Execute(null);
        }

        private async void ActivityFeedListView_ItemSelected(object sender, SelectedItemChangedEventArgs e)
        {
            if (e.SelectedItem != null)
            {
                var item = (ActivityFeedItem)e.SelectedItem;
                item.HasBeenSeen = true;
                await Settings.LocalDatabase.UpdateActivityFeedRecord(item);                

                if (item.ActivityType == ActivityFeedType.TournamentResult)
                {
                    await Shell.Current.GoToAsync($"tournament-results?tournamentId={item.RecordID.Value}");
                }

                if (DeviceInfo.Current.Platform == DevicePlatform.iOS)
                {
                    var remainingUnreads = await Settings.LocalDatabase.GetUnreadActivityCount();

                    //TODO: update badge
                    //CrossBadge.Current.SetBadge(remainingUnreads);
                }

                ActivityFeedListView.SelectedItem = null;
                activityFeedViewModel.LoadItemsCommand.Execute(null);
            }
        }

        private async void ToolbarItem_Clicked(object sender, System.EventArgs e)
        {
            foreach (var i in activityFeedViewModel.ActivityFeedItems.Where(n => !n.HasBeenSeen))
            {
                i.HasBeenSeen = true;
                await Settings.LocalDatabase.UpdateActivityFeedRecord(i);
            }           
            
            if (DeviceInfo.Current.Platform == DevicePlatform.iOS)
            {
                //TODO: update badge
                //CrossBadge.Current.ClearBadge();
            }

            activityFeedViewModel.LoadItemsCommand.Execute(null);
        }

        //Testing shim. 
        private async void ToolbarItem_Clicked_1(object sender, System.EventArgs e)
        {
            var newItem = new ActivityFeedItem
            {
                ActivityType = ActivityFeedType.TournamentResult,
                RecordID = 28089,
                CreatedDateTime = DateTime.Now,
                HasBeenSeen = false
            };
            await Settings.LocalDatabase.CreateActivityFeedRecord(newItem);
            //TODO: update badge
            //CrossBadge.Current.SetBadge(1);
            activityFeedViewModel.LoadItemsCommand.Execute(null);
        }
    }
}
