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

        protected override async void OnAppearing()
        {
            base.OnAppearing();

            if (ViewModel.NewsItems.Count == 0)
            {                
                await Task.Run(() => ViewModel.LoadItemsCommand.Execute(null));
            }
        }
    }
}