using Microsoft.Maui.Controls.Compatibility.Platform.iOS;
using Ifpa.Interfaces;
using Microsoft.Maui.Platform;
using Ifpa.Platforms.Utils;
using UIKit;
using Microsoft.Maui.Controls.Platform.Compatibility;
using System.Reflection;
using System.Runtime.CompilerServices;
using Serilog;

namespace Ifpa.Platforms.Services
{
    public class ToolbarBadgeService : IToolbarBadgeService
    {
        record BadgeState(int Value, Color BackgroundColor, Color TextColor);

        static readonly ConditionalWeakTable<ToolbarItem, BadgeState> badges = new();

        readonly IDispatcher Dispatcher;
        public ToolbarBadgeService(IDispatcher dispatcher)
        {
            Dispatcher = dispatcher ?? throw new ArgumentNullException(nameof(dispatcher));
        }

        public void SetBadge(Page page, ToolbarItem item, int value, Color backgroundColor, Color textColor)
        {
            badges.AddOrUpdate(item, new BadgeState(value, backgroundColor, textColor));

            Dispatcher.Dispatch(() =>
            {
#pragma warning disable CA1422 // Validate platform compatibility
                var rootViewController = UIApplication.SharedApplication.KeyWindow.RootViewController;
#pragma warning restore CA1422 // Validate platform compatibility

                var rightButtonItems = FindRightBarButtonItemsInViewController(rootViewController, page);

                if (rightButtonItems != null)
                {
                    ApplyBadges(page, rightButtonItems);
                }
                else
                {
                    //log that we couldn't find the right bar button items
                    Log.Warning("ToolbarBadgeService - Couldn't find right bar button items");
                }
            });
        }

        /// <summary>
        /// Puts the saved badges on the bar button items of a page. MAUI disposes and re-creates the bar button
        /// items each time it updates the toolbar, so the Shell page tracker calls this after each update.
        /// </summary>
        public static void ApplyBadges(Page page, UIBarButtonItem[] rightButtonItems)
        {
            if (page == null || rightButtonItems == null)
            {
                return;
            }

            foreach (var item in page.ToolbarItems)
            {
                if (!badges.TryGetValue(item, out var state))
                {
                    continue;
                }

                // index is actually inversed order (Why? MAUI).
                var index = page.ToolbarItems.Count - page.ToolbarItems.IndexOf(item) - 1;

                if (index < rightButtonItems.Length)
                {
                    rightButtonItems[index].AddBadge(state.Value, state.BackgroundColor.ToPlatform(), state.TextColor.ToPlatform());
                }
            }
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
