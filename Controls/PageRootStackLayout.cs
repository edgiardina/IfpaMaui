using Microsoft.Maui.Controls;

namespace Ifpa.Controls;

/// <summary>
/// A StackLayout that automatically applies IgnoreSafeArea=true on iOS for liquid glass effect.
/// Use this as the root container in ContentPages to enable the full liquid glass experience.
/// </summary>
public class PageRootStackLayout : StackLayout
{
    public PageRootStackLayout()
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