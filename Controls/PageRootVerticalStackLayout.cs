using Microsoft.Maui.Controls;

namespace Ifpa.Controls;

/// <summary>
/// A VerticalStackLayout that automatically applies IgnoreSafeArea=true on iOS for liquid glass effect.
/// Also adds appropriate top padding so content isn't hidden behind the navigation bar.
/// Use this as the root container in ContentPages to enable the full liquid glass experience.
/// </summary>
public class PageRootVerticalStackLayout : VerticalStackLayout
{
    public PageRootVerticalStackLayout()
    {
        LiquidGlassHelper.ApplyLiquidGlassBehavior(this);
    }
}