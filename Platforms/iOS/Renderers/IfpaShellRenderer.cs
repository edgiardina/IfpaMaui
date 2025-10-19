using Ifpa.Platforms.iOS.Utils;
using Ifpa.Services;
using Microsoft.Maui.Controls.Handlers.Compatibility;
using Microsoft.Maui.Controls.Platform.Compatibility;
using Microsoft.Maui.Platform;
using UIKit;

namespace Ifpa.Platforms.Renderers
{
    /// <summary>
    /// We override the ShellRenderer to provide custom tab bar appearance and secondary toolbar menu button functionality.
    /// </summary>
    public class IfpaShellRenderer : ShellRenderer
    {
        /// <summary>
        /// Create a custom ShellTabBarAppearanceTracker to show badges on the tab bar
        /// </summary>
        /// <returns></returns>
        protected override IShellTabBarAppearanceTracker CreateTabBarAppearanceTracker()
        {
            return new BadgeShellTabbarAppearanceTracker();
        }

        /// <summary>
        /// Create a custom ShellPageRendererTracker that builds a UIMenu for secondary toolbar items
        /// </summary>
        /// <returns></returns>
        protected override IShellPageRendererTracker CreatePageRendererTracker()
        {
            // use our tracker that builds a UIMenu for secondary items
            return new SecondaryToolbarMenuPageRendererTracker(this);
        }

        protected override IShellItemRenderer CreateShellItemRenderer(ShellItem item)
        {
            var renderer = new LargeTitleShellItemRenderer(this)
            {
                ShellItem = item
            };
            return renderer;
        }

    }

    class BadgeShellTabbarAppearanceTracker : ShellTabBarAppearanceTracker
    {
        private readonly NotificationService notificationService;

        private const int myStatsTabIndex = 2;

        private UITabBarItem _myStatsTabBarItem;

        public BadgeShellTabbarAppearanceTracker()
        {
            notificationService = Application.Current.Handler.MauiContext.Services.GetService<NotificationService>();
        }

        public override void SetAppearance(UITabBarController controller, ShellAppearance appearance)
        {
            base.SetAppearance(controller, appearance);

            // Tabbar has changed in iOS 18
            // https://stackoverflow.com/questions/79049272/maui-floating-tabbar-on-ipads-in-ios-18
            if (UIDevice.CurrentDevice.CheckSystemVersion(17, 0))
            {
                controller.TraitOverrides.HorizontalSizeClass = UIUserInterfaceSizeClass.Compact;
            }
        }

        public override void UpdateLayout(UITabBarController controller)
        {
            base.UpdateLayout(controller);

            if (_myStatsTabBarItem is null)
            {
                _myStatsTabBarItem = controller.TabBar.Items?[myStatsTabIndex];
                if (_myStatsTabBarItem is not null)
                {
                    UpdateBadge(0);
                    notificationService.ActivityFeedNotificationChanged += OnCountChanged;
                }
            }
        }

        private void OnCountChanged(object sender, ActivityFeedNotificationChangedEventArgs e)
        {
            UpdateBadge(e.UnreadCount);
        }

        private void UpdateBadge(int count)
        {
            if (_myStatsTabBarItem is not null)
            {
                if (count <= 0)
                {
                    _myStatsTabBarItem.BadgeValue = null;
                }
                else
                {
                    _myStatsTabBarItem.BadgeValue = count.ToString();
                    _myStatsTabBarItem.BadgeColor = Colors.Red.ToPlatform();
                }
            }
        }
        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);
            notificationService.ActivityFeedNotificationChanged -= OnCountChanged;
        }
    }

    /// <summary>
    /// Collects ToolbarItemOrder.Secondary items into a single UIMenu button (ellipsis) on iOS/MacCatalyst.
    /// </summary>
    internal sealed class SecondaryToolbarMenuPageRendererTracker : ShellPageRendererTracker
    {
        private readonly IFontManager _fontManager;

        public SecondaryToolbarMenuPageRendererTracker(IShellContext context) : base(context)
        {
            _fontManager = IPlatformApplication.Current?.Services.GetService<IFontManager>();
        }

        protected override void UpdateToolbarItems()
        {
            var page = Page;

            // Prefer Page toolbar items; fall back to Shell toolbar items
            IEnumerable<ToolbarItem> items =
                (page?.ToolbarItems?.Count > 0 ? page!.ToolbarItems : Shell.Current?.ToolbarItems)
                ?? Enumerable.Empty<ToolbarItem>();

            var primaryButtons = new List<UIBarButtonItem>();
            var secondaryActions = new List<UIMenuElement>();

            foreach (var item in items.OrderBy(x => x.Priority))
            {
                if (item.Order == ToolbarItemOrder.Secondary)
                {
                    secondaryActions.Add(item.ToSecondaryUiAction(_fontManager));
                }
                else
                {
                    primaryButtons.Add(item.ToUIBarButtonItem(_fontManager));
                }
            }

            // Build the “More” (ellipsis) menu button if there are secondaries
            if (secondaryActions.Count > 0)
            {
                var icon = ResolveMenuIcon();
                var menu = UIMenu.Create(title: string.Empty, image: null, identifier: UIMenuIdentifier.None,
                                         options: UIMenuOptions.DisplayInline, children: secondaryActions.ToArray());

                var menuButton = new UIBarButtonItem(icon, menu)
                {
                    AccessibilityIdentifier = "SecondaryToolbarMenuButton"
                };

                // Insert at the front so it shows closest to the title
                primaryButtons.Insert(0, menuButton);
            }

            // iOS shows right bar items in reverse order of insertion; reverse for desired visual order
            if (primaryButtons.Count > 0)
                primaryButtons.Reverse();

            if (ViewController?.NavigationItem is UINavigationItem navItem)
            {
                navItem.SetRightBarButtonItems(primaryButtons.ToArray(), animated: false);
            }
        }

        static UIImage ResolveMenuIcon()
        {
            // 1) If you add an Asset Catalog image named "toolbar_more", it will be used.
            var bundleIcon = UIImage.FromBundle("toolbar_more");
            if (bundleIcon is not null) return bundleIcon;

            // 2) Fallback to SF Symbol “ellipsis.circle” (iOS 13+ / Catalyst)
            var sys = UIImage.GetSystemImage("ellipsis.circle");
            if (sys is not null) return sys;

            // 3) Last resort: a plain more icon (rarely null on modern iOS)
            return UIImage.FromBundle("more");
        }
    }
}
