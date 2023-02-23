using System.Diagnostics;
using Ifpa.Interfaces;
using PinballApi;

namespace Ifpa.ViewModels
{
    public class CalendarDetailViewModel : BaseViewModel
    {
        public string PrivateFlag { get; set; }

        public string Details { get; set; }

        public double Longitude { get; set; }

        public double Latitude { get; set; }

        public string DirectorName { get; set; }

        public DateTime EndDate { get; set; }

        public DateTime StartDate { get; set; }

        public string TournamentDuration => $"{StartDate.ToString("d")}" + (StartDate != EndDate ? $" - {EndDate.ToString("d")}" : string.Empty );
        
        public string Website { get; set; }

        public string Zipcode { get; set; }

        public string State { get; set; }

        public string City { get; set; }

        public string Address2 { get; set; }

        public string Address1 { get; set; }

        public string TournamentName { get; set; }

        public int TournamentId { get; set; }
        
        public string CountryName { get; set; }

        public string Location => $"{Address1} {Address2} {City}{(!string.IsNullOrEmpty(City) && !string.IsNullOrEmpty(State) ? "," : string.Empty)} {State} {CountryName}".Trim().Replace("  ", " ");

        public Location GeocodedLocation { get; set; } = new Location();

        public Command LoadItemsCommand { get; set; }

        public int CalendarId { get; set; }

        private readonly IReminderService ReminderService;

        public CalendarDetailViewModel(IReminderService reminderService, PinballRankingApiV1 pinballRankingApiV1, PinballRankingApiV2 pinballRankingApiV2) : base(pinballRankingApiV1, pinballRankingApiV2)
        {
            ReminderService = reminderService;
            LoadItemsCommand = new Command(async () => await ExecuteLoadItemsCommand());
        }

        public async Task ExecuteLoadItemsCommand()
        {
            if (IsBusy)
                return;

            IsBusy = true;

            try
            {                
                var items = await PinballRankingApi.GetCalendarById(CalendarId);

                var calendarEntry = items.Calendar.FirstOrDefault();
                Title = calendarEntry.TournamentName;
                Website = calendarEntry.Website;
                City = calendarEntry.City;
                DirectorName = calendarEntry.DirectorName;
                TournamentName = calendarEntry.TournamentName;
                Address1 = calendarEntry.Address1;
                Address2 = calendarEntry.Address2;
                City = calendarEntry.City;
                State = calendarEntry.State;
                CountryName = calendarEntry.CountryName;
                Details = calendarEntry.Details;
                StartDate = calendarEntry.StartDate;
                EndDate = calendarEntry.EndDate;
                TournamentId = calendarEntry.TournamentId;

                GeocodedLocation = new Location(calendarEntry.Latitude, calendarEntry.Longitude);
                Latitude = GeocodedLocation.Latitude;
                Longitude = GeocodedLocation.Longitude;

                OnPropertyChanged(null);
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex);
            }
            finally
            {
                IsBusy = false;
            }
        }

        public async Task AddToCalendar()
        {
            var status = await Permissions.CheckStatusAsync<Permissions.CalendarRead>();
            if (status != PermissionStatus.Granted)
            {
                status = await Permissions.RequestAsync<Permissions.CalendarWrite>();
            }

            if (status == PermissionStatus.Granted)
            {
                string selectedCalendar = null;

                //iOS Supports multiple calendars. no idea how to do this in Android yet. 
                if (DeviceInfo.Current.Platform == DevicePlatform.iOS)
                {
                    var calendars = await ReminderService.GetCalendarList();
                    selectedCalendar = await Shell.Current.DisplayActionSheet("This event will be added to your phone's calendar", "Cancel", null, calendars.ToArray());
                }

                if (selectedCalendar != "Cancel")
                {
                    var result = await ReminderService.CreateReminder(this, selectedCalendar);

                    if (result)
                    {
                        await Shell.Current.DisplayAlert("Success", "Tournament added to your Calendar", "OK");
                    }
                    else
                    {
                        await Shell.Current.DisplayAlert("Error", "Unable to add Tournament to your Calendar", "OK");
                    }
                }
            }
            else
            {
                await Shell.Current.DisplayAlert("Permission Required", "IFPA Companion requires your permission before adding items to your Calendar", "OK");
            }
        }
    }
}
