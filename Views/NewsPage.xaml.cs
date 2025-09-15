using Ifpa.ViewModels;

namespace Ifpa.Views
{
    [XamlCompilation(XamlCompilationOptions.Compile)]
    public partial class NewsPage : ContentPage
    {
        NewsViewModel ViewModel;

        public NewsPage(NewsViewModel viewModel)
        {
            InitializeComponent();

            BindingContext = this.ViewModel = viewModel;
        }

        protected override void OnAppearing()
        {
            base.OnAppearing();

            if (ViewModel.NewsItems.Count == 0)
            {
                _ = Task.Run(ViewModel.ExecuteLoadItems);
            }
        }
    }
}