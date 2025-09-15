using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Ifpa.Views
{
    public class MoreItemsMenuItem : BindableObject
    {
        public int Id { get; set; }

        public static readonly BindableProperty TitleProperty =
            BindableProperty.Create(nameof(Title), typeof(string), typeof(MoreItemsMenuItem), default(string));

        public string Title
        {
            get { return (string)GetValue(TitleProperty); }
            set { SetValue(TitleProperty, value); }
        }

        public string Route { get; set; }

        public ImageSource IconSource { get; set; }
    }
}