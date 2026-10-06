using Ifpa.ViewModels;
using Ifpa.Models;
using Microsoft.Maui.Maps;
using Microsoft.Maui.Controls.Maps;
using Microsoft.Extensions.Logging;

namespace Ifpa.Views
{
    [XamlCompilation(XamlCompilationOptions.Compile)]
    public partial class CalendarPage : ContentPage
    {
        public CalendarViewModel ViewModel { get; set; }

        private readonly ILogger<CalendarPage> logger;

        private bool isLoading;
        private bool isReloadPending;

        public CalendarPage(CalendarViewModel viewModel, ILogger<CalendarPage> logger)
        {
            InitializeComponent();

            // On iPad the native month does not grow with the view, and the adjacent months show
            // in the extra width. Keep the view as wide as one month.
            if (DeviceInfo.Platform == DevicePlatform.iOS && DeviceInfo.Idiom == DeviceIdiom.Tablet)
            {
                nativeCalendar.MaximumWidthRequest = 400;
            }

            this.logger = logger;

            BindingContext = ViewModel = viewModel;
            viewModel.IsBusy = true;

            viewModel.FilterChanged += async (_, _) => await ReloadCalendar();
        }

        protected override void OnAppearing()
        {
            base.OnAppearing();
            _ = ReloadCalendar();
        }

        // A filter change during a load starts one more load, so the calendar always shows the last filter
        private async Task ReloadCalendar()
        {
            isReloadPending = true;
            if (isLoading)
                return;

            isLoading = true;
            try
            {
                while (isReloadPending)
                {
                    isReloadPending = false;
                    await UpdateCalendarData();
                }
            }
            finally
            {
                isLoading = false;
            }
        }

        private async Task UpdateCalendarData()
        {
            try
            {
                mapShim.Children.Clear();

                Location geoLocation = null;

                // Default to Chicago if we can't get a location
                var defaultLocation = new Location(41.8781, -87.6298);

                var locationPermission = await Permissions.CheckStatusAsync<Permissions.LocationWhenInUse>();

                try
                {
                    geoLocation = (await Geocoding.GetLocationsAsync(Settings.LastCalendarLocation)).First();
                }
                catch (Exception e)
                {
                    logger.LogWarning(e, "Error geolocating");

                    if (locationPermission == PermissionStatus.Granted)
                    {
                        geoLocation = await Geolocation.GetLastKnownLocationAsync();

                    }

                    if (geoLocation == null)
                    {
                        geoLocation = defaultLocation;
                    }
                }

                var mapSpan = MapSpan.FromCenterAndRadius(new Location(geoLocation.Latitude, geoLocation.Longitude),
                                                                        Distance.FromMiles(Settings.LastCalendarDistance));

                var map = new Microsoft.Maui.Controls.Maps.Map(mapSpan);

                map.ItemTemplate = PinDataTemplate;
                map.SetBinding(Microsoft.Maui.Controls.Maps.Map.ItemsSourceProperty, new Binding(nameof(ViewModel.Pins), source: ViewModel));

                mapShim.Children.Add(map);

                await ViewModel.LoadItems(geoLocation, Settings.LastCalendarDistance);

                // For whatever reason Android on re-load via modal doesn't re-center the map.
                map.MoveToRegion(mapSpan);
            }
            catch (Exception e)
            {
                //don't let the calendar crash our entire app
                logger.LogError(e, "Error loading calendar data");
            }
        }

        // TODO: Pin Markers and Info Windows don't currently support commanding
        private void Pin_MarkerClicked(object sender, PinClickedEventArgs e)
        {
            var pin = (Pin)sender;
            var calendarItem = ViewModel.Tournaments.FirstOrDefault(n => n.TournamentName == pin.Label && n.Latitude == pin.Location.Latitude && n.Longitude == pin.Location.Longitude);
            TournamentListView.ScrollTo(calendarItem, position: ScrollToPosition.Start, animate: true);
        }

        private async void Pin_InfoWindowClicked(object sender, PinClickedEventArgs e)
        {
            var pin = (Pin)sender;
            var calendarItem = ViewModel.Tournaments.FirstOrDefault(n => n.TournamentName == pin.Label && n.Latitude == pin.Location.Latitude && n.Longitude == pin.Location.Longitude);

            await Shell.Current.GoToAsync($"calendar-detail?tournamentId={calendarItem.TournamentId}");
        }
    }
}