using Ifpa.ViewModels;
using System.Diagnostics;
using PinballApi.Models.WPPR.v1.Calendar;
using Ifpa.Models;
using Syncfusion.Maui.Scheduler;
using Microsoft.Maui.Maps;
using Microsoft.Maui.Controls.Maps;
using MauiIcons.Fluent;
using Microsoft.Maui.Controls;

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
        }

        protected async override void OnAppearing()
        {
            base.OnAppearing();

            if (ViewModel.CalendarDetails.Count == 0)
            {                
                await UpdateCalendarData();
            }

            //TODO: Get resource dictionary by name not by order in list
            var colorResources = App.Current.Resources.MergedDictionaries.First();

            //TODO: This should be in the XAML since it's UI / markup / descriptive not logic.
            calendar.HeaderView.TextStyle.SetAppTheme(SchedulerTextStyle.TextColorProperty,
                colorResources["PrimaryTextColor"],
                colorResources["PrimaryTextColorDark"]);


            //TODO: actually restrict calendar not to go to months in the past.
        }

        private async void MyLocation_Clicked(object sender, EventArgs e)
        {
            var filterPage = new CalendarFilterModalPage();

            filterPage.FilterSaved += async () => { await UpdateCalendarData(); };

            await Navigation.PushModalAsync(filterPage);
        }

        private async Task UpdateCalendarData()
        {
            if (!IsBusy)
            {
                try
                {
                    var location = Preferences.Get("LastCalendarLocation", "Chicago, Il");
                    var distance = Preferences.Get("LastCalendarDistance", 150);

                    Preferences.Set("LastCalendarLocation", location);
                    Preferences.Set("LastCalendarDistance", distance);
                    
                    calendarMap.Pins.Clear();

                    var geoLocation = await Geocoding.GetLocationsAsync(location);
                    calendarMap.MoveToRegion(MapSpan.FromCenterAndRadius(new Location(geoLocation.First().Latitude, geoLocation.First().Longitude), 
                                                                            Distance.FromMiles(distance)));
                    
                    await ViewModel.ExecuteLoadItemsCommand(location, distance);

                    List<Task> listOfTasks = new List<Task>();
                    foreach (var detail in ViewModel.CalendarDetails)
                    {
                       listOfTasks.Add(LoadEventOntoCalendar(detail));
                    }
                    await Task.WhenAll(listOfTasks);
                }
                catch (Exception e)
                {
                    //don't let the calendar crash our entire app
                    Debug.WriteLine(e.Message);
                }

                IsBusy = false;
            }
        }

        private async Task LoadEventOntoCalendar(CalendarDetails detail)
        {
            var pin = new Pin();
            var locations = await Geocoding.GetLocationsAsync(detail.Address1 + " " + detail.City + ", " + detail.State);
            pin.Location = new Location(locations.First().Latitude, locations.First().Longitude);
            pin.Label = detail.TournamentName;
            pin.Type = PinType.Place;

            //TODO: on pinpress scroll listview to find item. 
            pin.MarkerClicked += (sender, e) =>
            {
                TournamentListView.ScrollTo(ViewModel.CalendarDetails.IndexOf(detail), position: ScrollToPosition.MakeVisible, animate: true);
            };

            calendarMap.Pins.Add(pin);
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
            if(e.Appointments != null && e.Appointments.Any())
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
    }
}