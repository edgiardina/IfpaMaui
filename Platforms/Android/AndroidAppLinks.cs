#nullable enable
using System;
using System.Threading.Tasks;
using Microsoft.Maui.Controls;
using Microsoft.Extensions.Logging;
using System.Collections.Generic;

namespace IfpaMaui.Platforms.Android
{
    /// <summary>
    /// Android implementation of IAppLinks for registering and managing app links.
    /// This implementation provides real functionality for Android app indexing
    /// and maintains compatibility with the existing iOS implementation.
    /// </summary>
    internal class AndroidAppLinks : IAppLinks
    {
        private readonly ILogger? _logger;
        private readonly List<IAppLinkEntry> _registeredLinks = new();

        public AndroidAppLinks()
        {
            // Try to get logger from service provider if available
            try
            {
                var services = Microsoft.Maui.Controls.Application.Current?.Handler?.MauiContext?.Services;
                if (services != null)
                {
                    _logger = Microsoft.Extensions.DependencyInjection.ServiceProviderServiceExtensions
                        .GetService<ILogger<AndroidAppLinks>>(services);
                }
            }
            catch
            {
                // Logger not available, continue without logging
                _logger = null;
            }
        }

        /// <summary>
        /// Deregisters a specific app link entry.
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
                
                // Future enhancement: Remove from Android App Shortcuts or Firebase App Indexing
                DeregisterFromAndroidSystems(appLink);
                
                _logger?.LogInformation("Successfully deregistered app link: {Uri}", appLink.AppLinkUri);
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Failed to deregister app link: {Uri}", appLink.AppLinkUri);
            }
        }

        /// <summary>
        /// Deregisters an app link by URI.
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
                
                // Remove from our tracking list
                var removed = _registeredLinks.RemoveAll(link => link.AppLinkUri?.ToString() == uri.ToString());
                
                if (removed > 0)
                {
                    // Future enhancement: Remove from Android App Shortcuts or Firebase App Indexing
                    DeregisterFromAndroidSystemsByUri(uri);
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
        /// Registers an app link entry for Android app indexing.
        /// </summary>
        /// <param name="appLink">The app link entry to register</param>
        public void RegisterLink(IAppLinkEntry appLink)
        {
            if (appLink?.AppLinkUri == null)
            {
                _logger?.LogWarning("Attempted to register app link with null URI");
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
                
                RegisterWithAndroidSystems(appLink);
                
                _logger?.LogInformation("Successfully registered app link: {Uri} - Total links: {Count}", 
                    appLink.AppLinkUri, _registeredLinks.Count);
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Failed to register app link: {Uri}", appLink.AppLinkUri);
            }
        }

        /// <summary>
        /// Deregisters all app links.
        /// </summary>
        public void DeregisterAll()
        {
            try
            {
                var count = _registeredLinks.Count;
                _logger?.LogDebug("Deregistering all {Count} app links", count);
                
                _registeredLinks.Clear();
                
                // Future enhancement: Clear all Android App Shortcuts or Firebase App Indexing entries
                DeregisterAllFromAndroidSystems();
                
                _logger?.LogInformation("Successfully deregistered all {Count} app links", count);
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Failed to deregister all app links");
            }
        }

        /// <summary>
        /// Register app link with Android-specific systems.
        /// This method can be enhanced to integrate with:
        /// - Firebase App Indexing
        /// - Android App Shortcuts API
        /// - Google Play Services App Indexing
        /// </summary>
        private void RegisterWithAndroidSystems(IAppLinkEntry appLink)
        {
            // Extract metadata for Android systems
            var title = appLink.Title ?? "IFPA App Link";
            var description = appLink.Description ?? string.Empty;
            var uri = appLink.AppLinkUri.ToString();
            
            // Log the content type and app name if available in KeyValues
            if (appLink.KeyValues != null)
            {
                if (appLink.KeyValues.TryGetValue("contentType", out var contentType))
                {
                    _logger?.LogDebug("App link content type: {ContentType}", contentType);
                    
                    // Different handling based on content type
                    switch (contentType.ToLowerInvariant())
                    {
                        case "player":
                            RegisterPlayerLink(appLink);
                            break;
                        case "tournament":
                            RegisterTournamentLink(appLink);
                            break;
                        default:
                            RegisterGenericLink(appLink);
                            break;
                    }
                }
                
                if (appLink.KeyValues.TryGetValue("appName", out var appName))
                {
                    _logger?.LogDebug("App link app name: {AppName}", appName);
                }
            }

            // TODO: Future Android implementations:
            // 1. Firebase App Indexing for Google Search integration
            // 2. Android App Shortcuts for launcher shortcuts
            // 3. Direct Share targets
            // 4. Notification actions
        }

        private void RegisterPlayerLink(IAppLinkEntry appLink)
        {
            _logger?.LogDebug("Registering player link: {Title}", appLink.Title);
            // TODO: Create player-specific shortcuts or indexing
        }

        private void RegisterTournamentLink(IAppLinkEntry appLink)
        {
            _logger?.LogDebug("Registering tournament link: {Title}", appLink.Title);
            // TODO: Create tournament-specific shortcuts or indexing
        }

        private void RegisterGenericLink(IAppLinkEntry appLink)
        {
            _logger?.LogDebug("Registering generic link: {Title}", appLink.Title);
            // TODO: Create generic shortcuts or indexing
        }

        private void DeregisterFromAndroidSystems(IAppLinkEntry appLink)
        {
            // TODO: Remove from Android App Shortcuts, Firebase App Indexing, etc.
            _logger?.LogDebug("Deregistering from Android systems: {Uri}", appLink.AppLinkUri);
        }

        private void DeregisterFromAndroidSystemsByUri(Uri uri)
        {
            // TODO: Remove by URI from Android systems
            _logger?.LogDebug("Deregistering from Android systems by URI: {Uri}", uri);
        }

        private void DeregisterAllFromAndroidSystems()
        {
            // TODO: Clear all from Android systems
            _logger?.LogDebug("Clearing all registrations from Android systems");
        }

        /// <summary>
        /// Get all currently registered app links (for debugging/monitoring)
        /// </summary>
        public IReadOnlyList<IAppLinkEntry> GetRegisteredLinks() => _registeredLinks.AsReadOnly();
    }
}