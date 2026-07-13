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

}
