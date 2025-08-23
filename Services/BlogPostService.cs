using Ifpa.Models;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System.Net.Http.Headers;
using System.ServiceModel.Syndication;
using System.Xml;

namespace Ifpa.Services
{
    public class BlogPostService
    {
        private readonly AppSettings _appSettings;
        private readonly ILogger<BlogPostService> _logger;
        private readonly HttpClient _http;

        public BlogPostService(IConfiguration config, ILogger<BlogPostService> logger, HttpClient http)
        {
            _appSettings = config.GetRequiredSection("AppSettings").Get<AppSettings>();
            _logger = logger;
            _http = http;
        }

        public async Task<IReadOnlyList<SyndicationItem>> GetBlogPosts()
        {
            var items = await Parse(_appSettings.IfpaRssFeedUrl).ConfigureAwait(false);
            foreach (var item in items) ExtractAndSetAuthor(item);
            return items;
        }

        public async Task<IReadOnlyList<SyndicationItem>> GetCommentsForBlogPost(string blogPostId)
        {
            var posts = await Parse(_appSettings.IfpaRssFeedUrl).ConfigureAwait(false);
            var post = posts.Single(n => n.Id == blogPostId);
            var link = post.Links.FirstOrDefault()?.Uri?.ToString();
            if (string.IsNullOrEmpty(link)) return Array.Empty<SyndicationItem>();

            var comments = await Parse(link + "/feed").ConfigureAwait(false);
            foreach (var c in comments) ExtractAndSetAuthor(c);
            return comments;
        }

        public int ParseBlogPostIdFromInternalIdUrl(string internalIdUrl) =>
            int.Parse(internalIdUrl.Split('=')[1]);

        public void ExtractAndSetAuthor(SyndicationItem item)
        {
            try
            {
                var creatorElement = item.ElementExtensions.FirstOrDefault(ext =>
                    ext.OuterName == "creator" &&
                    (ext.OuterNamespace == "http://purl.org/dc/elements/1.1/" || ext.OuterNamespace == ""));

                if (creatorElement != null)
                {
                    var creatorName = creatorElement.GetObject<XmlElement>()?.InnerText;
                    if (!string.IsNullOrWhiteSpace(creatorName))
                    {
                        item.Authors.Clear();
                        item.Authors.Add(new SyndicationPerson { Name = creatorName });
                    }
                }

                if (!item.Authors.Any())
                    item.Authors.Add(new SyndicationPerson { Name = "IFPA" });
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Error extracting author from RSS item");
                if (!item.Authors.Any())
                    item.Authors.Add(new SyndicationPerson { Name = "IFPA" });
            }
        }

        private async Task<IReadOnlyList<SyndicationItem>> Parse(string url)
        {
            try
            {
                var sep = url.Contains('?') ? "&" : "?";
                var urlWithTs = $"{url}{sep}_t={DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()}";

                using var req = new HttpRequestMessage(HttpMethod.Get, urlWithTs);
                req.Headers.CacheControl = new CacheControlHeaderValue
                {
                    NoCache = true,
                    NoStore = true,
                    MaxAge = TimeSpan.Zero,
                    MustRevalidate = true
                };
                req.Headers.Pragma.ParseAdd("no-cache");

                using var resp = await _http.SendAsync(
                    req,
                    HttpCompletionOption.ResponseHeadersRead
                ).ConfigureAwait(false);

                resp.EnsureSuccessStatusCode();

                await using var stream = await resp.Content.ReadAsStreamAsync().ConfigureAwait(false);
                var xmlSettings = new XmlReaderSettings { Async = true, DtdProcessing = DtdProcessing.Ignore };

                using var reader = XmlReader.Create(stream, xmlSettings);
                var feed = SyndicationFeed.Load(reader);
                return (feed?.Items?.ToList() ?? new List<SyndicationItem>());
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to parse RSS from {Url}", url);
                return Array.Empty<SyndicationItem>();
            }
        }
    }
}
