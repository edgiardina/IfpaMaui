using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Linq;
using Foundation;
using Microsoft.Maui.Controls;
using FormsPage = Microsoft.Maui.Controls.Page;
using iOSNamespace = Microsoft.Maui.Controls.PlatformConfiguration.iOS;
using iOSPage = Microsoft.Maui.Controls.PlatformConfiguration.iOSSpecific.Page;
using Microsoft.Maui.Controls.PlatformConfiguration.iOSSpecific;
using Microsoft.Maui.Graphics;
using ObjCRuntime;
using UIKit;
using Microsoft.Maui.Controls.Platform.Compatibility;
using Microsoft.Maui.Controls.Compatibility.Platform.iOS;

namespace Ifpa.Platforms.Renderers
{
    /// <summary>
    /// Custom ShellItemRenderer for iOS that supports Large Titles, pulled forward from .NET 10 MAUI PR.
    /// </summary>
    internal class LargeTitleShellItemRenderer : UITabBarController, IShellItemRenderer, IUINavigationControllerDelegate
    {
        readonly IShellContext _context;
        ShellItem _shellItem;
        List<IShellSectionRenderer> _renderers = new();
        bool _disposed;

        public LargeTitleShellItemRenderer(IShellContext context)
        {
            _context = context;
            Delegate = new ShellItemRendererDelegate(this);
        }

        public ShellItem ShellItem
        {
            get => _shellItem;
            set
            {
                if (_shellItem == value)
                    return;

                if (_shellItem != null)
                {
                    ((INotifyCollectionChanged)_shellItem.Items).CollectionChanged -= OnShellSectionsChanged;
                }

                _shellItem = value;
                ((INotifyCollectionChanged)_shellItem.Items).CollectionChanged += OnShellSectionsChanged;

                ResetTabs();
            }
        }

        public UIViewController ViewController => this;

        public void SetAppearance(ShellAppearance appearance)
        {
            // No-op in this forward-port; section renderers in this MAUI version don't expose SetAppearance.
        }

        public void ResetAppearance()
        {
            // No-op; appearance will be handled by individual section renderers/trackers.
        }

        public override void ViewDidLayoutSubviews()
        {
            base.ViewDidLayoutSubviews();
            UpdateLargeTitles();
        }

        void OnShellSectionsChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            ResetTabs();
        }

        void ResetTabs()
        {
            foreach (var r in _renderers)
            {
                TryDisconnect(r);

                (r as IDisposable)?.Dispose();
            }

            _renderers.Clear();

            var controllers = new List<UIViewController>();

            foreach (var section in _shellItem.Items)
            {
                var renderer = CreateShellSectionRenderer(section);
                _renderers.Add(renderer);
                controllers.Add(renderer.ViewController);
            }

            ViewControllers = controllers.ToArray();

            if (SelectedIndex >= ViewControllers.Length)
                SelectedIndex = 0;

            OnDisplayedPageChanged();
        }

        IShellSectionRenderer CreateShellSectionRenderer(ShellSection section)
        {
            var renderer = _context.CreateShellSectionRenderer(section);
            return renderer;
        }

        void OnDisplayedPageChanged()
        {
            var currentRenderer = _renderers.ElementAtOrDefault((int)SelectedIndex);
            if (currentRenderer == null)
                return;

            var navController = currentRenderer.ViewController as UINavigationController;
            if (navController == null)
                return;

            var top = navController.TopViewController;
            if (top == null)
                return;

            UpdateLargeTitles(navController, top);
        }

        void UpdateLargeTitles()
        {
            if (((int)SelectedIndex) >= _renderers.Count)
                return;

            if (_renderers[(int)SelectedIndex].ViewController is UINavigationController navController)
            {
                var top = navController.TopViewController;
                if (top != null)
                    UpdateLargeTitles(navController, top);
            }
        }

        void UpdateLargeTitles(UINavigationController navigationController, UIViewController top)
        {
            if (navigationController == null || top == null)
                return;

            if (OperatingSystem.IsIOSVersionAtLeast(11))
            {
                FormsPage page = null;
                if (top is PageRenderer pr)
                    page = pr.Element as FormsPage;
                if (page == null)
                    return;

                var mode = iOSPage.LargeTitleDisplay(page.On<iOSNamespace>());

                navigationController.NavigationBar.PrefersLargeTitles = mode != LargeTitleDisplayMode.Never;

                switch (mode)
                {
                    case LargeTitleDisplayMode.Always:
                        top.NavigationItem.LargeTitleDisplayMode = UINavigationItemLargeTitleDisplayMode.Always;
                        break;
                    case LargeTitleDisplayMode.Never:
                        top.NavigationItem.LargeTitleDisplayMode = UINavigationItemLargeTitleDisplayMode.Never;
                        break;
                    default:
                        top.NavigationItem.LargeTitleDisplayMode = UINavigationItemLargeTitleDisplayMode.Automatic;
                        break;
                }
            }
        }

        static void TryDisconnect(object obj)
        {
            try
            {
                var m = obj?.GetType().GetMethod("Disconnect", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic);
                m?.Invoke(obj, null);
            }
            catch { }
        }

        public void Disconnect()
        {
            if (_disposed)
                return;

            _disposed = true;

            foreach (var renderer in _renderers)
            {
                TryDisconnect(renderer);

                (renderer as IDisposable)?.Dispose();
            }

            _renderers.Clear();

            if (_shellItem != null)
                ((INotifyCollectionChanged)_shellItem.Items).CollectionChanged -= OnShellSectionsChanged;
        }

        protected override void Dispose(bool disposing)
        {
            if (_disposed)
                return;

            base.Dispose(disposing);
            Disconnect();
        }

        class ShellItemRendererDelegate : UITabBarControllerDelegate
        {
            readonly LargeTitleShellItemRenderer _renderer;

            public ShellItemRendererDelegate(LargeTitleShellItemRenderer renderer)
            {
                _renderer = renderer;
            }

            public override void ViewControllerSelected(UITabBarController tabController, UIViewController viewController)
            {
                base.ViewControllerSelected(tabController, viewController);
                _renderer.OnDisplayedPageChanged();
            }
        }
    }
}
