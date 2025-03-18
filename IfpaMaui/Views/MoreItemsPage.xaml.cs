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

        private async void MoreItemsCollection_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (e.CurrentSelection != null && e.CurrentSelection.Count > 0)
            {
                await Shell.Current.GoToAsync(((MoreItemsMenuItem)e.CurrentSelection.FirstOrDefault()).Route);

                ((CollectionView)sender).SelectedItem = null;     
            }
        }
    }
}