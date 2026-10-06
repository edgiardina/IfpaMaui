using Ifpa.ViewModels;
using Ifpa.Models;
using Microsoft.Maui.Maps;
using Microsoft.Maui.Controls.Maps;
using Microsoft.Extensions.Logging;
using CommunityToolkit.Mvvm.Messaging;
using Plugin.Maui.NativeContextMenus;

namespace Ifpa.Views
{
    [XamlCompilation(XamlCompilationOptions.Compile)]
    public partial class CalendarPage : ContentPage
    {
        public CalendarViewModel ViewModel { get; set; }

        private readonly ILogger<CalendarPage> logger;

        private static readonly int[] Distances = [25, 50, 100, 150, 250];
        private static readonly string[] RankingSystems = ["All", "Main", "Women"];

        private readonly Dictionary<int, MenuNode> distanceNodes = new();
        private readonly Dictionary<string, MenuNode> rankingSystemNodes = new();
        private readonly MenuNode locationNode = new();
        private readonly MenuNode distanceMenu = new();
        private readonly MenuNode showLeaguesNode = new() { Title = Strings.CalendarFilterModalPage_ShowLeagues, IsCheckable = true };

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

            BuildFilterMenu();
        }

        protected override void OnAppearing()
        {
            base.OnAppearing();

            // Earlier versions let the distance go to 1000 miles, which loads hundreds of events
            if (Settings.LastCalendarDistance > Distances[^1])
            {
                Settings.LastCalendarDistance = Distances[^1];
            }

            _ = ReloadCalendar();
        }

        private void BuildFilterMenu()
        {
            locationNode.Tapped += async (_, _) => await PromptForLocation();

            var myLocationNode = new MenuNode { Title = Strings.CalendarPage_MyLocation };
            myLocationNode.Tapped += async (_, _) => await UseMyLocation();

            foreach (var distance in Distances)
            {
                distanceNodes[distance] = CreateRadioNode($"{distance} {Strings.Miles_Abbreviation}", "Distance", () => Settings.LastCalendarDistance = distance);
                distanceMenu.Children.Add(distanceNodes[distance]);
            }

            var locationSection = new MenuNode();
            locationSection.Children.Add(locationNode);
            locationSection.Children.Add(myLocationNode);
            locationSection.Children.Add(distanceMenu);

            var rankingSystemSection = new MenuNode();
            foreach (var system in RankingSystems)
            {
                rankingSystemNodes[system] = CreateRadioNode(system, "RankingSystem", () => Settings.CalendarRankingSystem = system);
                rankingSystemSection.Children.Add(rankingSystemNodes[system]);
            }

            // The plugin changes IsChecked before it raises Tapped
            showLeaguesNode.Tapped += async (_, _) =>
            {
                Settings.CalendarShowLeagues = showLeaguesNode.IsChecked;
                await ApplyFilter();
            };

            var showLeaguesSection = new MenuNode();
            showLeaguesSection.Children.Add(showLeaguesNode);

            FilterMenu.Items.Add(locationSection);
            FilterMenu.Items.Add(rankingSystemSection);
            FilterMenu.Items.Add(showLeaguesSection);

            UpdateFilterMenu();
        }

        private MenuNode CreateRadioNode(string title, string groupKey, Action select)
        {
            var node = new MenuNode { Title = title, GroupKey = groupKey, IsCheckable = true };
            node.Tapped += async (_, _) =>
            {
                select();
                await ApplyFilter();
            };
            return node;
        }

        // Sets the menu to the saved filter. The plugin reads the nodes each time the menu opens.
        private void UpdateFilterMenu()
        {
            locationNode.Title = $"{Strings.CalendarFilterModalPage_Location}: {Settings.LastCalendarLocation}";
            distanceMenu.Title = $"{Strings.CalendarFilterModalPage_Distance}: {Settings.LastCalendarDistance} {Strings.Miles_Abbreviation}";

            foreach (var (distance, node) in distanceNodes)
                node.IsChecked = distance == Settings.LastCalendarDistance;
            foreach (var (system, node) in rankingSystemNodes)
                node.IsChecked = system == Settings.CalendarRankingSystem;

            showLeaguesNode.IsChecked = Settings.CalendarShowLeagues;
        }

        private async Task PromptForLocation()
        {
            var location = await DisplayPromptAsync(
                Strings.CalendarFilterModalPage_SetCalendarLocation,
                null,
                Strings.OK,
                Strings.Cancel,
                placeholder: "Chicago, Illinois",
                initialValue: Settings.LastCalendarLocation);

            if (string.IsNullOrWhiteSpace(location))
                return;

            Settings.LastCalendarLocation = location.Trim();
            await ApplyFilter();
        }

        private async Task UseMyLocation()
        {
            var status = await Permissions.CheckStatusAsync<Permissions.LocationWhenInUse>();
            if (status != PermissionStatus.Granted)
            {
                status = await Permissions.RequestAsync<Permissions.LocationWhenInUse>();
            }

            if (status != PermissionStatus.Granted)
            {
                await DisplayAlertAsync(Strings.PermissionRequired, "IFPA Companion requires your permission before polling your location for Calendar Search", Strings.OK);
                return;
            }

            try
            {
                var location = await Geolocation.GetLastKnownLocationAsync();
                var placemark = (await Geocoding.GetPlacemarksAsync(location)).First();

                Settings.LastCalendarLocation = placemark.Locality + ", " + placemark.AdminArea;
            }
            catch (Exception e)
            {
                logger.LogError(e, "Error finding the current location for the calendar filter");
                return;
            }

            await ApplyFilter();
        }

        private async Task ApplyFilter()
        {
            UpdateFilterMenu();
            WeakReferenceMessenger.Default.Send(new CalendarFilterChangedMessage());
            await ReloadCalendar();
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