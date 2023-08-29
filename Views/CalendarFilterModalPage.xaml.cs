using System.Diagnostics;
using Ifpa.Models;
using Serilog;

namespace Ifpa.Views
{
    [XamlCompilation(XamlCompilationOptions.Compile)]
    public partial class CalendarFilterModalPage : ContentPage
    {
        public delegate void FilterSavedDelegate();

        public event FilterSavedDelegate FilterSaved;

        public CalendarFilterModalPage()
        {
            InitializeComponent();

            var lastCalendarLocation = Settings.LastCalendarLocation;
            var lastCalendarDistance = Settings.LastCalendarDistance;

            DistanceSlider.Value = lastCalendarDistance;
            LocationEntry.Text = lastCalendarLocation;
            DistanceText.Text = ((int)DistanceSlider.Value).ToString();
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
                await DisplayAlert("Permission Required", "IFPA Companion requires your permission before polling your location for Calendar Search", "OK");
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

            await Navigation.PopModalAsync();
            FilterSaved?.Invoke();
        }
    }
}