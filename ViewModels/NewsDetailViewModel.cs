using System.Collections.ObjectModel;
using System.ServiceModel.Syndication;
using Ifpa.Services;
using System.Xml;
using PinballApi;
using Microsoft.Extensions.Logging;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Maui.Core.Extensions;
using CommunityToolkit.Mvvm.Input;

namespace Ifpa.ViewModels
{
    public partial class NewsDetailViewModel : BaseViewModel
    {
        [ObservableProperty]
        private SyndicationItem newsItem;

        [ObservableProperty]
        private ObservableCollection<SyndicationItem> comments = new ObservableCollection<SyndicationItem>();

        [ObservableProperty]
        private int commentCounts;

        public Uri NewsItemUrl { get; set; }

        [ObservableProperty]
        private HtmlWebViewSource newsItemContent;

        private BlogPostService BlogPostService { get; set; }

        public NewsDetailViewModel(BlogPostService blogPostService, ILogger<NewsDetailViewModel> logger) : base(logger)
        {
            BlogPostService = blogPostService;
        }

        [RelayCommand]
        public async Task LoadItems()
        {
            if (IsBusy)
                return;

            IsBusy = true;

            try
            {
                Comments.Clear();
                var newsItemDetails = await BlogPostService.GetBlogPosts();

                NewsItem = newsItemDetails.Single(n => n.Links.Any(m => m.Uri == NewsItemUrl));

                Title = NewsItem.Title.Text;
                //TODO: make this HTML and CSS in discrete files 
                var articleContent = NewsItem.ElementExtensions
                      .FirstOrDefault(ext => ext.OuterName == "encoded")
                      .GetObject<XmlElement>().InnerText;

                string webviewTheme = (Application.Current.RequestedTheme == AppTheme.Dark) ? "dark-theme" : "light-theme";

                await MainThread.InvokeOnMainThreadAsync(() =>
                {
                    NewsItemContent = new HtmlWebViewSource();
                    NewsItemContent.Html = @$"<html>
                                               <head>
                                                  <style>img {{display:block; clear:both;  margin: auto; margin-botton:10px !important;}}</style>
                                                  <style>
                                                    body.light-theme{{background-color: #fcfffc; color: #343434;}}
                                                    body.dark-theme {{background-color: #333333; color: #fcfffc;}}
                                                  </style>
                                               </head>
                                               <body style='font-family:sans-serif;'> 
                                                    {articleContent} 
                                                    <script type=""text/javascript"">
                                                    document.body.classList.add(""{webviewTheme}"");
                                                    </script>
                                               </body>
                                            </html>";
                });

                var comments = await BlogPostService.GetCommentsForBlogPost(NewsItem.Id);
                Comments = comments.ToObservableCollection();
                CommentCounts = Comments.Count;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error loading news item {0}", NewsItemUrl);
            }
            finally
            {
                IsBusy = false;
            }
        }
    }
}