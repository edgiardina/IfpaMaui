#nullable enable
using CoreGraphics;
using Foundation;
using UIKit;

namespace Ifpa.Platforms.iOS.Utils
{
    internal static class ToolbarItemSecondaryExtensions
    {
        public static UIAction ToSecondaryUiAction(this ToolbarItem item)
        {
            var weak = new WeakReference<ToolbarItem>(item);

            var action = UIAction.Create(
                item.Text ?? string.Empty,
                image: GetUIImage(item.IconImageSource, defaultPointSize: 17),
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
                    action.Image = GetUIImage(item.IconImageSource, defaultPointSize: 17);
            };

            return action;
        }

        public static UIBarButtonItem ToUIBarButtonItem(this ToolbarItem item)
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

            var image = GetUIImage(item.IconImageSource, defaultPointSize: 17);

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
                    var newImg = GetUIImage(item.IconImageSource, defaultPointSize: 17);
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
        static UIImage? GetUIImage(ImageSource? src, nfloat defaultPointSize)
        {
            if (src is null) return null;

            if (src is FileImageSource fi)
            {
                var img = UIImage.FromBundle(fi.File);
                if (img != null) return img;
            }

            if (src is FontImageSource font)
                return RenderFontImage(font, defaultPointSize);

            return null;
        }

        /// <summary>
        /// Renders a FontImageSource into a UIImage using CoreGraphics.
        /// Honors Glyph, Size, Color, and FontFamily.
        /// </summary>
        static UIImage? RenderFontImage(FontImageSource font, nfloat defaultPointSize)
        {
            var glyph = font.Glyph;
            if (string.IsNullOrEmpty(glyph))
                return null;

            var sizePt = (nfloat)(font.Size > 0 ? font.Size : defaultPointSize);

            // Resolve UIColor (defaults to label color)
            UIColor color = font.Color.ToNative() ?? UIColor.Label;

            // Resolve UIFont
            UIFont uiFont;
            if (!string.IsNullOrEmpty(font.FontFamily))
                uiFont = UIFont.FromName(font.FontFamily, sizePt) ?? UIFont.SystemFontOfSize(sizePt);
            else
                uiFont = UIFont.SystemFontOfSize(sizePt);

            var attributes = new UIStringAttributes
            {
                ForegroundColor = color,
                Font = uiFont
            };

            using var ns = new NSString(glyph);
            var stringSize = ns.GetSizeUsingAttributes(attributes);

            var padding = new nfloat(2);
            var imgSize = new CGSize(Math.Ceiling(stringSize.Width + padding * 2),
                                     Math.Ceiling(stringSize.Height + padding * 2));

            // Use UIGraphicsImageRenderer instead of deprecated BeginImageContext
            var format = new UIGraphicsImageRendererFormat
            {
                Opaque = false,
                Scale = 0 // use device scale
            };
            var renderer = new UIGraphicsImageRenderer(imgSize, format);

            var image = renderer.CreateImage(ctx =>
            {
                var rect = new CGRect(padding, padding, stringSize.Width, stringSize.Height);
                ns.DrawString(rect, attributes);
            });

            return image;
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
