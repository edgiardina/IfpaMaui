using Ifpa.ViewModels;
using System.Diagnostics;
using PinballApi.Models.WPPR.v1.Calendar;
using Ifpa.Models;
using Microsoft.Maui.Maps;
using Microsoft.Maui.Controls.Maps;
using MauiIcons.Fluent;
using Serilog.Core;
using Serilog;
using MauiIcons.Core;

namespace Ifpa.Views
{
    public enum CalendarPageView
    {
        MapAndList,
        Calendar
    }


    [XamlCompilation(XamlCompilationOptions.Compile)]
    public partial class CalendarPage : ContentPage
    {
        public CalendarViewModel ViewModel { get; set; }

        public CalendarPageView View { get; set; }

        public CalendarPage(CalendarViewModel viewModel)
        {
            InitializeComponent();

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
                                                                        Microsoft.Maui.Maps.Distance.FromMiles(Settings.LastCalendarDistance)));
                map.ItemTemplate = PinDataTemplate;
                map.ItemsSource = ViewModel.Pins;

                mapShim.Children.Add(map);

                await ViewModel.ExecuteLoadItemsCommand(Settings.LastCalendarLocation, Settings.LastCalendarDistance);
            }
            catch (Exception e)
            {
                //don't let the calendar crash our entire app
                //TODO: dependency inject this?
                Log.Logger.Error(e, "Error loading calendar data");
            }
        }

        private void ToggleView_Clicked(object sender, EventArgs e)
        {
            var colorDictionary = Application.Current.Resources.MergedDictionaries.First();
            var toolbarIconColor = (Color)colorDictionary["IconAccentColor"];

            if (View == CalendarPageView.Calendar)
            {
                MapLayout.IsVisible = true;
                calendar.IsVisible = false;
                View = CalendarPageView.MapAndList;
                ToolbarItems.SingleOrDefault(n => n.Text == "Toggle View").IconImageSource = (FontImageSource)new MauiIcon() { Icon = FluentIcons.CalendarLtr28, IconColor = toolbarIconColor };
            }
            else
            {
                MapLayout.IsVisible = false;
                calendar.IsVisible = true;
                View = CalendarPageView.Calendar;
                ToolbarItems.SingleOrDefault(n => n.Text == "Toggle View").IconImageSource = (FontImageSource)new MauiIcon() { Icon = FluentIcons.Map24, IconColor = toolbarIconColor };
            }
        }

        private async void calendar_Tapped(object sender, Syncfusion.Maui.Scheduler.SchedulerTappedEventArgs e)
        {
            if (e.Appointments != null && e.Appointments.Any())
            {
                var calendar = e.Appointments.First() as InlineCalendarItem;
                if (calendar == null)
                    return;

                await Shell.Current.GoToAsync($"calendar-detail?calendarId={calendar.CalendarId}");
            }
        }

        private async void TournamentListView_SelectionChanged(object sender, SelectionChangedEventArgs e)
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