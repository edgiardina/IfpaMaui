using Microsoft.Maui.Controls;

namespace Ifpa.Controls;

/// <summary>
/// A Grid that automatically applies IgnoreSafeArea=true on iOS for liquid glass effect.
/// Use this as the root container in ContentPages to enable the full liquid glass experience.
/// </summary>
public class PageRootGrid : Grid
{
    public PageRootGrid()
    {
#if IOS
        // Only apply on iOS for liquid glass effect
        if (DeviceInfo.Version.Major >= 26)
        {
            this.IgnoreSafeArea = true;
        }
#endif
    }
}