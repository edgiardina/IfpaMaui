using CommunityToolkit.Maui.ApplicationModel;
using Ifpa.Models;
using Ifpa.ViewModels;
//using Plugin.Badge;


namespace Ifpa.Views
{
    [XamlCompilation(XamlCompilationOptions.Compile)]
    public partial class ActivityFeedPage : ContentPage
    {
        private ActivityFeedViewModel activityFeedViewModel;
        private readonly IBadge Badge;

        public ActivityFeedPage(ActivityFeedViewModel viewModel, IBadge badge)
        {
            InitializeComponent();

            BindingContext = activityFeedViewModel = viewModel;
            this.Badge = badge;
        }

        protected override void OnAppearing()
        {
            base.OnAppearing();

            activityFeedViewModel.LoadItemsCommand.Execute(null);
        }


        private async void ToolbarItem_Clicked(object sender, System.EventArgs e)
        {
            foreach (var i in activityFeedViewModel.ActivityFeedItems.Where(n => !n.HasBeenSeen))
            {
                i.HasBeenSeen = true;
                await Settings.LocalDatabase.UpdateActivityFeedRecord(i);
            }

            Badge.SetCount(0);

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

            Badge.SetCount(1);
            activityFeedViewModel.LoadItemsCommand.Execute(null);
        }

        private async void ActivityFeedListView_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (e.CurrentSelection.Count() > 0)
            {
                var item = (ActivityFeedItem)e.CurrentSelection.FirstOrDefault();
                item.HasBeenSeen = true;
                await Settings.LocalDatabase.UpdateActivityFeedRecord(item);

                if (item.ActivityType == ActivityFeedType.TournamentResult)
                {
                    await Shell.Current.GoToAsync($"tournament-results?tournamentId={item.RecordID.Value}");
                }

                var remainingUnreads = await Settings.LocalDatabase.GetUnreadActivityCount();

                Badge.SetCount((uint)remainingUnreads);

                ActivityFeedListView.SelectedItem = null;
                activityFeedViewModel.LoadItemsCommand.Execute(null);
            }
        }
    }
}
