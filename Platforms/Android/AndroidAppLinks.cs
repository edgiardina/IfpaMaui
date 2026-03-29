#nullable enable
using System;
using System.Threading.Tasks;
using Microsoft.Maui.Controls;
using Microsoft.Extensions.Logging;
using System.Collections.Generic;
using System.Linq;
using Android.Content;
using AndroidX.Core.Content.PM;
using AndroidX.Core.Graphics.Drawable;
using Android.Graphics;

namespace IfpaMaui.Platforms.Android
{
    /// <summary>
    /// Android implementation of IAppLinks for registering and managing app links using
    /// Android Shortcuts API to enable content searchability similar to iOS Spotlight.
    /// </summary>
    internal class AndroidAppLinks : IAppLinks
    {
        private readonly ILogger? _logger;
        private readonly List<IAppLinkEntry> _registeredLinks = new();
        private readonly Context? _context;

        public AndroidAppLinks()
        {
            // Try to get logger and context from service provider if available
            try
            {
                var services = Microsoft.Maui.Controls.Application.Current?.Handler?.MauiContext?.Services;
                if (services != null)
                {
                    _logger = Microsoft.Extensions.DependencyInjection.ServiceProviderServiceExtensions
                        .GetService<ILogger<AndroidAppLinks>>(services);
                }

                // Get Android context
                _context = Platform.CurrentActivity ?? global::Android.App.Application.Context;
            }
            catch
            {
                // Logger or context not available, continue with limited functionality
                _logger = null;
                _context = null;
            }
        }

        /// <summary>
        /// Deregisters a specific app link entry from Android shortcuts.
        /// </summary>
        /// <param name="appLink">The app link entry to deregister</param>
        public void DeregisterLink(IAppLinkEntry appLink)
        {
            if (appLink?.AppLinkUri == null)
            {
                _logger?.LogWarning("Attempted to deregister app link with null URI");
                return;
            }

            try
            {
                _logger?.LogDebug("Deregistering app link: {Uri}", appLink.AppLinkUri);
                
                // Remove from our tracking list
                _registeredLinks.RemoveAll(link => link.AppLinkUri?.ToString() == appLink.AppLinkUri.ToString());
                
                // Remove from Android shortcuts
                RemoveShortcut(appLink);
                
                _logger?.LogInformation("Successfully deregistered app link: {Uri}", appLink.AppLinkUri);
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Failed to deregister app link: {Uri}", appLink.AppLinkUri);
            }
        }

        /// <summary>
        /// Deregisters an app link by URI from Android shortcuts.
        /// </summary>
        /// <param name="uri">The URI to deregister</param>
        public void DeregisterLink(Uri uri)
        {
            if (uri == null)
            {
                _logger?.LogWarning("Attempted to deregister app link with null URI");
                return;
            }

            try
            {
                _logger?.LogDebug("Deregistering app link by URI: {Uri}", uri);
                
                // Find matching links
                var linksToRemove = _registeredLinks.Where(link => 
                    link.AppLinkUri?.ToString() == uri.ToString()).ToList();
                
                // Remove from tracking list
                var removed = _registeredLinks.RemoveAll(link => link.AppLinkUri?.ToString() == uri.ToString());
                
                if (removed > 0)
                {
                    // Remove shortcuts for each link
                    foreach (var link in linksToRemove)
                    {
                        RemoveShortcut(link);
                    }
                    
                    _logger?.LogInformation("Successfully deregistered {Count} app link(s) with URI: {Uri}", removed, uri);
                }
                else
                {
                    _logger?.LogInformation("No app links found to deregister with URI: {Uri}", uri);
                }
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Failed to deregister app link URI: {Uri}", uri);
            }
        }

