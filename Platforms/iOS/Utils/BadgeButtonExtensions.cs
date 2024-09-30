using CoreAnimation;
using CoreGraphics;
using Foundation;
using ObjCRuntime;
using System.Runtime.InteropServices;
using UIKit;

namespace Ifpa.Platforms.Utils
{
    public static class BarButtonItemExtensions
    {
        public static void AddBadge(this UIBarButtonItem barButtonItem, int value, UIColor backgroundColor = null, UIColor textColor = null)
        {
            // Remove any existing badge
            RemoveBadge(barButtonItem);

            // Create the badge view
            var badge = new UILabel
            {
                Text = value.ToString(),
                TextColor = textColor ?? UIColor.White,
                BackgroundColor = backgroundColor ?? UIColor.Red,
                TextAlignment = UITextAlignment.Center,
                Font = UIFont.BoldSystemFontOfSize(11),
                ClipsToBounds = true, // Removed as you pointed out
                Layer = { CornerRadius = 9 }, // 18px height, so half is 9 for circular badge
                Frame = new CGRect(0, 0, 18, 18), // Set frame explicitly
                TranslatesAutoresizingMaskIntoConstraints = false                
            };

            // Ensure the barButtonItem's view is available
            if (barButtonItem.ValueForKey(new NSString("view")) is UIView barButtonView)
            {
                // Add the badge as a subview of the barButtonItem's view
                barButtonView.AddSubview(badge);

                NSLayoutConstraint.ActivateConstraints(new[]
                {
                    badge.TopAnchor.ConstraintEqualTo(barButtonView.TopAnchor, 3), // Adjust position
                    badge.RightAnchor.ConstraintEqualTo(barButtonView.RightAnchor, 5), // Adjust position
                    badge.WidthAnchor.ConstraintEqualTo(18),
                    badge.HeightAnchor.ConstraintEqualTo(18)
                });

                barButtonView.BringSubviewToFront(badge); // Ensure badge is on top

                // Debugging: Print the frame to check position
                Console.WriteLine($"Badge Frame: {badge.Frame}");
                Console.WriteLine($"Bar Button View Frame: {barButtonView.Frame}");
            }
            else
            {
                Console.WriteLine("Could not find the UIBarButtonItem view.");
            }
        }

        public static void RemoveBadge(this UIBarButtonItem barButtonItem)
        {
            // Ensure barButtonItem's view is available
            if (barButtonItem.ValueForKey(new NSString("view")) is UIView barButtonView)
            {
                // Find the badge (if any) and remove it
                foreach (var subview in barButtonView.Subviews)
                {
                    if (subview is UILabel badge && badge.BackgroundColor == UIColor.Red)
                    {
                        badge.RemoveFromSuperview();
                    }
                }
            }
        }

    }
}
