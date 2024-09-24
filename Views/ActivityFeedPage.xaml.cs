using CommunityToolkit.Maui.ApplicationModel;
using Ifpa.Models;
using Ifpa.Services;
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

        //Testing shim. 
        //private async void ToolbarItem_Clicked_1(object sender, System.EventArgs e)
        //{
        //    var newItem = new ActivityFeedItem
        //    {
        //        ActivityType = ActivityFeedType.TournamentResult,
        //        RecordID = 28089,
        //        CreatedDateTime = DateTime.Now,
        //        HasBeenSeen = false
        //    };
        //    await Settings.LocalDatabase.CreateActivityFeedRecord(newItem);

        //    Badge.SetCount(1);
        //    activityFeedViewModel.LoadItemsCommand.Execute(null);
        //}
    }
}
