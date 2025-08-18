using UIKit;

namespace Ifpa.Platforms.iOS.Utils
{
    internal static class ToolbarItemSecondaryExtensions
    {
        public static UIAction ToSecondaryUiAction(this ToolbarItem item)
        {
            var weak = new WeakReference<ToolbarItem>(item);

            return UIAction.Create(item.Text ?? string.Empty, image: null, identifier: null, handler: _ =>
            {
                if (item is IMenuItemController mic)
                {
                    mic.Activate();
                    return;
                }

                if (weak.TryGetTarget(out var target))
                    target.Command?.Execute(target.CommandParameter);
            });
        }

        public static UIBarButtonItem ToUIBarButtonItem(this ToolbarItem item, bool forceShowAsPrimary = false, bool addToRightSide = true)
        {
            // Build a UIBarButtonItem from the ToolbarItem (text or icon)
            var barItem = CreateBarButtonItem(item);

            // Keep the enabled/text/icon in sync with the ToolbarItem
            item.PropertyChanged += (_, e) =>
            {
                if (e.PropertyName == nameof(ToolbarItem.IsEnabled))
                    barItem.Enabled = item.IsEnabled;

                if (e.PropertyName == nameof(ToolbarItem.Text) && string.IsNullOrEmpty(item.IconImageSource?.ToString()))
                    barItem.Title = item.Text;

                if (e.PropertyName == nameof(ToolbarItem.IconImageSource) && item.IconImageSource is FileImageSource fi2)
                {
                    var img2 = UIImage.FromBundle(fi2.File);
                    if (img2 != null) barItem.Image = img2;
                }
            };

            return barItem;
        }

        static UIBarButtonItem CreateBarButtonItem(ToolbarItem item)
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

            // Prefer icon if provided (FileImageSource only; expand if you need stream/FontImage support)
            if (item.IconImageSource is FileImageSource fi && !string.IsNullOrEmpty(fi.File))
            {
                var image = UIImage.FromBundle(fi.File);
                if (image != null)
                {
                    var bbi = new UIBarButtonItem(image, UIBarButtonItemStyle.Plain, onClick)
                    {
                        Enabled = item.IsEnabled,
                        AccessibilityLabel = item.Text ?? string.Empty
                    };
                    return bbi;
                }
            }

            // Fallback to text
            var title = item.Text ?? string.Empty;
            var barItem = new UIBarButtonItem(title, UIBarButtonItemStyle.Plain, onClick)
            {
                Enabled = item.IsEnabled
            };
            return barItem;
        }
    }
}
