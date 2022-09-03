using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Ifpa.Views
{
    [XamlCompilation(XamlCompilationOptions.Compile)]
    public partial class MoreItemsPage : ContentPage
    {
        public MoreItemsPage()
        {
            InitializeComponent();
        }

        private async void ListView_ItemSelected(object sender, SelectedItemChangedEventArgs e)
        {
            if (listView.SelectedItem != null)
            {
                await Shell.Current.GoToAsync(((MoreItemsMenuItem)e.SelectedItem).Route);

                listView.SelectedItem = null;
            }
        }
    }
}