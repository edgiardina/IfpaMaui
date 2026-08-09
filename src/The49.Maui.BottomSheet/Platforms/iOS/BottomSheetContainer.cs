using CoreAnimation;
using CoreGraphics;
using Microsoft.Maui.Platform;
using UIKit;

namespace The49.Maui.BottomSheet;

internal class BottomSheetContainer : UIView
{
    BottomSheet _sheet;
    UIView _view;
    CAShapeLayer _cardMask;

    // Can't get the sheet max height with large and medium detents
    // custom detents are not supported on iOS 15
    // can't use largestUndimmedIdentifier or selected detent with custom detents on iOS 16
    // So I guess we'll just have to calculate the sheet height ourselves then
    // This number was found by getting the full screen height, subtracting the sheet's UIView height and the top inset
    // This seems to be the spacing iOS leaves at the top of the screen when a sheet is fullscreen
    // TODO: Check if this is the same number for fullscreen sheets opened on top of another sheet
    const int SheetTopSpacing = 10;

    double CalculateTallestDetent(double heightConstraint)
    {
        var window = UIApplication.SharedApplication.KeyWindow;
        var topPadding = window?.SafeAreaInsets.Top ?? 0;
        var maximumDetentValue = heightConstraint - topPadding - SheetTopSpacing;

        return _sheet.GetEnabledDetents().Select(d => d.GetHeight(_sheet, maximumDetentValue)).Max();
    }

    internal BottomSheetContainer(BottomSheet sheet, UIView view)
    {
        _sheet = sheet;
        _view = view;
        AddSubview(_view);
    }
    public override void LayoutSubviews()
    {
        base.LayoutSubviews();
        var h = CalculateTallestDetent(_sheet.Window.Height - BottomSheetManager.KeyboardHeight);
        _view.Frame = new CGRect(0, 0, Bounds.Width, h);
        _sheet.Arrange(_view.Frame.ToRectangle());
        _sheet.Controller.Layout();

        // iOS 26 renders partial-height sheets as floating cards, but on iPhone the card stays
        // attached to the bottom edge with square corners that the device's rounded display then
        // clips. Lift the card above the home-indicator curve and round every corner so it reads
        // as the native floating card. Older iOS keeps its original edge-attached behavior.
        if (OperatingSystem.IsIOSVersionAtLeast(26))
        {
            ApplyFloatingCardMask((nfloat)h);
        }
    }

    void ApplyFloatingCardMask(nfloat tallestDetentHeight)
    {
        if (Bounds.Width <= 0 || Bounds.Height <= 0)
        {
            return;
        }

        // The presented view is kept transparent (see BottomSheetViewController.UpdateBackground),
        // so paint the visible card here where it will be masked to the lifted, rounded shape.
        Microsoft.Maui.Graphics.Paint paint = _sheet.BackgroundBrush;
        BackgroundColor = paint?.ToColor()?.ToPlatform() ?? UIColor.SystemBackground;

        var isFullscreen = Bounds.Height >= tallestDetentHeight - 20;
        var bottomInset = Window?.SafeAreaInsets.Bottom ?? 0;
        // On devices without a home indicator, still lift a little so the rounded corners clear the curve.
        var lift = isFullscreen ? (nfloat)0 : (nfloat)System.Math.Max((double)bottomInset, 8.0);
        var radius = (nfloat)System.Math.Max(_sheet.CornerRadius, 0);

        var cardHeight = (nfloat)System.Math.Max(0.0, (double)(Bounds.Height - lift));
        var cardRect = new CGRect(0, 0, Bounds.Width, cardHeight);
        var path = UIBezierPath.FromRoundedRect(cardRect, radius);

        _cardMask ??= new CAShapeLayer();
        _cardMask.Path = path.CGPath;
        Layer.Mask = _cardMask;
    }
}
