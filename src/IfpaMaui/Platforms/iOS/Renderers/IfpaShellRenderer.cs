using Ifpa.Platforms.iOS.Utils;
using Ifpa.Platforms.Services;
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
        /// Create a custom ShellPageRendererTracker to keep badges on the toolbar buttons
        /// </summary>
        /// <returns></returns>
        protected override IShellPageRendererTracker CreatePageRendererTracker()
        {
            return new BadgeShellPageRendererTracker(this);
        }
    }
    class BadgeShellPageRendererTracker : ShellPageRendererTracker
    {
        public BadgeShellPageRendererTracker(IShellContext context) : base(context)
        {
        }

        protected override void UpdateToolbarItems()
        {
            base.UpdateToolbarItems();

            var rightItems = ViewController?.NavigationItem?.RightBarButtonItems;
            MatchOverflowButtonTint(rightItems);
            ToolbarBadgeService.ApplyBadges(Page, rightItems);
        }

        /// <summary>
        /// WORKAROUND for https://github.com/dotnet/maui/issues/40495 (MAUI 10.0.120+, caused by
        /// dotnet/maui#36821): the "..." overflow button for Secondary toolbar
        /// items is now a UIButton custom view. MAUI sets its tint to null, so it inherits the nav bar's
        /// blue Shell.ForegroundColor, while the glass bar buttons next to it draw in the label color.
        /// Pin custom views to the label color so they match. Remove once #40495 is fixed.
        /// MAUI resets these tints if Shell.ForegroundColor changes at runtime; the app never does that.
        /// </summary>
        static void MatchOverflowButtonTint(UIBarButtonItem[] rightItems)
        {
            if (!OperatingSystem.IsIOSVersionAtLeast(26) || rightItems is null)
                return;

            foreach (var item in rightItems)
            {
                if (item.CustomView is { } customView)
                    customView.TintColor = UIColor.Label;
            }
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

            if (OperatingSystem.IsIOSVersionAtLeast(26))
            {
                RestoreGlassBackground(controller);
            }
        }

        /// <summary>
        /// Since MAUI 10.0.90, Shell.TabBarBackgroundColor also sets UITabBar.BackgroundColor on iOS 26,
        /// which draws an opaque rectangle behind the Liquid Glass tab bar
        /// (https://github.com/dotnet/maui/issues/37423). Put back the system glass background after
        /// MAUI applies the Shell appearance. The item title and icon colors stay as MAUI set them.
        /// </summary>
        private static void RestoreGlassBackground(UITabBarController controller)
        {
            var tabBar = controller.TabBar;
            var tabBarAppearance = tabBar.StandardAppearance;

            tabBarAppearance.ConfigureWithDefaultBackground();
            tabBarAppearance.ShadowColor = UIColor.Clear;

            tabBar.StandardAppearance = tabBarAppearance;
            tabBar.ScrollEdgeAppearance = tabBarAppearance;
            tabBar.BackgroundColor = UIColor.Clear;
            tabBar.BarTintColor = UIColor.Clear;
            tabBar.Translucent = true;
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

}
