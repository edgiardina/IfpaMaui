using System.Collections.Specialized;
using Ifpa.ViewModels;
using PinballApi.Models.WPPR.Universal.Rankings;
using Plugin.Maui.NativeContextMenus;

namespace Ifpa.Views
{
    [XamlCompilation(XamlCompilationOptions.Compile)]
    public partial class RankingsPage : ContentPage
    {
        RankingsViewModel ViewModel;

        public RankingsPage(RankingsViewModel viewModel)
        {
            InitializeComponent();

            BindingContext = ViewModel = viewModel;

            // The country list comes from the API, and a menu node cannot bind to a list
            AddCountryNodes(viewModel.Countries);
            viewModel.Countries.CollectionChanged += Countries_CollectionChanged;
        }

        void Countries_CollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            if (e.Action == NotifyCollectionChangedAction.Add)
            {
                AddCountryNodes(e.NewItems.Cast<Country>());
            }
            else
            {
                CountryMenu.Children.Clear();
                AddCountryNodes(ViewModel.Countries);
            }
        }

        void AddCountryNodes(IEnumerable<Country> countries)
        {
            foreach (var country in countries)
            {
                CountryMenu.Children.Add(new MenuNode { Title = country.CountryName, Value = country.CountryName });
            }
        }

        protected override void OnAppearing()
        {
            if (ViewModel.Players.Count == 0)
            {
                ViewModel.RestoreFilter();
                _ = ViewModel.Reload();
            }
            base.OnAppearing();
        }
    }
}
