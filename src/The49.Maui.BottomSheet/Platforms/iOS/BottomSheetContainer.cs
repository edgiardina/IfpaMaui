using CoreGraphics;
using Microsoft.Maui.Platform;
using UIKit;

namespace The49.Maui.BottomSheet;

internal class BottomSheetContainer : UIView
{
    BottomSheet _sheet;
    UIView _view;

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

        // iOS 26 sizes (and insets) a sheet from its content's measured size. The49 sizes the
        // content to the TALLEST detent so dragging between detents doesn't re-layout the content,
        // but on iOS 26 that overflow makes the system present the sheet as an inset card whose
        // square bottom corners the rounded display then clips. Fitting the content to the card
        // lets iOS 26 render the native edge-to-edge sheet, with the bottom corners following the
        // device curve automatically. Earlier iOS keeps the original (no-relayout) behavior.
        if (OperatingSystem.IsIOSVersionAtLeast(26))
        {
            _view.Frame = Bounds;
        }
        else
        {
            _view.Frame = new CGRect(0, 0, Bounds.Width, h);
        }

        _sheet.Arrange(_view.Frame.ToRectangle());
        _sheet.Controller.Layout();
    }
}
