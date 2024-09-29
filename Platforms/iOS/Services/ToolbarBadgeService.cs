using Microsoft.Maui.Controls.Compatibility.Platform.iOS;
using Ifpa.Interfaces;
using Microsoft.Maui.Platform;
using Ifpa.Platforms.Utils;

namespace Ifpa.Platforms.Services
{
    public class ToolbarItemBadgeService : IToolbarBadgeService
    {
        public void SetBadge(Page page, ToolbarItem item, int value, Color backgroundColor, Color textColor)
        {
            Device.BeginInvokeOnMainThread(() =>
            {
                var renderer = Microsoft.Maui.Controls.Compatibility.Platform.iOS.Platform.GetRenderer(page);
                if (renderer == null)
                {
                    renderer = Microsoft.Maui.Controls.Compatibility.Platform.iOS.Platform.CreateRenderer(page);
                    Microsoft.Maui.Controls.Compatibility.Platform.iOS.Platform.SetRenderer(page, renderer);
                }
                var vc = renderer.ViewController;

                var rightButtonItems = vc?.ParentViewController?.NavigationItem?.RightBarButtonItems;

                // If we can't find the button where it typically is check the child view controllers
                // as this is where MasterDetailPages are kept
                if (rightButtonItems == null && vc.ChildViewControllerForHomeIndicatorAutoHidden != null)
                    foreach (var uiObject in vc.ChildViewControllerForHomeIndicatorAutoHidden)
                    {
                        string uiObjectType = uiObject.GetType().ToString();

                        if (uiObjectType.Contains("FormsNav"))
                        {
                            UIKit.UINavigationBar navobj = (UIKit.UINavigationBar)uiObject;

                            if (navobj.Items != null)
                                foreach (UIKit.UINavigationItem navitem in navobj.Items)
                                {
                                    if (navitem.RightBarButtonItems != null)
                                    {
                                        rightButtonItems = navitem.RightBarButtonItems;
                                        break;
                                    }
                                }
                        }
                    }

                var idx = page.ToolbarItems.IndexOf(item);
                if (rightButtonItems != null && rightButtonItems.Length > idx)
                {
                    var barItem = rightButtonItems[idx];
                    if (barItem != null)
                    {
                        barItem.UpdateBadge(value.ToString(), backgroundColor.ToPlatform(), textColor.ToPlatform());
                    }
                }

            });
        }
    }
}
