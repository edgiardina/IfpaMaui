using System.Collections.ObjectModel;
using System.ServiceModel.Syndication;
using Ifpa.Services;
using System.Xml;
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
        private WebViewSource newsItemContent;

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
            Title = "Loading...";

            try
            {
                Comments.Clear();
                
                var cancellationToken = new CancellationTokenSource(TimeSpan.FromSeconds(30));
                var newsItemDetails = await BlogPostService.GetBlogPosts();
                logger.LogInformation($"GetBlogPosts() returned {newsItemDetails?.Count() ?? 0} items");

                NewsItem = newsItemDetails.Single(n => n.Links.Any(m => m.Uri == NewsItemUrl));        
                
                //TODO: make this HTML and CSS in discrete files 
                var articleContent = NewsItem.ElementExtensions
                      .FirstOrDefault(ext => ext.OuterName == "encoded")
                      .GetObject<XmlElement>().InnerText;

                string webviewTheme = (Application.Current.RequestedTheme == AppTheme.Dark) ? "dark-theme" : "light-theme";
           
                // Create improved HTML content with better CSS
                var htmlContent = @$"<!DOCTYPE html>
                                    <html>
                                    <head>
                                        <meta charset='utf-8'>
                                        <meta name='viewport' content='width=device-width, initial-scale=1.0'>
                                        <style>
                                            img {{
                                                display: block; 
                                                clear: both; 
                                                margin: auto; 
                                                margin-bottom: 10px !important;
                                                max-width: 100%;
                                                height: auto;
                                            }}
                                            body.light-theme {{
                                                background-color: #fcfffc; 
                                                color: #343434;
                                            }}
                                            body.dark-theme {{
                                                background-color: #333333; 
                                                color: #fcfffc;
                                            }}
                                            body {{
                                                font-family: -apple-system, BlinkMacSystemFont, 'Segoe UI', Roboto, sans-serif;
                                                margin: 16px;
                                                line-height: 1.6;
                                            }}
                                        </style>
                                    </head>
                                    <body class='{webviewTheme}'> 
                                        {articleContent} 
                                    </body>
                                    </html>";


                    logger.LogInformation("Setting WebView content");
                    NewsItemContent = new HtmlWebViewSource
                    {
                        Html = htmlContent,
                        BaseUrl = "https://www.ifpapinball.com/"
                    };
                    logger.LogInformation("WebView content set");
             
                // Set final title
                Title = NewsItem.Title.Text;

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
                // Ensure IsBusy update happens on main thread
                await MainThread.InvokeOnMainThreadAsync(() =>
                {
                    IsBusy = false;
                });
            }
        }
    }
}