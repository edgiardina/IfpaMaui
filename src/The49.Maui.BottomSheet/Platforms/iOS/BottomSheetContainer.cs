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

    // At a partial detent the card's bottom corners sit inside the device's rounded display,
    // so round them enough to hug the screen curve. Fullscreen reaches the true screen corners.
    const double PartialBottomCornerRadius = 44;

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
        // attached to the bottom edge with SQUARE corners, which the device's rounded display then
        // clips. Round the card's bottom corners enough to sit inside that curve. Older iOS keeps
        // its original edge-attached behavior.
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
        // so paint the visible card here where it will be masked to the rounded shape.
        Microsoft.Maui.Graphics.Paint paint = _sheet.BackgroundBrush;
        BackgroundColor = paint?.ToColor()?.ToPlatform() ?? UIColor.SystemBackground;

        var isFullscreen = Bounds.Height >= tallestDetentHeight - 20;
        var maxR = (nfloat)System.Math.Min(Bounds.Width / 2.0, Bounds.Height / 2.0);
        var top = (nfloat)System.Math.Min(System.Math.Max(_sheet.CornerRadius, 0), (double)maxR);
        // Only the partial (floating) detent needs the larger bottom radius; fullscreen reaches the
        // real screen corners where the sheet's own radius is correct.
        var bottom = isFullscreen
            ? top
            : (nfloat)System.Math.Min(System.Math.Max(_sheet.CornerRadius, PartialBottomCornerRadius), (double)maxR);

        _cardMask ??= new CAShapeLayer();
        _cardMask.Path = BuildCardPath(Bounds, top, bottom);
        Layer.Mask = _cardMask;
    }

    // Rounded-rect path with independent top and bottom corner radii.
    static CGPath BuildCardPath(CGRect r, nfloat rTop, nfloat rBottom)
    {
        var p = new CGPath();
        p.MoveToPoint(r.Left + rTop, r.Top);
        p.AddLineToPoint(r.Right - rTop, r.Top);
        p.AddArcToPoint(r.Right, r.Top, r.Right, r.Top + rTop, rTop);
        p.AddLineToPoint(r.Right, r.Bottom - rBottom);
        p.AddArcToPoint(r.Right, r.Bottom, r.Right - rBottom, r.Bottom, rBottom);
        p.AddLineToPoint(r.Left + rBottom, r.Bottom);
        p.AddArcToPoint(r.Left, r.Bottom, r.Left, r.Bottom - rBottom, rBottom);
        p.AddLineToPoint(r.Left, r.Top + rTop);
        p.AddArcToPoint(r.Left, r.Top, r.Left + rTop, r.Top, rTop);
        p.CloseSubpath();
        return p;
    }
}