        /// <summary>
        /// Registers an app link entry for Android shortcuts to enable searchability.
        /// Creates dynamic shortcuts that appear in launcher and system search.
        /// </summary>
        /// <param name="appLink">The app link entry to register</param>
        public async void RegisterLink(IAppLinkEntry appLink)
        {
            if (appLink?.AppLinkUri == null)
            {
                _logger?.LogWarning("Attempted to register app link with null URI");
                return;
            }

            if (_context == null)
            {
                _logger?.LogWarning("Android context not available, cannot create shortcuts");
                return;
            }

            try
            {
                _logger?.LogDebug("Registering app link: {Uri} - Title: {Title}", 
                    appLink.AppLinkUri, appLink.Title);

                // Add to our tracking list (prevent duplicates)
                var existingIndex = _registeredLinks.FindIndex(link => 
                    link.AppLinkUri?.ToString() == appLink.AppLinkUri.ToString());
                
                if (existingIndex >= 0)
                {
                    _registeredLinks[existingIndex] = appLink; // Update existing
                    _logger?.LogDebug("Updated existing app link: {Uri}", appLink.AppLinkUri);
                }
                else
                {
                    _registeredLinks.Add(appLink); // Add new
                    _logger?.LogDebug("Added new app link: {Uri}", appLink.AppLinkUri);
                }
                
                // Create Android shortcut for searchability
                await CreateShortcutAsync(appLink);
                
                _logger?.LogInformation("Successfully registered app link: {Uri} - Total links: {Count}", 
                    appLink.AppLinkUri, _registeredLinks.Count);
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Failed to register app link: {Uri}", appLink.AppLinkUri);
            }
        }

        /// <summary>
        /// Deregisters all app links from Android shortcuts.
        /// </summary>
        public void DeregisterAll()
        {
            try
            {
                var count = _registeredLinks.Count;
                _logger?.LogDebug("Deregistering all {Count} app links", count);
                
                if (_context != null)
                {
                    // Remove all dynamic shortcuts
                    ShortcutManagerCompat.RemoveAllDynamicShortcuts(_context);
                }
                
                _registeredLinks.Clear();
                
                _logger?.LogInformation("Successfully deregistered all {Count} app links", count);
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Failed to deregister all app links");
            }
        }

        /// <summary>
        /// Creates an Android dynamic shortcut for the app link entry.
        /// This makes the content searchable in launcher and system search.
        /// </summary>
        private async Task CreateShortcutAsync(IAppLinkEntry appLink)
        {
            if (_context == null || appLink.AppLinkUri == null)
                return;

            try
            {
                // Generate shortcut ID from URI
                var shortcutId = GenerateShortcutId(appLink.AppLinkUri);
                
                // Create intent to open the app link
                var intent = new Intent(Intent.ActionView, global::Android.Net.Uri.Parse(appLink.AppLinkUri.ToString()));
                intent.SetPackage(_context.PackageName);
                
                // Get content type for appropriate icon and category
                var contentType = GetContentType(appLink);
                var category = GetShortcutCategory(contentType);
                
                // Build shortcut
                var shortcutBuilder = new ShortcutInfoCompat.Builder(_context, shortcutId)
                    .SetShortLabel(appLink.Title ?? "IFPA Content")
                    .SetLongLabel(appLink.Description ?? appLink.Title ?? "IFPA App Content")
                    .SetIntent(intent);
                
                // Add category for better organization
                if (!string.IsNullOrEmpty(category))
                {
                    // Use Intent categories instead of capability bindings
                    intent.AddCategory(category);
                }
                
                // Set icon based on content type and thumbnail
                await SetShortcutIcon(shortcutBuilder, contentType, appLink);
                
                var shortcut = shortcutBuilder.Build();
                
                if (shortcut == null)
                {
                    _logger?.LogError("Failed to build shortcut for {Uri}", appLink.AppLinkUri);
                    return;
                }
                
                // Add shortcut to system
                ShortcutManagerCompat.PushDynamicShortcut(_context, shortcut);
                
                _logger?.LogDebug("Created Android shortcut for {Title} (ID: {Id})", 
                    appLink.Title, shortcutId);
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Failed to create Android shortcut for {Uri}", appLink.AppLinkUri);
            }
        }

