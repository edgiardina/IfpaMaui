using Android.Views.Inspectors;
using AndroidX.AppCompat.App;

namespace Plugin.BottomSheet.Android;

/// <summary>
/// Provides a mechanism to handle touch events occurring outside a bottom sheet,
/// ensuring proper interaction and event propagation in non-modal scenarios.
/// </summary>
/// <remarks>
/// The dialog window fills the screen and sits above the activity window and the activity's sub-windows
/// (toolbar overflow menus, popup menus, spinner drop-downs). The window manager therefore sends every touch
/// to the dialog, and a popup below it never sees its own touches or the outside touches that dismiss it.
/// This listener routes each gesture the way the window manager would if the dialog window were not there.
/// </remarks>
internal sealed class BottomSheetDialogTouchOutsideListener : Java.Lang.Object, View.IOnTouchListener
{
    private const int FirstSubWindowType = 1000;
    private const int LastSubWindowType = 1999;

    private readonly AppCompatActivity _activity;
    private readonly global::Android.App.Dialog _dialog;

    private View? _gestureTarget;
    private bool _swallowGesture;

    /// <summary>
    /// Initializes a new instance of the <see cref="BottomSheetDialogTouchOutsideListener"/> class.
    /// Handles touch events outside the bottom sheet for non-modal behavior.
    /// </summary>
    /// <param name="activity">The activity hosting the bottom sheet dialog.</param>
    /// <param name="dialog">The bottom sheet dialog whose window receives the touches.</param>
    public BottomSheetDialogTouchOutsideListener(AppCompatActivity activity, global::Android.App.Dialog dialog)
    {
        _activity = activity;
        _dialog = dialog;
    }

    /// <summary>
    /// Handles touch events and dispatches them to the popup under the touch, or to the activity.
    /// </summary>
    /// <param name="v">The view that was touched.</param>
    /// <param name="e">The motion event details.</param>
    /// <returns>Always returns false to allow further event processing.</returns>
    public bool OnTouch(View? v, MotionEvent? e)
    {
        if (e is null)
        {
            return false;
        }

        // Pick the target on DOWN and keep it for the rest of the gesture.
        if (e.ActionMasked == MotionEventActions.Down)
        {
            _swallowGesture = false;
            _gestureTarget = FindSubWindow(e.RawX, e.RawY, containsPoint: true);

            // A touch-modal popup (e.g. an overflow menu) gets touches outside its bounds and dismisses itself.
            // Like the window manager, don't pass that gesture on to the activity.
            if (_gestureTarget is null
                && FindSubWindow(e.RawX, e.RawY, containsPoint: false) is View touchModalPopup)
            {
                DispatchToWindow(touchModalPopup, e);
                _swallowGesture = true;
            }
        }

        if (_swallowGesture == false)
        {
            if (_gestureTarget is not null
                && _gestureTarget.IsAttachedToWindow)
            {
                DispatchToWindow(_gestureTarget, e);
            }
            else
            {
                _ = _activity.DispatchTouchEvent(e);
            }
        }

        if (e.ActionMasked is MotionEventActions.Up or MotionEventActions.Cancel)
        {
            _gestureTarget = null;
            _swallowGesture = false;
        }

        return false;
    }

    /// <summary>
    /// Dispatches a copy of the event to the root view of another window, translated into that window's coordinates.
    /// </summary>
    /// <param name="windowRoot">Root view of the target window.</param>
    /// <param name="e">The motion event received by the dialog window.</param>
    private static void DispatchToWindow(View windowRoot, MotionEvent e)
    {
        int[] location = new int[2];
        windowRoot.GetLocationOnScreen(location);

        MotionEvent copy = MotionEvent.Obtain(e);
        copy.OffsetLocation(e.RawX - e.GetX() - location[0], e.RawY - e.GetY() - location[1]);

        _ = windowRoot.DispatchTouchEvent(copy);

        copy.Recycle();
    }

    /// <summary>
    /// Finds the topmost visible sub-window (for example a popup menu) of this app.
    /// </summary>
    /// <param name="rawX">Screen x coordinate.</param>
    /// <param name="rawY">Screen y coordinate.</param>
    /// <param name="containsPoint">
    /// True to find the sub-window that contains the point.
    /// False to find a touch-modal sub-window that does not contain it.
    /// </param>
    /// <returns>The root view of the sub-window, or null if there is none.</returns>
    private View? FindSubWindow(float rawX, float rawY, bool containsPoint)
    {
        // WindowInspector (API 29+) is the only public way to list the app's windows.
        if (OperatingSystem.IsAndroidVersionAtLeast(29) == false)
        {
            return null;
        }

        IList<View> windowRoots = WindowInspector.GlobalWindowViews;
        View? dialogRoot = _dialog.Window?.DecorView;
        int[] location = new int[2];

        // Windows are listed in the order they were added, so walk backwards to find the topmost one.
        for (int i = windowRoots.Count - 1; i >= 0; i--)
        {
            View root = windowRoots[i];

            if (root == dialogRoot
                || root.IsShown == false
                || root.LayoutParameters is not WindowManagerLayoutParams layoutParams
                || (int)layoutParams.Type < FirstSubWindowType
                || (int)layoutParams.Type > LastSubWindowType)
            {
                continue;
            }

            root.GetLocationOnScreen(location);

            bool hit = rawX >= location[0]
                && rawX < location[0] + root.Width
                && rawY >= location[1]
                && rawY < location[1] + root.Height;

            if (containsPoint && hit)
            {
                return root;
            }

            if (containsPoint == false
                && hit == false
                && IsTouchModal(layoutParams))
            {
                return root;
            }
        }

        return null;
    }

    /// <summary>
    /// Gets whether the window manager sends a window the touches outside its bounds.
    /// </summary>
    /// <param name="layoutParams">The window's layout parameters.</param>
    /// <returns>True if the window is touch-modal.</returns>
    private static bool IsTouchModal(WindowManagerLayoutParams layoutParams)
        => (layoutParams.Flags & (WindowManagerFlags.NotFocusable | WindowManagerFlags.NotTouchModal)) == 0;
}
