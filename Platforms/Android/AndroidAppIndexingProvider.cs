using Microsoft.Maui.Controls;

namespace IfpaMaui.Platforms.Android
{
    /// <summary>
    /// Android implementation of IAppIndexingProvider.
    /// Provides the AndroidAppLinks instance for app link management on Android platform.
    /// </summary>
    public class AndroidAppIndexingProvider : IAppIndexingProvider
    {
        /// <summary>
        /// Gets the Android-specific IAppLinks implementation.
        /// </summary>
        public IAppLinks AppLinks => new AndroidAppLinks();
    }
}