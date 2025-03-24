using Ifpa.ViewModels;
using System.Linq;
using System.ServiceModel.Syndication;
using System.Threading.Tasks;
using Microsoft.Maui;


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

        private async void ItemsListView_ItemSelected(object sender, SelectedItemChangedEventArgs e)
        {
            var newsItem = e.SelectedItem as SyndicationItem;
            if (newsItem == null)
                return;

            ItemsListView.SelectedItem = null;

            await Shell.Current.GoToAsync($"news-detail?newsUri={System.Uri.EscapeDataString(newsItem.Links.FirstOrDefault().Uri.ToString())}");    
        }
    }
}