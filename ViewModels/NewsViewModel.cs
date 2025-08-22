using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Ifpa.Services;
using Microsoft.Extensions.Logging;
using System.Collections.ObjectModel;
using System.ServiceModel.Syndication;
using System.Web;

namespace Ifpa.ViewModels
{
    public partial class NewsViewModel : BaseViewModel
    {
        public ObservableCollection<SyndicationItem> NewsItems { get; set; } 

        [ObservableProperty]
        private SyndicationItem selectedNewsItem;

        [ObservableProperty]
        private bool isLoaded = false;

        /// <summary>
        /// Returns true when data should be visible (not busy and loaded)
        /// </summary>
        public bool IsDataReady => !IsBusy && IsLoaded;

        private BlogPostService BlogPostService { get; set; }

        public NewsViewModel(BlogPostService blogPostService, ILogger<NewsViewModel> logger) : base(logger)
        {
            BlogPostService = blogPostService;

            NewsItems = new ObservableCollection<SyndicationItem>();
        }

        [RelayCommand]
        public async Task ItemTapped()
        {
            await Shell.Current.GoToAsync($"news-detail?newsUri={Uri.EscapeDataString(SelectedNewsItem.Links.FirstOrDefault().Uri.ToString())}");
            SelectedNewsItem = null;
        }

        [RelayCommand]
        public async Task ExecuteLoadItems()
        {
            if (IsBusy)
                return;

            IsBusy = true;

            try
            {
                NewsItems.Clear();
                var newsItems = await BlogPostService.GetBlogPosts();

                foreach (var item in newsItems)
                {
                    item.Summary = new TextSyndicationContent(HttpUtility.HtmlDecode(item.Summary.Text));
                    NewsItems.Add(item);
                }
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error loading news");
            }
            finally
            {
                IsBusy = false;
                IsLoaded = true;
            }
        }
    }
}