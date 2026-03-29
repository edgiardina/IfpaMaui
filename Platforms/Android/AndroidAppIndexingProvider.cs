using Microsoft.Extensions.Logging;

namespace IfpaMaui.Platforms.Android
{
    /// <summary>
    /// Android no-op implementation of IAppIndexingProvider to fix dependency injection.
    /// 
    /// CONTEXT: iOS had IOSAppIndexingProvider but Android was missing equivalent.
    /// Shared MAUI code calls Application.Current.AppLinks but Android had no provider,
    /// causing NullReferenceException crashes.
    /// </summary>
    public class AndroidAppIndexingProvider : IAppIndexingProvider
    {
        private readonly ILogger<AndroidAppLinks> logger;

        public AndroidAppIndexingProvider(ILogger<AndroidAppLinks> logger = null)
        {
            this.logger = logger;
        }

        /// <summary>
        /// Returns no-op AndroidAppLinks instance to prevent crashes.
        /// </summary>
        public IAppLinks AppLinks => new AndroidAppLinks(logger);
    }
}