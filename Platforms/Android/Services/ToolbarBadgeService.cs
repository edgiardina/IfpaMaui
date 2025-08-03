using Android.Views;
using Google.Android.Material.Badge;
using Ifpa.Interfaces;
using Microsoft.Maui.Controls.Compatibility.Platform.Android;
using Serilog;

namespace Ifpa.Platforms.Services
{
    public class ToolbarBadgeService : IToolbarBadgeService
    {
        public void SetBadge(Page page, ToolbarItem item, int value, Color backgroundColor, Color textColor)
        {
            Device.BeginInvokeOnMainThread(() =>
            {

                //var toolbar = Microsoft.Maui.ApplicationModel.Platform.CurrentActivity.FindViewById<AndroidX.AppCompat.Widget.Toolbar>(Resource.Id.toolbar);

                var rootView = Microsoft.Maui.ApplicationModel.Platform.CurrentActivity?.Window?.DecorView?.RootView;

                if (rootView == null)
                    return;
                
                // Recursively search through the view hierarchy
                var toolbar =  FindToolbarInViewGroup(rootView as ViewGroup);

                if (toolbar != null)
                {
                    var idx = page.ToolbarItems.IndexOf(item);
                    if (toolbar.Menu.Size() > idx)
                    {
                        var menuItem = toolbar.Menu.GetItem(idx);

                        var badgeDrawable = BadgeDrawable.Create(toolbar.Context);

                        BadgeUtils.AttachBadgeDrawable(badgeDrawable, toolbar, menuItem.ItemId);

                        if (value <= 0)
                        {
                            badgeDrawable.SetVisible(false);
                        }
                        else
                        {
                            badgeDrawable.Number = value;
                            badgeDrawable.BackgroundColor = backgroundColor.ToAndroid();
                            badgeDrawable.BadgeTextColor = textColor.ToAndroid();
                            badgeDrawable.SetVisible(true);
                        }
                    }
                }
                else
                {
                    Log.Warning("ToolbarBadgeService - Couldn't find toolbar");
                }
            });
        }

        private AndroidX.AppCompat.Widget.Toolbar FindToolbarInViewGroup(ViewGroup viewGroup)
        {
            // If the view group is null, return null
            if (viewGroup == null)
                return null;

            // Loop through all child views of this ViewGroup
            for (int i = 0; i < viewGroup.ChildCount; i++)
            {
                var child = viewGroup.GetChildAt(i);

                // If this child is a Toolbar, return it
                if (child is AndroidX.AppCompat.Widget.Toolbar toolbar)
                {
                    return toolbar;
                }

                // If the child is a ViewGroup, search recursively
                if (child is ViewGroup childViewGroup)
                {
                    var foundToolbar = FindToolbarInViewGroup(childViewGroup);
                    if (foundToolbar != null)
                    {
                        return foundToolbar;
                    }
                }
            }

            // Return null if no toolbar is found
            return null;
        }
    }
}
