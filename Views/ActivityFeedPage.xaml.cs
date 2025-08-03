using Ifpa.ViewModels;

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

            activityFeedViewModel.SelectedItem = null;
            activityFeedViewModel.ExecuteLoadItems();
        }
    }
}
