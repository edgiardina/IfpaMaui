using System.Collections.ObjectModel;
using System.Diagnostics;
using Ifpa.Services;
using System.ServiceModel.Syndication;
using System.Web;
using Microsoft.Extensions.Configuration;
using PinballApi;

namespace Ifpa.ViewModels
{
    public class NewsViewModel : BaseViewModel
    {
        public ObservableCollection<SyndicationItem> NewsItems { get; set; }
        public Command LoadItemsCommand { get; set; }

        private BlogPostService BlogPostService { get; set; }

        public NewsViewModel(BlogPostService blogPostService, PinballRankingApiV1 pinballRankingApiV1, PinballRankingApiV2 pinballRankingApiV2) : base(pinballRankingApiV1, pinballRankingApiV2)
        {
            BlogPostService = blogPostService;

            Title = "News";
            NewsItems = new ObservableCollection<SyndicationItem>();
            LoadItemsCommand = new Command(async () => await ExecuteLoadItemsCommand());
        }

        async Task ExecuteLoadItemsCommand()
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
                Debug.WriteLine(ex);
            }
            finally
            {
                IsBusy = false;
            }
        }


    }
}