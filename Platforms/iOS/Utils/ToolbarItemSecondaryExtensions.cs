#nullable enable
using CoreGraphics;
using Foundation;
using UIKit;

namespace Ifpa.Platforms.iOS.Utils
{
    internal static class ToolbarItemSecondaryExtensions
    {
        public static UIAction ToSecondaryUiAction(this ToolbarItem item, IFontManager? fontManager)
        {
            var weak = new WeakReference<ToolbarItem>(item);

            var action = UIAction.Create(
                item.Text ?? string.Empty,
                image: GetUIImage(item.IconImageSource, defaultPointSize: 17, fontManager),
                identifier: new NSString($"Secondary_{item.Text ?? Guid.NewGuid().ToString()}"),
                            handler: _ =>
                {
                    if (item is IMenuItemController mic)
                    {
                        mic.Activate();
                        return;
                    }

                    if (weak.TryGetTarget(out var target) && target.Command?.CanExecute(target.CommandParameter) == true)
                        target.Command.Execute(target.CommandParameter);
                });

            // Update action’s image/text when item changes
            item.PropertyChanged += (_, e) =>
            {
                if (e.PropertyName == nameof(ToolbarItem.Text))
                    action.Title = item.Text ?? string.Empty;

                if (e.PropertyName == nameof(ToolbarItem.IconImageSource))
                    action.Image = GetUIImage(item.IconImageSource, defaultPointSize: 17, fontManager);
            };

            return action;
        }

        public static UIBarButtonItem ToUIBarButtonItem(this ToolbarItem item, IFontManager? fontManager)
        {
            EventHandler onClick = (_, __) =>
            {
                if (item is IMenuItemController mic)
                {
                    mic.Activate();
                    return;
                }

                if (item.Command?.CanExecute(item.CommandParameter) == true)
                    item.Command.Execute(item.CommandParameter);
            };

            var image = GetUIImage(item.IconImageSource, 17, fontManager);

            UIBarButtonItem barItem;
            if (image != null)
            {
                barItem = new UIBarButtonItem(image, UIBarButtonItemStyle.Plain, onClick)
                {
                    Enabled = item.IsEnabled,
                    AccessibilityLabel = item.Text ?? string.Empty
                };
            }
            else
            {
                barItem = new UIBarButtonItem(item.Text ?? string.Empty, UIBarButtonItemStyle.Plain, onClick)
                {
                    Enabled = item.IsEnabled
                };
            }

            // Keep in sync
            item.PropertyChanged += (_, e) =>
            {
                if (e.PropertyName == nameof(ToolbarItem.IsEnabled))
                    barItem.Enabled = item.IsEnabled;

                if (e.PropertyName == nameof(ToolbarItem.Text) && image == null)
                    barItem.Title = item.Text ?? string.Empty;

                if (e.PropertyName == nameof(ToolbarItem.IconImageSource))
                {
                    var newImg = GetUIImage(item.IconImageSource, defaultPointSize: 17, fontManager);
                    if (newImg != null)
                    {
                        barItem.Image = newImg;
                        barItem.Title = null; // prefer image
                    }
                    else
                    {
                        barItem.Image = null;
                        barItem.Title = item.Text ?? string.Empty;
                    }
                }
            };

            return barItem;
        }

        /// <summary>
        /// Returns a UIImage for FileImageSource or FontImageSource. Null for others.
        /// </summary>
        static UIImage? GetUIImage(ImageSource? src, nfloat defaultPointSize, IFontManager? fontManager)
        {
            if (src is null) return null;

            if (src is FileImageSource fi && !string.IsNullOrEmpty(fi.File))
                return UIImage.FromBundle(fi.File);

            if (src is FontImageSource fnt)
                return RenderFontImage(fnt, defaultPointSize, fontManager);

            return null;
        }

        /// <summary>
        /// Renders a FontImageSource into a UIImage using CoreGraphics.
        /// Honors Glyph, Size, Color, and FontFamily.
        /// </summary>
        static UIImage? RenderFontImage(FontImageSource font, nfloat defaultPointSize, IFontManager? fontManager)
        {
            // No glyph? nothing to draw.
            if (string.IsNullOrEmpty(font.Glyph))
                return null;

            var pointSize = (nfloat)(font.Size > 0 ? font.Size : defaultPointSize);

            // Resolve UIColor (defaults to label color)
            var uiColor = (font.Color is { } c)
                ? UIColor.FromRGBA((nfloat)c.Red, (nfloat)c.Green, (nfloat)c.Blue, (nfloat)c.Alpha)
                : UIColor.Label;

            // Use MAUI's font manager so aliases from ConfigureFonts() are honored
            UIFont uiFont;
            if (fontManager != null)
            {
                var mauiFont = Microsoft.Maui.Font.OfSize(font.FontFamily, pointSize);
                uiFont = fontManager.GetFont(mauiFont) ?? UIFont.SystemFontOfSize(pointSize);
            }
            else
            {
                // Fallback if DI unavailable
                uiFont = !string.IsNullOrEmpty(font.FontFamily)
                    ? (UIFont.FromName(font.FontFamily, pointSize) ?? UIFont.SystemFontOfSize(pointSize))
                    : UIFont.SystemFontOfSize(pointSize);
            }

            var attrs = new UIStringAttributes { Font = uiFont, ForegroundColor = uiColor };

            using var ns = new NSString(font.Glyph);
            var size = ns.GetSizeUsingAttributes(attrs);
            var pad = (nfloat)2;
            var imageSize = new CGSize(Math.Ceiling(size.Width + pad * 2), Math.Ceiling(size.Height + pad * 2));

            // iOS 17+ safe image renderer
            var format = new UIGraphicsImageRendererFormat { Opaque = false, Scale = 0 };
            var renderer = new UIGraphicsImageRenderer(imageSize, format);

            return renderer.CreateImage(ctx =>
            {
                ns.DrawString(new CGRect(pad, pad, size.Width, size.Height), attrs);
            });
        }


        /// <summary>
        /// Converts Microsoft.Maui.Graphics.Color? to UIColor (null → null).
        /// </summary>
        static UIColor? ToNative(this Microsoft.Maui.Graphics.Color? color)
        {
            if (color is null) return null;
            return UIColor.FromRGBA(
                (nfloat)color.Red,
                (nfloat)color.Green,
                (nfloat)color.Blue,
                (nfloat)color.Alpha);
        }
    }
}
