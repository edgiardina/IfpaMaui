using Ifpa.ViewModels;
using PinballApi.Models.WPPR.v1.Calendar;
using Ifpa.Models;
using Microsoft.Maui.Maps;
using Microsoft.Maui.Controls.Maps;
using MauiIcons.Fluent;
using MauiIcons.Core;
using Microsoft.Extensions.Logging;
using Syncfusion.Maui.Core.Carousel;

namespace Ifpa.Views
{
    [XamlCompilation(XamlCompilationOptions.Compile)]
    public partial class CalendarPage : ContentPage
    {
        public CalendarViewModel ViewModel { get; set; }


        private readonly ILogger<CalendarPage> logger;

        public CalendarPage(CalendarViewModel viewModel, ILogger<CalendarPage> logger)
        {
            InitializeComponent();

            this.logger = logger;

            BindingContext = ViewModel = viewModel;
            viewModel.IsBusy = true;
        }

        protected override async void OnNavigatedTo(NavigatedToEventArgs args)
        {
            base.OnNavigatedTo(args);

            if (ViewModel.CalendarDetails.Count == 0)
            {
                await UpdateCalendarData();
            }
        }

        private async void MyLocation_Clicked(object sender, EventArgs e)
        {
            var filterPage = new CalendarFilterModalPage();

            filterPage.FilterSaved += async () => { await UpdateCalendarData(); };

            await Navigation.PushModalAsync(filterPage);
        }

        private async Task UpdateCalendarData()
        {
            try
            {
                mapShim.Children.Clear();
                var geoLocation = await Geocoding.GetLocationsAsync(Settings.LastCalendarLocation);
                var map = new Microsoft.Maui.Controls.Maps.Map(MapSpan.FromCenterAndRadius(new Location(geoLocation.First().Latitude, geoLocation.First().Longitude),
                                                                        Distance.FromMiles(Settings.LastCalendarDistance)));
                map.ItemTemplate = PinDataTemplate;
                map.ItemsSource = ViewModel.Pins;

                mapShim.Children.Add(map);

                await ViewModel.ExecuteLoadItemsCommand(Settings.LastCalendarLocation, Settings.LastCalendarDistance);
            }
            catch (Exception e)
            {
                //don't let the calendar crash our entire app
                logger.LogError(e, "Error loading calendar data");
            }
        }

        // TODO: This should be in a trigger in the XAML but toolbaritems don't support triggers yet
        private void ToggleView_Clicked(object sender, EventArgs e)
        {
            var colorDictionary = Application.Current.Resources.MergedDictionaries.First();
            var toolbarIconColor = (Color)colorDictionary["IconAccentColor"];
            var vm = (CalendarViewModel) BindingContext;

            if (vm.CurrentType == CalendarType.MapAndList)
            {
                ToolbarItems.SingleOrDefault(n => n.Text == Strings.CalendarPage_ToggleView).IconImageSource = (FontImageSource)new MauiIcon() { Icon = FluentIcons.CalendarLtr28, IconColor = toolbarIconColor };
            }
            else
            {
                ToolbarItems.SingleOrDefault(n => n.Text == Strings.CalendarPage_ToggleView).IconImageSource = (FontImageSource)new MauiIcon() { Icon = FluentIcons.Map24, IconColor = toolbarIconColor };
            }
        }

        private async void TournamentListView_SelectionChanged(object sender, Microsoft.Maui.Controls.SelectionChangedEventArgs e)
        {
            var calendar = e.CurrentSelection.FirstOrDefault() as CalendarDetails;
            if (calendar == null)
                return;

            await Shell.Current.GoToAsync($"calendar-detail?calendarId={calendar.CalendarId}");

            // Manually deselect item.
            TournamentListView.SelectedItem = null;
        }

        private void Pin_MarkerClicked(object sender, PinClickedEventArgs e)
        {
            var pin = (Pin)sender;
            var calendarItem = ViewModel.CalendarDetails.FirstOrDefault(n => n.TournamentName == pin.Label && n.Latitude == pin.Location.Latitude && n.Longitude == pin.Location.Longitude);
            TournamentListView.ScrollTo(calendarItem, position: ScrollToPosition.Start, animate: true);
        }

        private async void Pin_InfoWindowClicked(object sender, PinClickedEventArgs e)
        {
            var pin = (Pin)sender;
            var calendarItem = ViewModel.CalendarDetails.FirstOrDefault(n => n.TournamentName == pin.Label && n.Latitude == pin.Location.Latitude && n.Longitude == pin.Location.Longitude);

            await Shell.Current.GoToAsync($"calendar-detail?calendarId={calendarItem.CalendarId}");
        }
    }
}