﻿using Ifpa.Models;
using Ifpa.Services;
using Plugin.Maui.CalendarStore;
using Serilog;

namespace Ifpa.Views
{
    [XamlCompilation(XamlCompilationOptions.Compile)]
    public partial class CalendarFilterModalPage : ContentPage
    {
        public delegate void FilterSavedDelegate();

        public event FilterSavedDelegate FilterSaved;
        private readonly ICalendarSyncService CalendarSyncService;

        public CalendarFilterModalPage(ICalendarSyncService calendarSyncService)
        {
            InitializeComponent();

            CalendarSyncService = calendarSyncService;

            // TODO: bindings instead of explicit assignment
            var lastCalendarLocation = Settings.LastCalendarLocation;
            var lastCalendarDistance = Settings.LastCalendarDistance;
            var lastCalendarRankingSystem = Settings.CalendarRankingSystem;

            DistanceSlider.Value = lastCalendarDistance;
            LocationEntry.Text = lastCalendarLocation;
            DistanceText.Text = ((int)DistanceSlider.Value).ToString();
            RankingTypePicker.SelectedItem = lastCalendarRankingSystem;
            ShowLeaguesCheckBox.IsChecked = Settings.CalendarShowLeagues;
        }

        private void Slider_ValueChanged(object sender, ValueChangedEventArgs e)
        {
            DistanceText.Text = ((int)DistanceSlider.Value).ToString();
        }

        private async Task PollAndUpdateUserLocation()
        {
            if (!IsBusy)
            {
                try
                {
                    IsBusy = true;
                    var location = await Geolocation.GetLastKnownLocationAsync();

                    var placemarks = await Geocoding.GetPlacemarksAsync(location);

                    var placemark = placemarks.First();
                    LocationEntry.Text = placemark.Locality + ", " + placemark.AdminArea;
                }
                catch (Exception e)
                {
                    //TODO: dependency inject this
                    Log.Logger.Error(e, "Error loading calendar filter data");
                }

                IsBusy = false;
            }
        }

        private async void ImageButton_Clicked(object sender, EventArgs e)
        {
            var status = await Permissions.CheckStatusAsync<Permissions.LocationWhenInUse>();
            if (status != PermissionStatus.Granted)
            {
                status = await Permissions.RequestAsync<Permissions.LocationWhenInUse>();
            }

            if (status == PermissionStatus.Granted)
            {
                await PollAndUpdateUserLocation();
            }
            else
            {
                await DisplayAlert(Strings.PermissionRequired, "IFPA Companion requires your permission before polling your location for Calendar Search", Strings.OK);
            }
        }

        private async void CancelButton_Clicked(object sender, EventArgs e)
        {
            await Navigation.PopModalAsync();
        }

        private async void FindButton_Clicked(object sender, EventArgs e)
        {
            Settings.LastCalendarLocation = LocationEntry.Text;
            Settings.LastCalendarDistance = (int)DistanceSlider.Value;
            Settings.CalendarRankingSystem = (string)RankingTypePicker.SelectedItem;
            Settings.CalendarShowLeagues = ShowLeaguesCheckBox.IsChecked;

            // When we change the filter, we need to clear the local calendar events and re-sync on the new location
            if (Settings.SyncCalendarWithSystem)
            {
#pragma warning disable CS4014 // Because this call is not awaited, execution of the current method continues before the call is completed
                Task.Run(async () =>
                {
                    await CalendarSyncService.DeleteIfpaDeviceCalendarAndClearLocalEvents();
                    await CalendarSyncService.SyncIfpaCalendarWithDeviceCalendar();
                });
#pragma warning restore CS4014 // Because this call is not awaited, execution of the current method continues before the call is completed
            }

            await Navigation.PopModalAsync();
            FilterSaved?.Invoke();
        }

    }
}