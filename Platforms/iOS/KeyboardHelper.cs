using UIKit;

namespace Ifpa.Platforms
{
    public static partial class KeyboardHelper
    {
        public static void HideKeyboard()
        {
            UIApplication.SharedApplication.Delegate.GetWindow().EndEditing(true);
        }
    }
}
