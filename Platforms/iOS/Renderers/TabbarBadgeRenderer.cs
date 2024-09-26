using Ifpa.Services;
using Microsoft.Maui.Controls.Handlers.Compatibility;
using Microsoft.Maui.Controls.Platform.Compatibility;
using Microsoft.Maui.Platform;
using UIKit;

namespace Ifpa.Platforms.Renderers
{
    public class TabbarBadgeRenderer : ShellRenderer
    {
        protected override IShellTabBarAppearanceTracker CreateTabBarAppearanceTracker()
        {
            return new BadgeShellTabbarAppearanceTracker();
        }
    }
    class BadgeShellTabbarAppearanceTracker : ShellTabBarAppearanceTracker
    {
        private readonly NotificationService notificationService;

        private const int myStatsTabIndex = 2;

        private UITabBarItem? _cartTabbarItem;

        public BadgeShellTabbarAppearanceTracker()
        {
            notificationService = Application.Current.MainPage.Handler.MauiContext.Services.GetService<NotificationService>();
        }

        public override void UpdateLayout(UITabBarController controller)
        {
            base.UpdateLayout(controller);

            if (_cartTabbarItem is null)
            {

                _cartTabbarItem = controller.TabBar.Items?[myStatsTabIndex];
                if (_cartTabbarItem is not null)
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
            if (_cartTabbarItem is not null)
            {
                if (count <= 0)
                {
                    _cartTabbarItem.BadgeValue = null;
                }
                else
                {
                    _cartTabbarItem.BadgeValue = count.ToString();
                    _cartTabbarItem.BadgeColor = Colors.Red.ToPlatform();
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
