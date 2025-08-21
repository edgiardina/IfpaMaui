using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Ifpa.Services;
using Microsoft.Extensions.Logging;
using System.Collections.ObjectModel;
using System.ServiceModel.Syndication;
using System.Web;
using System.Xml;

namespace Ifpa.ViewModels
{
    public partial class NewsViewModel : BaseViewModel
    {
        public ObservableCollection<SyndicationItem> NewsItems { get; set; }
        public Command LoadItemsCommand { get; set; }

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
            LoadItemsCommand = new Command(async () => await ExecuteLoadItemsCommand());
            
            // Subscribe to property changes to update IsDataReady
            PropertyChanged += (sender, e) =>
            {
                if (e.PropertyName == nameof(IsBusy) || e.PropertyName == nameof(IsLoaded))
                {
                    OnPropertyChanged(nameof(IsDataReady));
                }
            };
        }

        [RelayCommand]
        public async Task ItemTapped(SyndicationItem newsItem)
        {
            if (newsItem == null)
                return;

            SelectedNewsItem = null;

            await Shell.Current.GoToAsync($"news-detail?newsUri={System.Uri.EscapeDataString(newsItem.Links.FirstOrDefault().Uri.ToString())}");
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
                    
                    // Extract author from dc:creator element and add to Authors collection if not already present
                    ExtractAndSetAuthor(item);
                    
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