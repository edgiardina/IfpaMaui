using Ifpa.ViewModels;
using System;
using System.Collections;
using System.Threading.Tasks;
using Microsoft.Maui;


namespace Ifpa.Views
{
    [QueryProperty("NewsUri", "newsUri")]
    [XamlCompilation(XamlCompilationOptions.Compile)]
    public partial class NewsDetailPage : ContentPage
    {
        NewsDetailViewModel ViewModel;
        public string NewsUri { get; set; }

        public NewsDetailPage(NewsDetailViewModel viewModel)
        {
            InitializeComponent();

            BindingContext = this.ViewModel = viewModel;
        }

        protected override async void OnAppearing()
        {
            base.OnAppearing();

            if (ViewModel.NewsItem == null)
            {
                ViewModel.NewsItemUrl = new System.Uri(NewsUri);
                await Task.Run(() => ViewModel.LoadItemsCommand.Execute(null));
            }
        }

        private void TapGestureRecognizer_Tapped(object sender, System.EventArgs e)
        {
            if (ViewModel.CommentCounts > 0)
            {
                ItemsListView.IsVisible = !ItemsListView.IsVisible;
                ItemsListView.ScrollTo(((IList)ItemsListView.ItemsSource)[0], ScrollToPosition.Start, false);
            }
        }

        private void WebView_Navigating(object sender, WebNavigatingEventArgs e)
        {
            //open all links in a new browser window
            if (e.Url.StartsWith("http"))
            {
                try
                {
                    var uri = new Uri(e.Url);
                    Launcher.OpenAsync(uri);
                }
                catch (Exception)
                {
                }

                e.Cancel = true;
            }
        }
    }
}