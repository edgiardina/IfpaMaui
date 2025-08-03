using Ifpa.Models;
using Microsoft.Extensions.Configuration;
using System.ServiceModel.Syndication;
using System.Xml;

namespace Ifpa.Services
{
    public class BlogPostService
    {
        protected AppSettings AppSettings { get; set; }

        public BlogPostService(IConfiguration config)
        {
            AppSettings = config.GetRequiredSection("AppSettings").Get<AppSettings>();
        }

        public async Task<IEnumerable<SyndicationItem>> GetBlogPosts()
        {
            return await Parse(AppSettings.IfpaRssFeedUrl);
        }

        public async Task<IEnumerable<SyndicationItem>> GetCommentsForBlogPost(string blogPostId)
        {
            var blogPosts = await Parse(AppSettings.IfpaRssFeedUrl);
            var post = blogPosts.Single(n => n.Id == blogPostId);
            var link = post.Links.FirstOrDefault().Uri.ToString();

            return await Parse(link + "/feed");
        }

        public int ParseBlogPostIdFromInternalIdUrl(string internalIdUrl)
        {
            //parse url and return integer p value from the following url style
            //https://www.ifpapinball.com/?p=12345
            return int.Parse(internalIdUrl.Split('=')[1]);
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
