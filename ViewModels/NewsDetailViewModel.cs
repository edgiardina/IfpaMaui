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

        //TODO: convert IConfiguration to IOptions?
        public NewsDetailViewModel(BlogPostService blogPostService, ILogger<NewsDetailViewModel> logger) : base(logger)
        {
            Title = "News";
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

                // Extract author from dc:creator element for the main news item
                ExtractAndSetAuthor(NewsItem);

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
                
                // Extract authors for comments as well
                foreach (var comment in comments)
                {
                    ExtractAndSetAuthor(comment);
                }
                
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

        /// <summary>
        /// Extracts author information from dc:creator element and ensures it's available in the Authors collection
        /// </summary>
        private void ExtractAndSetAuthor(SyndicationItem item)
        {
            try
            {
                // Try to extract dc:creator from extension elements
                var creatorElement = item.ElementExtensions
                    .FirstOrDefault(ext => ext.OuterName == "creator" && 
                                          (ext.OuterNamespace == "http://purl.org/dc/elements/1.1/" || 
                                           ext.OuterNamespace == ""));

                if (creatorElement != null)
                {
                    var creatorName = creatorElement.GetObject<XmlElement>()?.InnerText;
                    
                    if (!string.IsNullOrEmpty(creatorName))
                    {
                        // Clear existing authors and add the creator
                        item.Authors.Clear();
                        item.Authors.Add(new SyndicationPerson { Name = creatorName });
                    }
                }
                
                // Fallback: if no authors and no dc:creator found, add a default
                if (!item.Authors.Any())
                {
                    item.Authors.Add(new SyndicationPerson { Name = "IFPA" });
                }
            }
            catch (Exception ex)
            {
                logger.LogWarning(ex, "Error extracting author from RSS item");
                // Ensure there's always at least a default author
                if (!item.Authors.Any())
                {
                    item.Authors.Add(new SyndicationPerson { Name = "IFPA" });
                }
            }
        }
    }
}