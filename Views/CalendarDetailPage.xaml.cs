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
            try
            {               
                await ViewModel.ExecuteLoadItemsCommand();

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

                pin.InfoWindowClicked += Pin_Clicked;
                pin.MarkerClicked += Pin_Clicked;

                calendarMap.Pins.Add(pin);

                mapShim.Children.Add(calendarMap);
                calendarMap.HeightRequest = DeviceDisplay.MainDisplayInfo.Height;
            }
            //unable to geocode position on the map, ignore. 
            catch (Exception)
            {
            }
            await sheet.ShowAsync();
        }

        protected override void OnDisappearing()
        {
            base.OnDisappearing();
            sheet.DismissAsync();
        }

        private void Pin_Clicked(object sender, PinClickedEventArgs e)
        {
            Address_Tapped(sender, e);
        }

        private async void Address_Tapped(object sender, EventArgs e)
        {
            var placemark = new Placemark
            {
                CountryName = ViewModel.Tournament.CountryName,
                AdminArea = ViewModel.Tournament.Stateprov,
                Thoroughfare = ViewModel.Tournament.Address1,
                Locality = ViewModel.Tournament.City
            };
            var options = new MapLaunchOptions { Name = ViewModel.Tournament.TournamentName };

            await Microsoft.Maui.ApplicationModel.Map.OpenAsync(placemark, options);
        }

        private async void AddToCalendarButton_Clicked(object sender, EventArgs e)
        {
            await ViewModel.AddToCalendar();
        }

        private async void ShareButton_Clicked(object sender, EventArgs e)
        {
            await Share.RequestAsync(new ShareTextRequest
            {
                Uri = $"https://www.ifpapinball.com/tournaments/view.php?t={ViewModel.TournamentId}",
                Title = Strings.CalendarDetailPage_SharePrompt
            });
        }
    }
}