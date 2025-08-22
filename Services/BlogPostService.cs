using Ifpa.Models;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System.ServiceModel.Syndication;
using System.Xml;

namespace Ifpa.Services
{
    public class BlogPostService
    {
        protected AppSettings AppSettings { get; set; }
        private readonly ILogger<BlogPostService> logger;

        public BlogPostService(IConfiguration config, ILogger<BlogPostService> logger)
        {
            AppSettings = config.GetRequiredSection("AppSettings").Get<AppSettings>();
            this.logger = logger;
        }

        public async Task<IEnumerable<SyndicationItem>> GetBlogPosts()
        {
            var items = await Parse(AppSettings.IfpaRssFeedUrl);
            
            // Extract authors for all items
            foreach (var item in items)
            {
                ExtractAndSetAuthor(item);
            }
            
            return items;
        }

        public async Task<IEnumerable<SyndicationItem>> GetCommentsForBlogPost(string blogPostId)
        {
            var blogPosts = await Parse(AppSettings.IfpaRssFeedUrl);
            var post = blogPosts.Single(n => n.Id == blogPostId);
            var link = post.Links.FirstOrDefault().Uri.ToString();

            var comments = await Parse(link + "/feed");
            
            // Extract authors for all comments
            foreach (var comment in comments)
            {
                ExtractAndSetAuthor(comment);
            }
            
            return comments;
        }

        public int ParseBlogPostIdFromInternalIdUrl(string internalIdUrl)
        {
            //parse url and return integer p value from the following url style
            //https://www.ifpapinball.com/?p=12345
            return int.Parse(internalIdUrl.Split('=')[1]);
        }

        /// <summary>
        /// Extracts author information from dc:creator element and ensures it's available in the Authors collection
        /// </summary>
        public void ExtractAndSetAuthor(SyndicationItem item)
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

        private async Task<IEnumerable<SyndicationItem>> Parse(string url)
        {
            Stream stream = null;

            using (var client = new HttpClient())
            {
                stream = await client.GetStreamAsync(url);
            }
            
            if (stream == null) return new List<SyndicationItem>();

            XmlReader reader = XmlReader.Create(stream);
            SyndicationFeed feed = SyndicationFeed.Load(reader);
     
            return feed.Items;
        }
    }
}
