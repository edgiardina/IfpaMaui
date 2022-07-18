using Ifpa.ViewModels;
using PinballApi.Models.WPPR.v2.Rankings;

namespace Ifpa.Views
{
    [XamlCompilation(XamlCompilationOptions.Compile)]
    public partial class CustomRankingsPage : ContentPage
    {
        CustomRankingsViewModel ViewModel;

        public CustomRankingsPage(CustomRankingsViewModel viewModel)
        {
            InitializeComponent();

            BindingContext = ViewModel = viewModel;
        }

        async void OnItemSelected(object sender, SelectedItemChangedEventArgs args)
        {
            var view = args.SelectedItem as CustomRankingView;
            if (view == null)
                return;

            await Shell.Current.GoToAsync($"custom-ranking-details?viewId={view.ViewId}");

            // Manually deselect item.
            RankingsListView.SelectedItem = null;
        }

        protected override void OnAppearing()
        {
            if(ViewModel.CustomRankings.Count == 0)
                ViewModel.LoadItemsCommand.Execute(null);

            base.OnAppearing();
        }
    }
}