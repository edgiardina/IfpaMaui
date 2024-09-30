using Microsoft.Maui.Controls.Compatibility.Platform.iOS;
using Ifpa.Interfaces;
using Microsoft.Maui.Platform;
using Ifpa.Platforms.Utils;
using UIKit;
using Microsoft.Maui.Controls.Platform.Compatibility;
using System.Reflection;

namespace Ifpa.Platforms.Services
{
    public class ToolbarBadgeService : IToolbarBadgeService
    {
        public void SetBadge(Page page, ToolbarItem item, int value, Color backgroundColor, Color textColor)
        {
            Device.BeginInvokeOnMainThread(() =>
            {
                var rootViewController = UIApplication.SharedApplication.KeyWindow.RootViewController;

                var rightButtonItems = FindRightBarButtonItemsInViewController(rootViewController);

                var index = page.ToolbarItems.IndexOf(item);

                // index is actually inversed order (Why? MAUI).
                if (index >= 0)
                {
                    index = page.ToolbarItems.Count - index - 1;
                }

                var barButtonItem = rightButtonItems[index];
                barButtonItem.AddBadge(value, backgroundColor.ToPlatform(), textColor.ToPlatform());
            });
        }


        private UIBarButtonItem[] FindRightBarButtonItemsInViewController(UIViewController viewController)
        {
            if (viewController == null)
            {
                return null;
            }

            // Check if the current view controller is a ShellSectionRootRenderer
            if (viewController is ShellSectionRootRenderer shellSectionRootRenderer)
            {
                // Return the RightBarButtonItems from the NavigationItem
                //TODO: Why the fuck is it Title LRN? who knows. MAUI.
                if (shellSectionRootRenderer.NavigationItem?.RightBarButtonItems != null && shellSectionRootRenderer.NavigationItem?.Title == "LRN")
                {
                    return shellSectionRootRenderer.NavigationItem.RightBarButtonItems;
                }
            }

            // If not found, iterate through the child view controllers
            foreach (var childViewController in viewController.ChildViewControllers)
            {
                var rightBarButtonItems = FindRightBarButtonItemsInViewController(childViewController);

                if (rightBarButtonItems != null)
                {
                    return rightBarButtonItems;
                }
            }

            // If a presented view controller exists, check that one as well
            if (viewController.PresentedViewController != null)
            {
                var presentedRightBarButtonItems = FindRightBarButtonItemsInViewController(viewController.PresentedViewController);

                if (presentedRightBarButtonItems != null)
                {
                    return presentedRightBarButtonItems;
                }
            }

            // If no right bar button items were found
            return null;
        }

    }
}
