﻿using Ifpa.Models;
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

                MapSpan mapSpan = MapSpan.FromCenterAndRadius(ViewModel.GeocodedLocation, Microsoft.Maui.Maps.Distance.FromKilometers(1));
                var calendarMap = new Microsoft.Maui.Controls.Maps.Map(mapSpan)
                {
                    HeightRequest = 200,
                    IsZoomEnabled = false,
                    IsScrollEnabled = false,
                    MapType = MapType.Street
                };
                var pin = new Pin
                {
                    Label = ViewModel.TournamentName,
                    Location = ViewModel.GeocodedLocation,
                    Address = ViewModel.Location,
                    Type = PinType.Generic
                };

                pin.InfoWindowClicked += Pin_Clicked;
                pin.MarkerClicked += Pin_Clicked;

                calendarMap.Pins.Add(pin);

                mapShim.Children.Add(calendarMap);
            }
            //unable to geocode position on the map, ignore. 
            catch (Exception)
            {
            }
        }

        private void Pin_Clicked(object sender, PinClickedEventArgs e)
        {
            Address_Tapped(sender, e);
        }

        private async void WebsiteLabel_Tapped(object sender, EventArgs e)
        {
            await Browser.OpenAsync(ViewModel.Website, BrowserLaunchMode.SystemPreferred);
        }

        private async void Address_Tapped(object sender, EventArgs e)
        {
            var placemark = new Placemark
            {
                CountryName = ViewModel.CountryName,
                AdminArea = ViewModel.State,
                Thoroughfare = ViewModel.Address1,
                Locality = ViewModel.City
            };
            var options = new MapLaunchOptions { Name = ViewModel.TournamentName };

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
                Title = "Share Upcoming Tournament"
            });
        }
    }
}