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
        /// </summary>
        private async Task SetShortcutIcon(ShortcutInfoCompat.Builder? builder, string contentType, IAppLinkEntry appLink)
        {
            if (builder == null || _context == null) return;
            
            try
            {
                // Try to use custom image from app link thumbnail
                if (appLink.Thumbnail != null)
                {
                    var customIconSet = await SetCustomIconFromThumbnail(builder, appLink.Thumbnail);
                    if (customIconSet)
                    {
                        _logger?.LogDebug("Successfully set custom thumbnail icon for {ContentType}", contentType);
                        return;
                    }
                    _logger?.LogDebug("Failed to set custom icon, falling back to default for {ContentType}", contentType);
                }
                
                // Fall back to default icons based on content type
                var iconResource = contentType switch
                {
                    "player" => global::Android.Resource.Drawable.IcMenuShare, // Person icon alternative
                    "tournament" => global::Android.Resource.Drawable.IcMenuAgenda, // Event icon alternative
                    _ => global::Android.Resource.Drawable.IcMenuSearch // Generic search icon
                };
                
                builder.SetIcon(IconCompat.CreateWithResource(_context, iconResource));
                _logger?.LogDebug("Set default icon for {ContentType} shortcut", contentType);
            }
            catch (Exception ex)
            {
                _logger?.LogWarning(ex, "Failed to set shortcut icon for content type: {ContentType}", contentType);
                // Continue without icon - shortcut will still work
            }
        }

        /// <summary>
        /// Sets a custom icon from the app link thumbnail (player image, etc.)
        /// </summary>
        private async Task<bool> SetCustomIconFromThumbnail(ShortcutInfoCompat.Builder builder, Microsoft.Maui.IImageSource imageSource)
        {
            try
            {
                var mauiContext = Microsoft.Maui.Controls.Application.Current?.Handler?.MauiContext;
                if (mauiContext == null)
                {
                    _logger?.LogWarning("MauiContext not available for custom icon");
                    return false;
                }

                // Get platform-specific image
                var result = await imageSource.GetPlatformImageAsync(mauiContext);
                if (result?.Value == null)
                {
                    _logger?.LogDebug("GetPlatformImageAsync returned null");
                    return false;
                }

                // Process the image immediately and then dispose
                try 
                {
                    _logger?.LogDebug("Platform image result type: {Type}", result.Value.GetType().Name);
                    
                    // Convert platform image result to bitmap
                    // On Android, GetPlatformImageAsync typically returns a Drawable
                    global::Android.Graphics.Bitmap? bitmap = null;
                    
                    var drawable = result.Value as global::Android.Graphics.Drawables.Drawable;
                    if (drawable != null)
                    {
                        bitmap = ConvertDrawableToBitmap(drawable);
                        _logger?.LogDebug("Successfully converted drawable to bitmap");
                    }
                    else
                    {
                        _logger?.LogDebug("Platform image result is not a drawable");
                        return false;
                    }
                    
                    if (bitmap != null)
                    {
                        // Create circular bitmap for better appearance in shortcuts
                        var circularBitmap = CreateCircularBitmap(bitmap);
                        
                        // Always dispose the converted bitmap since we created it
                        bitmap.Dispose();
                        
                        if (circularBitmap != null)
                        {
                            using (circularBitmap)
                            {
                                builder.SetIcon(IconCompat.CreateWithBitmap(circularBitmap));
                                _logger?.LogDebug("Set custom thumbnail icon for shortcut");
                                return true;
                            }
                        }
                    }
                    
                    _logger?.LogDebug("Could not process image for custom icon");
                    return false;
                }
                finally
                {
                    // Dispose the result on the main thread to avoid threading issues
                    if (result != null)
                    {
                        try
                        {
                            // Post disposal to main thread to avoid IllegalStateException
                            if (global::Android.OS.Looper.MainLooper == global::Android.OS.Looper.MyLooper())
                            {
                                // We're already on main thread, dispose directly
                                result.Dispose();
                            }
                            else
                            {
                                // Post to main thread using Handler
                                var handler = new global::Android.OS.Handler(global::Android.OS.Looper.MainLooper!);
                                handler.Post(() => 
                                {
                                    try
                                    {
                                        result.Dispose();
                                    }
                                    catch (Exception ex)
                                    {
                                        _logger?.LogWarning(ex, "Error disposing image result on main thread");
                                    }
                                });
                            }
                        }
                        catch (Exception ex)
                        {
                            _logger?.LogWarning(ex, "Error handling image result disposal");
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Failed to set custom icon from thumbnail");
                return false;
            }
        }

        /// <summary>
        /// Converts an Android Drawable to a Bitmap
        /// </summary>
        private global::Android.Graphics.Bitmap? ConvertDrawableToBitmap(global::Android.Graphics.Drawables.Drawable drawable)
        {
            try
            {
                if (drawable is global::Android.Graphics.Drawables.BitmapDrawable bitmapDrawable)
                {
                    return bitmapDrawable.Bitmap;
                }
                
                // For other drawable types, create a bitmap and draw the drawable onto it
                int width = drawable.IntrinsicWidth > 0 ? drawable.IntrinsicWidth : 96;
                int height = drawable.IntrinsicHeight > 0 ? drawable.IntrinsicHeight : 96;
                
                var bitmap = global::Android.Graphics.Bitmap.CreateBitmap(width, height, 
                    global::Android.Graphics.Bitmap.Config.Argb8888!);
                
                if (bitmap == null) return null;
                
                using var canvas = new global::Android.Graphics.Canvas(bitmap);
                drawable.SetBounds(0, 0, canvas.Width, canvas.Height);
                drawable.Draw(canvas);
                
                return bitmap;
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Failed to convert drawable to bitmap");
                return null;
            }
        }

        /// <summary>
        /// Creates a circular bitmap from the original bitmap for better shortcut appearance.
        /// </summary>
        private global::Android.Graphics.Bitmap? CreateCircularBitmap(global::Android.Graphics.Bitmap originalBitmap)
        {
            try
            {
                const int targetSize = 96; // Standard shortcut icon size in dp
                
                // Scale bitmap to target size
                var scaledBitmap = global::Android.Graphics.Bitmap.CreateScaledBitmap(
                    originalBitmap, targetSize, targetSize, true);
                
                // Create circular bitmap
                var circularBitmap = global::Android.Graphics.Bitmap.CreateBitmap(
                    targetSize, targetSize, global::Android.Graphics.Bitmap.Config.Argb8888!);
                
                if (circularBitmap == null) return null;
                
                using var canvas = new global::Android.Graphics.Canvas(circularBitmap);
                using var paint = new global::Android.Graphics.Paint();
                using var rect = new global::Android.Graphics.Rect(0, 0, targetSize, targetSize);
                
                paint.AntiAlias = true;
                canvas.DrawARGB(0, 0, 0, 0);
                paint.Color = global::Android.Graphics.Color.White;
                
                // Draw circle
                canvas.DrawCircle(targetSize / 2f, targetSize / 2f, targetSize / 2f, paint);
                
                // Set blend mode to show image inside circle
                paint.SetXfermode(new global::Android.Graphics.PorterDuffXfermode(global::Android.Graphics.PorterDuff.Mode.SrcIn!));
                canvas.DrawBitmap(scaledBitmap, rect, rect, paint);
                
                scaledBitmap?.Dispose();
                return circularBitmap;
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Failed to create circular bitmap");
                return null;
            }
        }

        /// <summary>
        /// Get all currently registered app links (for debugging/monitoring)
        /// </summary>
        public IReadOnlyList<IAppLinkEntry> GetRegisteredLinks() => _registeredLinks.AsReadOnly();
    }
}