        /// <summary>
        /// Removes an Android shortcut for the app link entry.
        /// </summary>
        private void RemoveShortcut(IAppLinkEntry appLink)
        {
            if (_context == null || appLink.AppLinkUri == null)
                return;

            try
            {
                var shortcutId = GenerateShortcutId(appLink.AppLinkUri);
                ShortcutManagerCompat.RemoveDynamicShortcuts(_context, new[] { shortcutId });
                
                _logger?.LogDebug("Removed Android shortcut for {Title} (ID: {Id})", 
                    appLink.Title, shortcutId);
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Failed to remove Android shortcut for {Uri}", appLink.AppLinkUri);
            }
        }

        /// <summary>
        /// Generates a consistent shortcut ID from the app link URI.
        /// </summary>
        private static string GenerateShortcutId(Uri uri)
        {
            // Create a hash-based ID that's consistent and valid
            var hash = uri.ToString().GetHashCode();
            return $"ifpa_link_{Math.Abs(hash)}";
        }

        /// <summary>
        /// Extracts content type from app link metadata.
        /// </summary>
        private static string GetContentType(IAppLinkEntry appLink)
        {
            if (appLink.KeyValues?.TryGetValue("contentType", out var contentType) == true)
                return contentType.ToLowerInvariant();
            
            // Try to infer from URI
            var uri = appLink.AppLinkUri?.ToString()?.ToLowerInvariant() ?? "";
            if (uri.Contains("player")) return "player";
            if (uri.Contains("tournament")) return "tournament";
            return "generic";
        }

        /// <summary>
        /// Gets appropriate shortcut category based on content type.
        /// </summary>
        private static string GetShortcutCategory(string contentType)
        {
            // Use simple category strings instead of constants that don't exist
            return contentType switch
            {
                "player" => "com.android.action.SEND", // People-related
                "tournament" => "com.android.action.VIEW", // Event-related
                _ => "android.intent.action.VIEW"
            };
        }

        /// <summary>
        /// Sets appropriate icon for the shortcut based on content type and available images.
        /// For players, attempts to use their profile photo; falls back to generic icons.
        /// 
        /// NOTE: Custom player images are currently not implemented due to platform complexity.
        /// MAUI's GetPlatformImageAsync returns Drawable on Android, which requires conversion
        /// to Bitmap. This can be added in future versions for enhanced user experience.
        /// </summary>
        private async Task SetShortcutIcon(ShortcutInfoCompat.Builder? builder, string contentType, IAppLinkEntry appLink)
        {
            if (builder == null || _context == null) return;
            
            try
            {
                // TODO: Future enhancement - implement custom player images
                // Currently using default icons due to Drawable->Bitmap conversion complexity
                if (appLink.Thumbnail != null)
                {
                    _logger?.LogDebug("Custom thumbnails available but not yet implemented on Android");
                    // await SetCustomIconFromThumbnail(builder, appLink.Thumbnail);
                    // return;
                }
                
                // Fall back to default icons based on content type
                var iconResource = contentType switch
                {
                    "player" => global::Android.Resource.Drawable.IcMenuShare, // Person icon alternative
                    "tournament" => global::Android.Resource.Drawable.IcMenuAgenda, // Event icon alternative
                    _ => global::Android.Resource.Drawable.IcMenuSearch // Generic search icon
                };
                
                if (_context != null)
                {
                    builder.SetIcon(IconCompat.CreateWithResource(_context, iconResource));
                    _logger?.LogDebug("Set default icon for {ContentType} shortcut", contentType);
                }
            }
            catch (Exception ex)
            {
                _logger?.LogWarning(ex, "Failed to set shortcut icon for content type: {ContentType}", contentType);
                // Continue without icon - shortcut will still work
            }
        }

        /// <summary>
        /// Get all currently registered app links (for debugging/monitoring)
        /// </summary>
        public IReadOnlyList<IAppLinkEntry> GetRegisteredLinks() => _registeredLinks.AsReadOnly();
    }
}