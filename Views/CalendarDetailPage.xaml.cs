using Ifpa.ViewModels;

namespace Ifpa.Views
{
    [XamlCompilation(XamlCompilationOptions.Compile)]
    [QueryProperty("CalendarId", "calendarId")]
    public partial class CalendarDetailPage : ContentPage
    {
        CalendarDetailViewModel ViewModel;
        
        public int CalendarId { get; set; }

        public CalendarDetailPage(CalendarDetailViewModel viewModel)
        {       
            InitializeComponent();

            BindingContext = this.ViewModel = viewModel;

            ((CalendarDetailViewModel)BindingContext).PropertyChanged += new System.ComponentModel.PropertyChangedEventHandler(async (s, e) => await CalendarDetailPage_PropertyChanged(s, e)) ;            
        }


        protected override void OnAppearing()
        {
            base.OnAppearing();
            ViewModel.CalendarId = CalendarId;
            ViewModel.LoadItemsCommand.Execute(null);
        }

        private async Task CalendarDetailPage_PropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            //when busy is flipped back to false it means we are done loading the address
            if (e.PropertyName == "IsBusy" && ViewModel.IsBusy == false)
            {

                //TODO: Restore adding pin to map

                //try
                //{
                //    var locations = await Geocoding.GetLocationsAsync(viewModel.Address1 + " " + viewModel.City + ", " + viewModel.State);

                //    var position = new Position(locations.First().Latitude, locations.First().Longitude);
                //    calendarMap.MoveToRegion(new MapSpan(position, 0.1, 0.1));
                //    var pin = new Pin
                //    {
                //        Label = viewModel.TournamentName,
                //        Position = position,
                //        Address = viewModel.Location,
                //        Type = PinType.Generic                        
                //    };

                //    pin.InfoWindowClicked += Pin_Clicked;
               
                //    calendarMap.Pins.Add(pin);
                    
                //    calendarMap.IsVisible = true;
                //}
                ////unable to geocode position on the map, ignore. 
                //catch(Exception)
                //{
                //    calendarMap.IsVisible = false;
                //}                
            }
        }

        private void Pin_Clicked(object sender, EventArgs e)
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

            await Map.OpenAsync(placemark, options);
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