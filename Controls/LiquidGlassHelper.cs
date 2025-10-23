using Microsoft.Maui.Controls;

namespace Ifpa.Controls;

/// <summary>
/// Helper class for applying iOS liquid glass behavior to layout controls.
/// Provides consistent IgnoreSafeArea and navigation bar padding logic.
/// </summary>
public static class LiquidGlassHelper
{
    /// <summary>
    /// Applies liquid glass behavior to a layout control on iOS 26+.
    /// Sets IgnoreSafeArea=true and adds appropriate margin to enable iOS Large Title behavior.
    /// </summary>
    /// <param name="layout">The layout control to apply liquid glass behavior to</param>
    public static void ApplyLiquidGlassBehavior(Layout layout)
    {
#if IOS
        // Only apply on iOS for liquid glass effect
        if (DeviceInfo.Version.Major >= 26)
        {
            // Set IgnoreSafeArea to true to allow content to flow under the navigation bar
            layout.IgnoreSafeArea = true;
            
            // Instead of padding, use margin on the first child to create the Large Title effect
            // This allows scrolling content to flow under the nav bar while keeping initial content visible
            if (layout.Children.Count > 0 && layout.Children[0] is View firstChild)
            {
                var topMargin = GetNavigationBarHeight();
                var currentMargin = firstChild.Margin;
                
                // Set top margin to push first content below navigation bar initially
                firstChild.Margin = new Thickness(
                    currentMargin.Left,
                    Math.Max(currentMargin.Top, topMargin),
                    currentMargin.Right,
                    currentMargin.Bottom
                );
            }
        }
#endif
    }

#if IOS
    /// <summary>
    /// Calculates the appropriate top margin for iOS navigation bar to enable Large Title behavior.
    /// </summary>
    /// <returns>Top margin in device-independent pixels</returns>
    private static double GetNavigationBarHeight()
    {
        try
        {
            // Get display metrics to determine device type
            var displayInfo = DeviceDisplay.Current.MainDisplayInfo;
            var screenHeight = displayInfo.Height / displayInfo.Density;
            var screenWidth = displayInfo.Width / displayInfo.Density;
            
            // Determine if this is likely a device with notch/dynamic island
            bool hasNotch = screenHeight > 800 && (screenHeight / screenWidth > 2.0);
            
            var deviceModel = DeviceInfo.Current.Model;
            var deviceIdiom = DeviceInfo.Current.Idiom;
            
            if (deviceIdiom == DeviceIdiom.Phone)
            {
                if (hasNotch || deviceModel.Contains("iPhone1") || screenHeight > 2400)
                {
                    // Modern devices: Large Title area + Status bar + Nav bar
                    return 140.0; // Increased for Large Title space
                }
                else
                {
                    // Older devices: Smaller Large Title area
                    return 100.0;
                }
            }
            else if (deviceIdiom == DeviceIdiom.Tablet)
            {
                return hasNotch ? 120.0 : 100.0;
            }
            
            return 120.0; // Default for Large Title behavior
        }
        catch (Exception)
        {
            return 120.0; // Fallback for Large Title behavior
        }
    }
#endif
}