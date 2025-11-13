#if IOS
using CoreGraphics;
using Foundation;
using Microsoft.Maui.Handlers;
using UIKit;
using Ifpa.Controls;
using Microsoft.Maui.Platform;
using PlatformContentView = Microsoft.Maui.Platform.ContentView;

namespace Ifpa.Platforms.Handlers
{
    /// <summary>
    /// Handler that wraps ContentView content in a UITableView with inset grouped style
    /// </summary>
    public class InsetTableViewHandler : ContentViewHandler
    {
        private new static readonly IPropertyMapper<InsetTableView, InsetTableViewHandler> Mapper = 
            new PropertyMapper<InsetTableView, InsetTableViewHandler>(ViewHandler.ViewMapper)
            {
                [nameof(InsetTableView.SectionTitle)] = MapSectionTitle
            };

        private UITableView _tableView;
        private InsetTableViewCell _contentCell;

        public InsetTableViewHandler() : base(Mapper)
        {
        }

        protected override PlatformContentView CreatePlatformView()
        {
            // Create a wrapper view that contains the table
            var wrapperView = new PlatformContentView();
            
            _tableView = new UITableView(CGRect.Empty, UITableViewStyle.InsetGrouped)
            {
                BackgroundColor = UIColor.SystemGroupedBackground,
                ShowsVerticalScrollIndicator = false,
                SeparatorStyle = UITableViewCellSeparatorStyle.SingleLine,
                SeparatorColor = UIColor.Separator,
                AllowsSelection = false,
                ScrollEnabled = false // Disable scrolling since it's inside a ScrollView
            };

            _tableView.Source = new InsetTableViewSource(VirtualView as InsetTableView);
            
            return wrapperView;
        }

        protected override void ConnectHandler(PlatformContentView platformView)
        {
            base.ConnectHandler(platformView);
            
            if (_tableView != null && platformView != null)
            {
                // Add table view to the platform view
                _tableView.TranslatesAutoresizingMaskIntoConstraints = false;
                platformView.AddSubview(_tableView);
                
                NSLayoutConstraint.ActivateConstraints(new[]
                {
                    _tableView.TopAnchor.ConstraintEqualTo(platformView.TopAnchor),
                    _tableView.LeadingAnchor.ConstraintEqualTo(platformView.LeadingAnchor),
                    _tableView.TrailingAnchor.ConstraintEqualTo(platformView.TrailingAnchor),
                    _tableView.BottomAnchor.ConstraintEqualTo(platformView.BottomAnchor)
                });
                
                _tableView.ReloadData();
            }
        }

        protected override void DisconnectHandler(PlatformContentView platformView)
        {
            _tableView?.RemoveFromSuperview();
            _tableView = null;
            _contentCell = null;
            
            base.DisconnectHandler(platformView);
        }

        public static void MapSectionTitle(InsetTableViewHandler handler, InsetTableView tableView)
        {
            handler._tableView?.ReloadData();
        }
    }

    /// <summary>
    /// UITableViewSource that renders the MAUI content in a single cell
    /// </summary>
    public class InsetTableViewSource : UITableViewSource
    {
        private readonly InsetTableView _tableView;

        public InsetTableViewSource(InsetTableView tableView)
        {
            _tableView = tableView;
        }

        public override nint NumberOfSections(UITableView tableView)
        {
            return 1;
        }

        public override nint RowsInSection(UITableView tableview, nint section)
        {
            return 1; // Single cell containing all the MAUI content
        }

        public override string TitleForHeader(UITableView tableView, nint section)
        {
            return _tableView?.SectionTitle;
        }

        public override UIView GetViewForHeader(UITableView tableView, nint section)
        {
            if (string.IsNullOrEmpty(_tableView?.SectionTitle))
                return null;

            var headerView = new UIView(new CGRect(0, 0, tableView.Frame.Width, 40));
            headerView.BackgroundColor = UIColor.Clear;

            var label = new UILabel(new CGRect(20, 20, tableView.Frame.Width - 40, 20))
            {
                Text = _tableView.SectionTitle.ToUpper(),
                Font = UIFont.SystemFontOfSize(13, UIFontWeight.Regular),
                TextColor = UIColor.SecondaryLabel,
                BackgroundColor = UIColor.Clear
            };

            headerView.AddSubview(label);
            return headerView;
        }

        public override nfloat GetHeightForHeader(UITableView tableView, nint section)
        {
            return string.IsNullOrEmpty(_tableView?.SectionTitle) ? 0 : 40;
        }

        public override UITableViewCell GetCell(UITableView tableView, NSIndexPath indexPath)
        {
            var cell = new InsetTableViewCell(_tableView);
            cell.BackgroundColor = UIColor.SecondarySystemGroupedBackground;
            cell.SelectionStyle = UITableViewCellSelectionStyle.None;
            return cell;
        }

        public override nfloat GetHeightForRow(UITableView tableView, NSIndexPath indexPath)
        {
            return UITableView.AutomaticDimension;
        }
    }

    /// <summary>
    /// UITableViewCell that hosts the MAUI content
    /// </summary>
    public class InsetTableViewCell : UITableViewCell
    {
        public InsetTableViewCell(InsetTableView mauiView) : base(UITableViewCellStyle.Default, "InsetCell")
        {
            if (mauiView?.Content != null)
            {
                // Convert MAUI content to native iOS view
                var handler = mauiView.Content.ToHandler(mauiView.Handler?.MauiContext);
                var nativeView = handler?.PlatformView as UIView;
                
                if (nativeView != null)
                {
                    ContentView.AddSubview(nativeView);
                    nativeView.TranslatesAutoresizingMaskIntoConstraints = false;
                    
                    NSLayoutConstraint.ActivateConstraints(new[]
                    {
                        nativeView.TopAnchor.ConstraintEqualTo(ContentView.TopAnchor),
                        nativeView.LeadingAnchor.ConstraintEqualTo(ContentView.LeadingAnchor),
                        nativeView.TrailingAnchor.ConstraintEqualTo(ContentView.TrailingAnchor),
                        nativeView.BottomAnchor.ConstraintEqualTo(ContentView.BottomAnchor)
                    });
                }
            }
        }
    }
}
#endif