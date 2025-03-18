using Microsoft.Maui.Controls.Handlers.Compatibility;
using System.Drawing;
using UIKit;

namespace Ifpa.Platforms.Renderers
{
    class InsetTableViewRenderer : TableViewRenderer
    {
        public InsetTableViewRenderer()
        {
            
        }

        protected override UITableView CreateNativeControl()
        {
            return new UITableView(RectangleF.Empty, GetTableViewStyle(Element?.Intent ?? TableIntent.Data));
        }

        protected new UITableViewStyle GetTableViewStyle(TableIntent intent)
        {
            return UITableViewStyle.InsetGrouped;
        }

    }
}
