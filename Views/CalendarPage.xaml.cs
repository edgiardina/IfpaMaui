using Ifpa.ViewModels;
using Ifpa.Models;
using Microsoft.Maui.Maps;
using Microsoft.Maui.Controls.Maps;
using MauiIcons.Fluent;
using MauiIcons.Core;
using Microsoft.Extensions.Logging;

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

        protected override void OnAppearing()
        {
            base.OnAppearing();
            UpdateCalendarData();
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

                Location geoLocation;

                try
                {
                    geoLocation = (await Geocoding.GetLocationsAsync(Settings.LastCalendarLocation)).First();
                }
                catch (Exception e)
                {
                    logger.LogWarning(e, "Error geolocating");

                    geoLocation = await Geolocation.GetLastKnownLocationAsync();
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