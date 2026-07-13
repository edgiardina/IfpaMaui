using Microsoft.Maui.Controls.Compatibility.Platform.iOS;
using Ifpa.Interfaces;
using Microsoft.Maui.Platform;
using Ifpa.Platforms.Utils;
using UIKit;
using Microsoft.Maui.Controls.Platform.Compatibility;
using System.Reflection;
using Serilog;

namespace Ifpa.Platforms.Services
{
    public class ToolbarBadgeService : IToolbarBadgeService
    {
        readonly IDispatcher Dispatcher;
        public ToolbarBadgeService(IDispatcher dispatcher)
        {
            Dispatcher = dispatcher ?? throw new ArgumentNullException(nameof(dispatcher));
        }

        public void SetBadge(Page page, ToolbarItem item, int value, Color backgroundColor, Color textColor)
        {
            Dispatcher.Dispatch(() =>
            {
#pragma warning disable CA1422 // Validate platform compatibility
                var rootViewController = UIApplication.SharedApplication.KeyWindow.RootViewController;
#pragma warning restore CA1422 // Validate platform compatibility

                var rightButtonItems = FindRightBarButtonItemsInViewController(rootViewController, page);

                var index = page.ToolbarItems.IndexOf(item);

                // index is actually inversed order (Why? MAUI).
                if (index >= 0)
                {
                    index = page.ToolbarItems.Count - index - 1;
                }

                if(rightButtonItems != null && index < rightButtonItems.Length)
                {
                    var barButtonItem = rightButtonItems[index];
                    barButtonItem.AddBadge(value, backgroundColor.ToPlatform(), textColor.ToPlatform());
                }
                else
                {
                    //log that we couldn't find the right bar button items
                    Log.Warning("ToolbarBadgeService - Couldn't find right bar button items");
                }
            });
        }


        private UIBarButtonItem[] FindRightBarButtonItemsInViewController(UIViewController viewController, Page page)
        {
            if (viewController == null)
            {
                return null;
            }

            // Check if the current view controller is a ShellSectionRootRenderer
            if (viewController is ShellSectionRootRenderer shellSectionRootRenderer)
            {
                // Return the RightBarButtonItems from the NavigationItem
                if (shellSectionRootRenderer.NavigationItem?.RightBarButtonItems != null && shellSectionRootRenderer.NavigationItem?.Title == page.Title)
                {
                    Log.Information("ToolbarBadgeService - found Navigation Item with Title {Title}", shellSectionRootRenderer.NavigationItem?.Title);
                    return shellSectionRootRenderer.NavigationItem.RightBarButtonItems;
                }
            }

            // If not found, iterate through the child view controllers
            foreach (var childViewController in viewController.ChildViewControllers)
            {
                var rightBarButtonItems = FindRightBarButtonItemsInViewController(childViewController, page);

                if (rightBarButtonItems != null)
                {
                    return rightBarButtonItems;
                }
            }

            // If a presented view controller exists, check that one as well
            if (viewController.PresentedViewController != null)
            {
                var presentedRightBarButtonItems = FindRightBarButtonItemsInViewController(viewController.PresentedViewController, page);

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
