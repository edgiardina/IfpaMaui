using Microsoft.Maui.Controls.Handlers.Items2;
using Microsoft.Maui.Handlers;
using UIKit;

namespace Ifpa.Platforms.Handlers
{
    /// <summary>
    /// Lets page content scroll under the iOS 26 Liquid Glass tab bar.
    /// </summary>
    /// <remarks>
    /// On iOS 26 the tab bar floats over the page, and UIKit already sizes each tab's page to
    /// the full screen. Two MAUI defaults stop the content from reaching the bottom:
    /// <list type="bullet">
    /// <item>A <see cref="Layout"/> obeys the bottom safe area (<see cref="SafeAreaRegions.Container"/>),
    /// so it pads its children up to the top of the bar and the page background shows behind it.</item>
    /// <item>A <see cref="CollectionView"/> turns off UIKit's content inset adjustment. Without the
    /// layout padding, its last rows would sit under the bar with no way to scroll them clear.</item>
    /// </list>
    /// These mappings let layouts on tab-hosted pages ignore the bottom safe area, and give the
    /// bottom inset to the collection view so it scrolls its content under the bar instead.
    /// Modal pages have no tab bar, so they keep the default behaviour.
    /// </remarks>
    public static class GlassTabBarContentMappings
    {
        public static void Register()
        {
            // Liquid Glass arrived in iOS 26. Older versions draw a solid bar that content cannot show through.
            if (!OperatingSystem.IsIOSVersionAtLeast(26))
            {
                return;
            }

            LayoutHandler.Mapper.AppendToMapping(nameof(GlassTabBarContentMappings), (handler, view) =>
            {
                if (view is Layout layout && IsOnTabHostedPage(layout))
                {
                    ExtendUnderTabBar(layout);
                }
            });

            // Some third-party containers (Syncfusion's SfView, which hosts the SfTabView content)
            // only implement the legacy ISafeAreaView, so SafeAreaEdges does not reach them.
            ViewHandler.ViewMapper.AppendToMapping(nameof(GlassTabBarContentMappings), (handler, view) =>
            {
                // MAUI's own containers read SafeAreaEdges instead, and Layout is handled above.
                if (view is ISafeAreaView legacyView
                    && view is not (Layout or ContentView or ScrollView or Page)
                    && !legacyView.IgnoreSafeArea
                    && view is Element element
                    && IsOnTabHostedPage(element))
                {
                    IgnoreLegacySafeArea(element, handler.PlatformView as UIView);
                }
            });

            CollectionViewHandler2.Mapper.AppendToMapping(nameof(GlassTabBarContentMappings), (handler, view) =>
            {
                if (view is not VisualElement element || !IsOnTabHostedPage(element))
                {
                    return;
                }

                // The inset depends on where the list lands, so recalculate it after every layout pass.
                // SizeChanged fires before the parent views have their final frames, so wait one turn
                // of the main loop.
                if (TrackedCollectionViews.TryAdd(element, null))
                {
                    element.SizeChanged += (_, _) =>
                        element.Dispatcher.Dispatch(() => InsetForTabBar(handler.PlatformView));
                }

                element.Dispatcher.Dispatch(() => InsetForTabBar(handler.PlatformView));
            });
        }

        private static readonly System.Runtime.CompilerServices.ConditionalWeakTable<VisualElement, object> TrackedCollectionViews = new();

        private static bool IsOnTabHostedPage(Element element)
        {
            Page page = null;

            for (var current = element; current is not null; current = current.Parent)
            {
                if (page is null && current is Page nearestPage)
                {
                    page = nearestPage;
                }

                if (current is ShellSection)
                {
                    // A Shell route can still be presented modally, and a modal page has no tab bar.
                    return page is not null
                        && !Shell.GetPresentationMode(page).HasFlag(PresentationMode.Modal)
                        && Shell.GetTabBarIsVisible(page);
                }
            }

            // Pages pushed with Navigation.PushModalAsync are not parented to a ShellSection.
            return false;
        }

        private static void ExtendUnderTabBar(Layout layout)
        {
            // Respect a SafeAreaEdges value that a page sets on purpose.
            if (layout.IsSet(Layout.SafeAreaEdgesProperty))
            {
                return;
            }

            var edges = layout.SafeAreaEdges;
            layout.SafeAreaEdges = new SafeAreaEdges(edges.Left, edges.Top, edges.Right, SafeAreaRegions.None);
        }

        private static void IgnoreLegacySafeArea(Element element, UIView platformView)
        {
            // Syncfusion keeps the flag behind an internal setter, with no public way to change it.
            // If a later version renames it, leave the view alone: it keeps the old padding, no crash.
            try
            {
                var property = element.GetType().GetProperty(
                    nameof(ISafeAreaView.IgnoreSafeArea),
                    System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic);

                if (property is null || !property.CanWrite || property.PropertyType != typeof(bool))
                {
                    return;
                }

                property.SetValue(element, true);
            }
            catch (System.Reflection.AmbiguousMatchException)
            {
                return;
            }

            (element as IView)?.InvalidateMeasure();
            platformView?.SetNeedsLayout();
        }

        private static void InsetForTabBar(UIView platformView)
        {
            var collectionView = FindCollectionView(platformView);
            if (collectionView is null)
            {
                return;
            }

            // UIKit cannot supply this inset. The collection view lives in its own view controller,
            // whose view reports a zero safe area, so read the safe area from the nearest parent
            // that has one and inset by the part of the list that overlaps it.
            var bottomInset = OverlapWithBottomSafeArea(collectionView);

            var contentInset = collectionView.ContentInset;
            if (contentInset.Bottom != bottomInset)
            {
                contentInset.Bottom = bottomInset;
                collectionView.ContentInset = contentInset;

                var indicatorInsets = collectionView.VerticalScrollIndicatorInsets;
                indicatorInsets.Bottom = bottomInset;
                collectionView.VerticalScrollIndicatorInsets = indicatorInsets;
            }
        }

        private static nfloat OverlapWithBottomSafeArea(UIView view)
        {
            for (var ancestor = view.Superview; ancestor is not null; ancestor = ancestor.Superview)
            {
                var safeBottom = ancestor.SafeAreaInsets.Bottom;

                // Skip views with no safe area, and views that are not laid out yet.
                if (safeBottom <= 0 || ancestor.Bounds.Height <= safeBottom)
                {
                    continue;
                }

                var safeAreaTop = ancestor.Bounds.Height - safeBottom;
                var viewBottom = view.ConvertRectToView(view.Bounds, ancestor).Bottom;
                var overlap = Math.Clamp(viewBottom - safeAreaTop, 0, safeBottom);
                return (nfloat)overlap;
            }

            return 0;
        }

        private static UICollectionView FindCollectionView(UIView view)
        {
            if (view is null)
            {
                return null;
            }

            if (view is UICollectionView collectionView)
            {
                return collectionView;
            }

            foreach (var subview in view.Subviews)
            {
                var found = FindCollectionView(subview);
                if (found is not null)
                {
                    return found;
                }
            }

            return null;
        }
    }
}
