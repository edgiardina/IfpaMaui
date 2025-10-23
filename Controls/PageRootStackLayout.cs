using Microsoft.Maui.Controls;

namespace Ifpa.Controls;

/// <summary>
/// A StackLayout that automatically applies IgnoreSafeArea=true on iOS for liquid glass effect.
/// Also adds appropriate top padding so content isn't hidden behind the navigation bar.
/// Use this as the root container in ContentPages to enable the full liquid glass experience.
/// </summary>
public class PageRootStackLayout : StackLayout
{
    public PageRootStackLayout()
    {
        LiquidGlassHelper.ApplyLiquidGlassBehavior(this);
    }
}