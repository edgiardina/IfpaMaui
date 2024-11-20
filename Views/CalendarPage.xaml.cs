using Ifpa.ViewModels;
using Ifpa.Models;
using Microsoft.Maui.Maps;
using Microsoft.Maui.Controls.Maps;
using MauiIcons.Fluent;
using MauiIcons.Core;
using Microsoft.Extensions.Logging;
using TournamentSearch = PinballApi.Models.WPPR.Universal.Tournaments.Search.Tournament;

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
                map.ItemsSource = ViewModel.Pins;

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
            var tournament = e.CurrentSelection.FirstOrDefault() as TournamentSearch;
            if (tournament == null)
                return;

            await Shell.Current.GoToAsync($"calendar-detail?tournamentId={tournament.TournamentId}");

            // Manually deselect item.
            TournamentListView.SelectedItem = null;
        }

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