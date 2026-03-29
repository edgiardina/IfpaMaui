using Microsoft.Extensions.Logging;

namespace IfpaMaui.Platforms.Android
{
    /// <summary>
    /// Android no-op implementation of IAppLinks to prevent crashes.
    /// </summary>
    public class AndroidAppLinks : IAppLinks
    {
        private readonly ILogger<AndroidAppLinks> logger;

        public AndroidAppLinks(ILogger<AndroidAppLinks> logger = null)
        {
            this.logger = logger;
        }

        /// <summary>
        /// No-op implementation to prevent crashes when shared code calls RegisterLink().
        /// Real app indexing happens via web-based Android App Links (already implemented).
        /// </summary>
        public void RegisterLink(IAppLinkEntry appLink)
        {
            // No-op: Android app indexing already works through:
            // - Digital Asset Links verification (ifpapinball.com/.well-known/assetlinks.json) 
            // - MainActivity intent filters for ifpapinball.com URLs
            // - Google Search showing "Open in App" buttons for IFPA content
            
            logger?.LogTrace("RegisterLink called for: {Url} (no-op - using web-based indexing)", 
                appLink?.AppLinkUri?.ToString() ?? "null");
        }

        /// <summary>
        /// No-op implementation for consistency.
        /// </summary>
        public void DeregisterLink(IAppLinkEntry appLink) 
        {
            // No-op: Android App Links don't require explicit deregistration
        }

        /// <summary>
        /// No-op implementation for consistency.
        /// </summary>
        public void DeregisterLink(Uri uri) 
        {
            // No-op: Android App Links don't require explicit deregistration
        }

        /// <summary>
        /// No-op implementation for consistency.
        /// </summary>
        public void DeregisterAll() 
        {
            // No-op: Android App Links don't require explicit deregistration
        }
    }
}