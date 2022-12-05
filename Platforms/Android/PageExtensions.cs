using Google.Android.Material.BottomSheet;
using Microsoft.Maui.Platform;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Ifpa.Platforms.Extensions
{
    public static partial class PageExtensions
    {
        public static void ShowBottomSheet(this Page page, IView bottomSheetContent, bool dimDismiss)
        {
            var bottomSheetDialog = new BottomSheetDialog(Platform.CurrentActivity?.Window?.DecorView.FindViewById(Android.Resource.Id.Content)?.RootView?.Context);
            bottomSheetDialog.SetContentView(bottomSheetContent.ToPlatform(page.Handler?.MauiContext ?? throw new Exception("MauiContext is null")));
            bottomSheetDialog.Show();
        }
    }
}
