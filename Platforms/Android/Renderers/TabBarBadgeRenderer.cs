using Google.Android.Material.Badge;
using Google.Android.Material.BottomNavigation;
using Ifpa.Controls;
using Ifpa.Services;
using Microsoft.Maui.Controls.Handlers.Compatibility;
using Microsoft.Maui.Controls.Platform.Compatibility;
using Microsoft.Maui.Platform;

namespace Ifpa.Platforms.Renderers
{
    public class TabbarBadgeRenderer : ShellRenderer
    {
        protected override IShellBottomNavViewAppearanceTracker CreateBottomNavViewAppearanceTracker(ShellItem shellItem)
        {
            return new BadgeShellBottomNavViewAppearanceTracker(this, shellItem);
        }
    }

    class BadgeShellBottomNavViewAppearanceTracker : ShellBottomNavViewAppearanceTracker
    {
        private readonly NotificationService notificationService;

        private const int myStatsTabIndex = 2;

        private BadgeDrawable? badgeDrawable;
        public BadgeShellBottomNavViewAppearanceTracker(IShellContext shellContext, ShellItem shellItem) : base(shellContext, shellItem)
        {
            notificationService = Application.Current.Handler.MauiContext.Services.GetService<NotificationService>();
        }
        public override void SetAppearance(BottomNavigationView bottomView, IShellAppearanceElement appearance)
        {
            base.SetAppearance(bottomView, appearance);

            if (badgeDrawable is null)
            {    
                badgeDrawable = bottomView.GetOrCreateBadge(myStatsTabIndex);
                UpdateBadge(0);
                notificationService.ActivityFeedNotificationChanged += OnCountChanged;
            }
        }

        private void OnCountChanged(object sender, ActivityFeedNotificationChangedEventArgs e)
        {
            UpdateBadge(e.UnreadCount);
        }

        private void UpdateBadge(int count)
        {
            if (badgeDrawable is not null)
            {
                if (count <= 0)
                {
                    badgeDrawable.SetVisible(false);
                }
                else
                {
                    badgeDrawable.Number = count;
                    badgeDrawable.BackgroundColor = Colors.Red.ToPlatform();
                    badgeDrawable.BadgeTextColor = Colors.White.ToPlatform();
                    badgeDrawable.SetVisible(true);
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
