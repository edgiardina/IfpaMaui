namespace Ifpa.Controls
{
    /// <summary>
    /// Color for a toolbar icon. On iOS 26 and later the value is null, so the icon is a template image
    /// and takes the same system tint as the other bar buttons. On all other platforms and versions
    /// the value is IconAccentColor.
    /// </summary>
    [AcceptEmptyServiceProvider]
    public class ToolbarIconColorExtension : IMarkupExtension<Color>
    {
        public static Color Value => OperatingSystem.IsIOSVersionAtLeast(26)
            ? null
            : (Color)Application.Current.Resources["IconAccentColor"];

        public Color ProvideValue(IServiceProvider serviceProvider) => Value;

        object IMarkupExtension.ProvideValue(IServiceProvider serviceProvider) => ProvideValue(serviceProvider);
    }
}
