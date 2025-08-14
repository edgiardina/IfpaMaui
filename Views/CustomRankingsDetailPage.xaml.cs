using Ifpa.ViewModels;

namespace Ifpa.Views
{
    [QueryProperty("ViewId", "viewId")]
    [XamlCompilation(XamlCompilationOptions.Compile)]
    public partial class CustomRankingsDetailPage : ContentPage
    {
        CustomRankingsDetailViewModel ViewModel;

        public int ViewId { get; set; }

        public CustomRankingsDetailPage(CustomRankingsDetailViewModel viewModel)
        {
            InitializeComponent();
            BindingContext = ViewModel = viewModel;
        }

        protected override void OnAppearing()
        {
            base.OnAppearing();

            ViewModel.ViewId = ViewId;

            if (ViewModel.ViewResults.Count == 0)
                _ = ViewModel.LoadItems();
        }
    }
}