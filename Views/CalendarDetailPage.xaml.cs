using Ifpa.ViewModels;
using Microsoft.Maui.Controls.Maps;
using Microsoft.Maui.Maps;

namespace Ifpa.Views
{
    [XamlCompilation(XamlCompilationOptions.Compile)]
    public partial class CalendarDetailPage : ContentPage
    {
        CalendarDetailViewModel ViewModel;

        public CalendarDetailPage(CalendarDetailViewModel viewModel)
        {
            InitializeComponent();

            BindingContext = this.ViewModel = viewModel;
        }

        protected override async void OnAppearing()
        {
            base.OnAppearing();

#pragma warning disable CS4014 // Because this call is not awaited, execution of the current method continues before the call is completed
            sheet.ShowAsync();
#pragma warning restore CS4014 // Because this call is not awaited, execution of the current method continues before the call is completed

            try
            {               
                await ViewModel.ExecuteLoadItems();

                double latitudeOffset = .007;

                var mapLocation = new Location(ViewModel.Tournament.Latitude - latitudeOffset, ViewModel.Tournament.Longitude);

                MapSpan mapSpan = MapSpan.FromCenterAndRadius(mapLocation, Distance.FromKilometers(1));
                var calendarMap = new Microsoft.Maui.Controls.Maps.Map(mapSpan)
                {
                    IsZoomEnabled = false,
                    IsScrollEnabled = false,
                    MapType = MapType.Street,
                    IsTrafficEnabled = false                    
                };
                var pin = new Pin
                {
                    Label = ViewModel.Tournament.TournamentName,
                    Location = new Location(ViewModel.Tournament.Latitude, ViewModel.Tournament.Longitude),
                    Address = ViewModel.Tournament.RawAddress,
                    Type = PinType.Generic
                };

                pin.InfoWindowClicked += async (sender, e) => await ViewModel.OpenMap();
                pin.MarkerClicked += async (sender, e) => await ViewModel.OpenMap();

                calendarMap.Pins.Add(pin);

                mapShim.Children.Add(calendarMap);
                calendarMap.HeightRequest = DeviceDisplay.MainDisplayInfo.Height;
            }
            //unable to geocode position on the map, ignore. 
            catch (Exception)
            {
            }
        }

        protected override void OnDisappearing()
        {
            base.OnDisappearing();
            sheet.DismissAsync();
        }
    }
}